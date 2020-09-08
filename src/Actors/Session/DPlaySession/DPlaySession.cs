using System;
using System.Diagnostics;
using System.IO;
using Akka.Actor;
using Ionic.Zlib;

namespace FLServer.Actors.Session.DPlaySession
{
    public partial class DPlaySession : TypedActor, 
        IHandle<DPlaySession.ConnectRequest>,
        IHandle<string>,
        IHandle<DPlaySession.ByteMessage>,
        IHandle<DPlaySession.SendNews>,IHandle<byte[]>
    {
        private FLServer.Player.Session.State _sessionState;
        private uint _dPlayID;

        readonly Akka.Event.LoggingAdapter _log = Akka.Event.Logging.GetLogger(Context);
        private ActorRef _globals;

        private LocalActorRef _playerRef;

        public void Handle(ConnectRequest message)
        {
            if (_sessionState == FLServer.Player.Session.State.CONNECTED) return;

            var selection = Context.System.ActorSelection("user/server/globals");
            var glob = selection.ResolveOne(TimeSpan.FromSeconds(1));
            glob.Wait(750);
            _globals = glob.Result;

            if (_dPlayID != 0 && message.DPlayID != _dPlayID)
            {
                //throw new Exception("DPlay ID changed!");
                _log.Warn("{0} DPlay ID changed!",Context.Self.Path);
                return;
                //TODO: do something clever
            }
            _sessionState = FLServer.Player.Session.State.CONNECTING;
            _dPlayID = message.DPlayID;

            byte[] pkt = { 0x88, 0x02 };
            FLMsgType.AddUInt8(ref pkt, message.MsgID++);
            FLMsgType.AddUInt8(ref pkt, message.RspID);
            FLMsgType.AddUInt32(ref pkt, 0x10004);
            FLMsgType.AddUInt32(ref pkt, message.DPlayID);
            FLMsgType.AddUInt32(ref pkt, (uint)Stopwatch.GetTimestamp());

            //sess.BytesTx += pkt.Length;
            Context.Parent.Tell(new Session.ConnectingMessage());
            Context.Sender.Tell(pkt,Context.Self);
            
        }

        public void Handle(string message)
        {
            switch (message)
            {
                case "GetSessState":
                    Context.Sender.Tell(_sessionState);
                    break;
                case "GetDPlayID":
                    Context.Sender.Tell(_dPlayID,Self);
                    break;
            }
        }

        public void Handle(ByteMessage message)
        {
            // If the user 1 flag is set then this is a session management message.
            // Do not pass to the higher layers but rather process locally.
            if ((message.Data[0] & 0x40) == 0x40)
            {
                var type = FLMsgType.GetUInt32(message.Data, ref message.Position);
                // On receipt of a trans user data session info we send back a dummy
                // entry that's just sufficient to let the client's dplay implementation
                // to recognise us.
                switch (type)
                {
                    case 0xC1:
                        if (_sessionState != FLServer.Player.Session.State.CONNECTING)
                            return;
                        _sessionState = FLServer.Player.Session.State.CONNECTING_SESSINFO;
                        SendTUDSessionInfo();
                        break;
                    case 0xC3:
                        if (_sessionState != FLServer.Player.Session.State.CONNECTING_SESSINFO)
                            return;
                        _sessionState = FLServer.Player.Session.State.CONNECTED;
                        var p = Context.System.Guardian.GetSingleChild("server").Ask<LocalActorRef>("NewPlayer");
                        p.Wait(10);
                        _playerRef = p.Result;

                        // FLPACKET_SERVER_CONNECTRESPONSE
                        byte[] omsg = {0x01, 0x02};
                        var num = _playerRef.Path.Name.Split('/')[1];
                        var pid = uint.Parse(num);
                        FLMsgType.AddUInt32(ref omsg, pid);
                        Handle(omsg);
                        Handle(new SendNews());
                        //Sender.Tell(omsg);
                        //OnPlayerConnected(sess);
                        break;
                }
            }
                // If this is a complete message then pass up.
            else if ((message.Data[0] & 0x30) == 0x30)
            {
                if ((message.Data.Length - message.Position) <= 0) return;

                byte[] msg = FLMsgType.GetArray(message.Data, ref message.Position, message.Data.Length - message.Position);
                _playerRef.Tell(msg);
            }
            // Otherwise we don't support this. Flag the error.
            else
            {
                _log.Warn("{0} unsupported message: {1}",Self.Path,message.Data);
            }
        }


        /// <summary>
        ///     Send a dummy trans
        /// </summary>
        private void SendTUDSessionInfo()
        {
            var pkt = new byte[0];
            FLMsgType.AddUInt32(ref pkt, 0xC2); // dwPacketType
            FLMsgType.AddUInt32(ref pkt, 0); // dwReplyOffset
            FLMsgType.AddUInt32(ref pkt, 0); // dwReplySize

            FLMsgType.AddUInt32(ref pkt, 0x50); // dwApplicationDescSize
            FLMsgType.AddUInt32(ref pkt, 0x01); // dwFlags




            var resp = _globals.Ask<Globals.ServerInfo>("GetInfo");
            resp.Wait(5);
            var result = resp.Result;
            FLMsgType.AddUInt32(ref pkt, result.MaxPlayers + 1); // dwMaxPlayers
            FLMsgType.AddUInt32(ref pkt, (uint)result.CurrentPlayers + 1); // dwCurrentPlayers
            FLMsgType.AddUInt32(ref pkt, 0x6C + 0x60); // dwSessionNameOffset
            FLMsgType.AddUInt32(ref pkt, (uint)result.ServerName.Length * 2); // dwSessionNameSize
            FLMsgType.AddUInt32(ref pkt, 0); // dwPasswordOffset
            FLMsgType.AddUInt32(ref pkt, 0); // dwPasswordSize
            FLMsgType.AddUInt32(ref pkt, 0); // dwReservedDataOffset
            FLMsgType.AddUInt32(ref pkt, 0); // dwReservedDataSize
            FLMsgType.AddUInt32(ref pkt, 0); // dwApplicationReservedDataOffset
            FLMsgType.AddUInt32(ref pkt, 0); // dwApplicationReservedDataSize
            FLMsgType.AddArray(ref pkt, result.InstanceGUID);
            FLMsgType.AddArray(ref pkt, result.AppGUID);
            FLMsgType.AddUInt32(ref pkt, _dPlayID); // dpnid
            FLMsgType.AddUInt32(ref pkt, _dPlayID); // dwVersion
            FLMsgType.AddUInt32(ref pkt, 0); // dwVersionNotUsed
            FLMsgType.AddUInt32(ref pkt, 2); // dwEntryCount
            FLMsgType.AddUInt32(ref pkt, 0); // dwMembershipCount

            // server name table entry
            FLMsgType.AddUInt32(ref pkt, 1); // dpnid
            FLMsgType.AddUInt32(ref pkt, 0); // dpnidOwner
            FLMsgType.AddUInt32(ref pkt, 0x000402); // dwFlags
            FLMsgType.AddUInt32(ref pkt, 2); // dwVersion
            FLMsgType.AddUInt32(ref pkt, 0); // dwVersionNotUsed
            FLMsgType.AddUInt32(ref pkt, 7); // dwDNETVersion
            FLMsgType.AddUInt32(ref pkt, 0); // dwNameOffset
            FLMsgType.AddUInt32(ref pkt, 0); // dwNameSize
            FLMsgType.AddUInt32(ref pkt, 0); // dwDataOffset
            FLMsgType.AddUInt32(ref pkt, 0); // dwDataSize
            FLMsgType.AddUInt32(ref pkt, 0); // dwURLOffset
            FLMsgType.AddUInt32(ref pkt, 0); // dwURLSize

            // connecting client name table entry
            FLMsgType.AddUInt32(ref pkt, _dPlayID); // dpnid
            FLMsgType.AddUInt32(ref pkt, 0); // dpnidOwner
            FLMsgType.AddUInt32(ref pkt, 0x020000); // dwFlags
            FLMsgType.AddUInt32(ref pkt, _dPlayID); // dwVersion
            FLMsgType.AddUInt32(ref pkt, 0); // dwVersionNotUsed
            FLMsgType.AddUInt32(ref pkt, 7); // dwDNETVersion
            FLMsgType.AddUInt32(ref pkt, 0); // dwNameOffset
            FLMsgType.AddUInt32(ref pkt, 0); // dwNameSize
            FLMsgType.AddUInt32(ref pkt, 0); // dwDataOffset
            FLMsgType.AddUInt32(ref pkt, 0); // dwDataSize
            FLMsgType.AddUInt32(ref pkt, 0); // dwURLOffset
            FLMsgType.AddUInt32(ref pkt, 0); // dwURLSize

            FLMsgType.AddUnicodeStringLen0(ref pkt, result.ServerName);
            Context.ActorSelection("../congestion").Tell(pkt);
            //UserData.AddLast(pkt);
            //SendDFrame(sess);
        }

        public void Handle(SendNews message)
        {
            _log.Debug("{0} Sending news", Self.Path);
                    
            byte[] omsg = {0x54, 0x02, 0x10, 0x00};


            var resp = _globals.Ask<string>("GetNews");
            resp.Wait(75);

            FLMsgType.AddUnicodeStringLen16(ref omsg, resp.Result);
                    
            Handle(omsg);
            //Context.ActorSelection("../socket").Tell(omsg);
            
        }

        public void Handle(byte[] message)
        {
            //_log.Debug("s>c uncompressed: {0}",BitConverter.ToString(message));
            if (message.Length > 0x50)
            {
                using (var ms = new MemoryStream())
                {
                    using (var zs = new ZlibStream(ms, CompressionMode.Compress))
                        zs.Write(message, 0, message.Length);
                    message = ms.ToArray();
                }
            }
            Context.ActorSelection("../congestion").Tell(message);
        }
    }
}
