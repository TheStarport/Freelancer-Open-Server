using System;
using System.IO;
using Akka.Actor;
using Akka.Event;
using FLServer.Actors.Player.PlayerList;
using FLServer.Actors.Player.Ship;
using FLServer.Actors.Player.State;
using FLServer.DataWorkers;
using Ionic.Zlib;

// ReSharper disable once CheckNamespace
using FLServer.Objects;
using System.Threading;


namespace FLServer.Actors
{
    partial class PlayerActor : TypedActor,IHandle<byte[]>,
        IHandle<PlayerActor.SetAccountID>,
        IHandle<PlayerActor.CharSelected>,
        IHandle<PlayerStatsRequest>,
        IHandle<PlayerActor.EnterBase>
    {

        readonly InternalActorRef _state;

        string _accountID;
        uint _flplayerID;

        CharDB.Account _account;
        ActorRef _socket;
        private ActorRef _chat;

        public PlayerActor()
        {
            _state = Context.ActorOf<State>("state");
            _chat = Context.ActorOf<Player.Chat.Chat>("chat");
            Context.ActorOf<PlayerListHandler>("listhandler");
            Context.ActorOf<PlayerShip>("ship");
        }

        readonly LoggingAdapter _log = Akka.Event.Logging.GetLogger(Context);

        //WARNING
        //The sender is DPlaySession
        public void Handle(byte[] message)
        {
            if (message[0] == FLMsgType.MSG_TYPE_COMPRESSED)
            {
                using (var ms = new MemoryStream(message, 0, message.Length))
                {
                    using (var zs = new ZlibStream(ms, CompressionMode.Decompress))
                    {
                        var buf = new byte[32767];
                        var msgLength = zs.Read(buf, 0, buf.Length);
                        Array.Resize(ref buf, msgLength);
                        message = buf;
                    }
                }
            }

            // Otherwise dispatch the message to the controller.
            if (message[0] != 0x01) //not a typo, log junk cleaning
                _log.Debug("rxMessage {0}: {1}", Sender.Path.Name, BitConverter.ToString(message));

            //FLServer.DPServer._dplay_GotMessage
            _state.Tell(message, Sender);
        }


        public void Handle(SetAccountID message)
        {
            _accountID = message.AccountID;
            _flplayerID = message.FLPlayerID;
        }

        public void Handle(CharSelected message)
        {
            _account = message.Account;
            _socket = Context.Sender;
            
            Context.Child("listhandler").Tell(_socket);
            Context.Child("listhandler").Tell(new SetListData(_flplayerID, 0, _account.Rank, FLUtility.CreateID(_account.System), _account.CharName));
            //Sender is socket

            // Reset the player list. I'm not certain that this is necessary.
            {
                byte[] omsg = { 0x52, 0x02 };
                FLMsgType.AddUInt32(ref omsg, 0);
                FLMsgType.AddUInt32(ref omsg, 0);
                FLMsgType.AddUInt8(ref omsg, 0);
                FLMsgType.AddUInt8(ref omsg, 0);
                Sender.Tell(omsg);
            }

            //TODO: should we hide em sometime? Admins etc.
            Context.ActorSelection("../*/listhandler").Tell(new PlayerJoined(_account.CharName, message.FLPlayerID), Context.Child("listhandler"));
            //TODO: initiate ShipData and pass to ./ship
        }

        public void Handle(PlayerStatsRequest message)
        {
            byte[] omsg = { 0x18, 0x01 };
            FLMsgType.AddInt32(ref omsg, (13 * 4) + (_account.Reputations.Count * 8) + (_account.Kills.Count * 8)); //

            FLMsgType.AddUInt32(ref omsg, 4); // rm_completed
            FLMsgType.AddUInt32(ref omsg, 0); // u_dword
            FLMsgType.AddUInt32(ref omsg, 2); // rm_failed
            FLMsgType.AddUInt32(ref omsg, 0); // u_dword
            FLMsgType.AddFloat(ref omsg, 10000.0f); // total_time_played
            FLMsgType.AddUInt32(ref omsg, 6); // systems_visited
            FLMsgType.AddUInt32(ref omsg, 5); // bases_visited
            FLMsgType.AddUInt32(ref omsg, 4); // holes_visited
            FLMsgType.AddInt32(ref omsg, _account.Kills.Count); // kills_cnt
            FLMsgType.AddUInt32(ref omsg, _account.Rank); // rank
            FLMsgType.AddUInt32(ref omsg, (UInt32)_account.Money); // current_worth
            FLMsgType.AddUInt32(ref omsg, 0); // dunno
            FLMsgType.AddInt32(ref omsg, _account.Reputations.Count);
            foreach (var pi in _account.Kills)
            {
                FLMsgType.AddUInt32(ref omsg, pi.Key);
                FLMsgType.AddUInt32(ref omsg, pi.Value);
            }
            foreach (var pi in _account.Reputations)
            {
                //TODO: check hash
                FLMsgType.AddUInt32(ref omsg, FLUtility.CreateFactionID(pi.Key));
                FLMsgType.AddFloat(ref omsg, pi.Value);
            }

            _socket.Tell(omsg);
        }

        public void Handle(EnterBase message)
        {
            _log.Debug("FLID {0} enters base {1}", _flplayerID, message.BaseID);
            //TODO: debug if null or ""
            var baseid = FLUtility.CreateID(_account.ShipState.Base);
            if (_account.ShipState.Base != null && baseid != message.BaseID)
            {
                _log.Warn("{0} CHEAT: tried to enter {1} when saved at {2} ({3}",
                    _account.CharName, message.BaseID, baseid,
                    _account.ShipState.Base);
                //TODO: kick for cheating
            }
            var sdata = Context.Child("ship").Ask<ShipData>(new AskShipData(), TimeSpan.FromMilliseconds(850));
            Context.Sender.Tell(new AccountShipData(_account, sdata.Result));
            Context.Child("ship").Tell(new AskHullStatus(), _socket);

        }
    }
}
