using System.Collections.Generic;
using System.Linq;
using Akka.Actor;
using FLServer.DataWorkers;
using FLServer.GameDB;
using FLServer.GameDB.Base;
using NLog;
using FLServer.Objects;
using System.Windows.Forms;
using System;

namespace FLServer.Actors.Player.State.Base
{
    class BaseState : TypedActor,
    IHandle<EnterBaseData>, IHandle<BaseInfoRequest>,
        IHandle<EnterLocation>,IHandle<LocInfoRequest>,
        IHandle<ExitLocation>, IHandle<SelectObject>,
        IHandle<RankRequest>
    {
        static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        CharDB.Account _charAccount;
        ShipData _shipData;
        uint _baseID;
        uint _roomID;
        GameDB.Base.Base _base;

        public void Handle(EnterBaseData message)
        {
            _baseID = message.BaseID;
            _charAccount = message.Account;
            _shipData = message.ShipData;
        }

        public void Handle(BaseInfoRequest message)
        {
            // FLPACKET_CLIENT_REQUESTBASEINFO
            
            int pos = 2;
            uint baseid = FLMsgType.GetUInt32(message.Message, ref pos);
            uint type = FLMsgType.GetUInt8(message.Message, ref pos);
            Logger.Debug("tx FLPACKET_CLIENT_REQUESTBASEINFO id {0} type {1}", baseid, type);
            if (baseid != _baseID)
            {
                //TODO: kick and log, docked base != requested base
            }

            // TODO: Eventually ignore the client message and send the base info
            // that the server believes the player to be at.

            if (type != 1)
                return;
            _base = BaseDB.GetBase(baseid);

            if (_base == null)
            {
                //TODO: kick and log
            }

            SendSetStartRoom(baseid, _base.StartRoomID); // fixme?
            SendGFCompleteMissionComputerList(baseid);
            SendGFCompleteNewsBroadcastList(_base);
        }


        /// <summary>
        ///     FLPACKET_SERVER_SETSTARTROOM
        /// Set a room for just-docked player.
        /// </summary>
        /// <param name="baseid"></param>
        /// <param name="roomid"></param>
        void SendSetStartRoom(uint baseid, uint roomid)
        {
            
            Logger.Debug("tx FLPACKET_SERVER_SETSTARTROOM char {2} id {0} room {1}", baseid, roomid, _charAccount.CharName);
            byte[] omsg = { 0x0d, 0x02 };
            FLMsgType.AddUInt32(ref omsg, baseid);
            FLMsgType.AddUInt32(ref omsg, roomid);
            Context.Sender.Tell(omsg);
        }

        /// <summary>
        ///     FLPACKET_SERVER_GFCOMPLETEMISSIONCOMPUTERLIST
        /// </summary>
        /// <param name="baseid"></param>
        void SendGFCompleteMissionComputerList(uint baseid)
        {
            Logger.Debug("tx FLPACKET_SERVER_GFCOMPLETEMISSIONCOMPUTERLIST char {0}", _charAccount.CharName);
            //TODO: send missions
            byte[] omsg = { 0x10, 0x02 };
            FLMsgType.AddUInt32(ref omsg, baseid);
            Context.Sender.Tell(omsg);
        }

        /// <summary>
        ///     FLPACKET_SERVER_GFCOMPLETENEWSBROADCASTLIST
        /// </summary>
        void SendGFCompleteNewsBroadcastList(GameDB.Base.Base ubase)
        {
            Logger.Debug("tx FLPACKET_BASE_NEWS char {2} {0} count {1}", ubase.Nickname, ubase.News.Count, _charAccount.CharName);
            //BaseData bd = UniverseDB.FindBase(baseid);

            uint newsid = 0;
            foreach (var ni in ubase.News)
            {
                byte[] omsg = { 0x1E, 0x02 };
                FLMsgType.AddUInt32(ref omsg, 40 + (uint)ni.Logo.Length); // size goes here

                FLMsgType.AddUInt32(ref omsg, newsid++);
                FLMsgType.AddUInt32(ref omsg, ubase.BaseID);
                FLMsgType.AddUInt16(ref omsg, 0);

                FLMsgType.AddUInt32(ref omsg, ni.Icon);
                FLMsgType.AddUInt32(ref omsg, ni.Category);
                FLMsgType.AddUInt16(ref omsg, 0);
                FLMsgType.AddUInt32(ref omsg, ni.Headline);
                FLMsgType.AddUInt16(ref omsg, 0);
                FLMsgType.AddUInt32(ref omsg, ni.Text);
                FLMsgType.AddUInt16(ref omsg, 0);
                FLMsgType.AddAsciiStringLen32(ref omsg, ni.Logo);
                FLMsgType.AddUInt32(ref omsg, 0); // unknown hash, 0 seems to work

                Context.Sender.Tell(omsg);
            }

            // Send "news list complete" message
            {
                byte[] omsg = { 0x0e, 0x02 };
                FLMsgType.AddUInt32(ref omsg, ubase.BaseID);
                Context.Sender.Tell(omsg);
            }
        }

        public void Handle(RequestAddItem Message)
        {
            throw new NotImplementedException();
        }

        public void Handle(SelectObject message)
        {
            // FLPACKET_CLIENT_GFSELECTOBJECT
            // Send the barman, dealers, etc. These have fixed locations and
            // fidget scripts
            foreach (var ch in _base.Chars.Values.Where(ch => ch.Type == "bar"))
            {
                SendNPCGFUpdateScripts(ch, 2);
            }
        }


        #region "Entering room"


        public void Handle(EnterLocation message)
        {
            // FLPACKET_CLIENT_ENTERLOCATION
            Logger.Debug("rx FLPACKET_CLIENT_ENTERLOCATION char {1} room {0}", message.RoomID, _charAccount.CharName);
            _roomID = message.RoomID;
        }

        public void Handle(ExitLocation message)
        {
            // FLPACKET_CLIENT_EXITLOCATION
            Logger.Debug("rx FLPACKET_CLIENT_EXITLOCATION char {1} {0}", message.RoomID, _charAccount.CharName);
            _roomID = 0;
        }

        public void Handle(LocInfoRequest message)
        {
            // FLPACKET_CLIENT_REQUESTLOCATIONINFO
            int pos = 2;
            uint roomid = FLMsgType.GetUInt32(message.Message, ref pos);
            if (roomid != _roomID)
            {
                //TODO: kick and log
            }
            uint type = FLMsgType.GetUInt8(message.Message, ref pos);
            Logger.Debug("rx FLPACKET_CLIENT_REQUESTLOCATIONINFO char {2} roomid={0} type={1}", roomid, type, _charAccount.CharName);

            if (type != 1)
                return;
            SendGFCompleteCharList(roomid);
            SendGFCompleteScriptBehaviourList(roomid);
            SendGFCompleteAmbientScriptList(roomid);
        }


        void SendGFCompleteCharList(uint roomid)
        {
            Logger.Debug("tx FLPACKET_SERVER_GFCOMPLETECHARLIST char {0}", _charAccount.CharName);

            // Send the player 'trent' character
            uint charid = 1;
            SendPlayerGFUpdateChar(roomid, charid++);

            // Send the barman, dealers, etc. These have fixed locations and
            // fidget scripts

            //TODO: send base characters
            //TODO: room change bug is likely here!

            foreach (var bchar in _base.Chars.Values.Where(ch=>ch.RoomID == roomid))
            {
                if (bchar.Type == null)
                    continue;
                SendNPCGFUpdateChar(bchar, charid);
                SendNPCGFUpdateScripts(bchar, charid);
                charid++;
            }

            // Send the barflies

            // Send "char list complete message"
            
            byte[] omsg = { 0x0f, 0x02 };
            FLMsgType.AddUInt32(ref omsg, roomid);
            Context.Sender.Tell(omsg);
            
        }

        static void SendNPCGFUpdateChar(Character ch, uint charid)
        {
            byte[] omsg = { 0x26, 0x02 };

            FLMsgType.AddUInt32(ref omsg, 68 + (uint)ch.FidgetScript.Length + (uint)ch.RoomLocation.Length);
            FLMsgType.AddUInt32(ref omsg, charid);
            FLMsgType.AddUInt32(ref omsg, 0); // 1 = player, 0 = npc
            FLMsgType.AddUInt32(ref omsg, ch.RoomID);
            FLMsgType.AddUInt32(ref omsg, ch.IndividualName); // npc name
            FLMsgType.AddUInt32(ref omsg, FLUtility.CreateFactionID(ch.Faction)); // faction
            FLMsgType.AddInt32(ref omsg, -1);
            FLMsgType.AddUInt32(ref omsg, ch.Head);
            FLMsgType.AddUInt32(ref omsg, ch.Body);
            FLMsgType.AddUInt32(ref omsg, ch.Lefthand);
            FLMsgType.AddUInt32(ref omsg, ch.Righthand);
            FLMsgType.AddUInt32(ref omsg, 0); // accessories count + list
            FLMsgType.AddAsciiStringLen32(ref omsg, ch.FidgetScript);
            FLMsgType.AddUInt32(ref omsg, charid); // behaviourid

            if (ch.RoomLocation.Length == 0)
                FLMsgType.AddInt32(ref omsg, -1);
            else
                FLMsgType.AddAsciiStringLen32(ref omsg, ch.RoomLocation);

            FLMsgType.AddUInt32(ref omsg, 0x00); // 1 = player, 0 = npc
            FLMsgType.AddUInt32(ref omsg, 0x00); // 1 = sitlow, 0 = stand
            FLMsgType.AddUInt32(ref omsg, ch.Voice); // voice

            Context.Sender.Tell(omsg);
        }

        static void SendNPCGFUpdateScripts(Character npc, uint charid)
        {
            List<string> scripts = BaseDB.GetScriptsForNPCInteraction(npc);

            //int scriptSize = 0;
            //foreach (string script in scripts)
            //scriptSize += 8 + script.Length;
            int scriptSize = scripts.Sum(script => 8 + script.Length);

            byte[] omsg = { 0x1A, 0x02 };
            FLMsgType.AddUInt32(ref omsg, 56 + (uint)scriptSize); // size
            FLMsgType.AddUInt32(ref omsg, charid); // behaviourid
            FLMsgType.AddUInt32(ref omsg, npc.RoomID);

            FLMsgType.AddUInt8(ref omsg, 0);
            FLMsgType.AddUInt8(ref omsg, 0);
            FLMsgType.AddUInt8(ref omsg, 1);
            FLMsgType.AddUInt32(ref omsg, 0);
            FLMsgType.AddUInt32(ref omsg, 0);
            FLMsgType.AddUInt8(ref omsg, 1);

            FLMsgType.AddUInt32(ref omsg, 0); // count for some strings (maybe locations?) that don't appear to be used

            FLMsgType.AddInt32(ref omsg, scripts.Count); // count for behaviour scripts
            foreach (string script in scripts)
            {
                FLMsgType.AddAsciiStringLen32(ref omsg, script); // script name
                FLMsgType.AddUInt32(ref omsg, 0); // script path level
            }

            FLMsgType.AddUInt32(ref omsg, 0); // talkid (nothing, bribe, mission, rumor, info)
            FLMsgType.AddUInt32(ref omsg, charid); // charid

            FLMsgType.AddUInt32(ref omsg, 1); // dunno

            FLMsgType.AddUInt32(ref omsg, 0); // resourceid
            FLMsgType.AddUInt16(ref omsg, 0); // count
            FLMsgType.AddUInt32(ref omsg, 0); // resourceid
            FLMsgType.AddUInt16(ref omsg, 0); // count
            FLMsgType.AddUInt32(ref omsg, 0); // dunno

            Context.Sender.Tell(omsg);
        }

        void SendPlayerGFUpdateChar(uint roomid, uint charid)
        {
            byte[] omsg = { 0x26, 0x02 };

            const string movementScript = "scripts\\extras\\player_fidget.thn";
            const string roomLocation = "";

            FLMsgType.AddUInt32(ref omsg,
                74 + (uint)_charAccount.CharName.Length + (uint)movementScript.Length + (uint)roomLocation.Length);
            FLMsgType.AddUInt32(ref omsg, charid);
            FLMsgType.AddUInt32(ref omsg, 0x01); // 1 = player, 0 = npc
            FLMsgType.AddUInt32(ref omsg, roomid);
            FLMsgType.AddUInt32(ref omsg, 0); // npc name
            FLMsgType.AddUInt32(ref omsg, FLUtility.CreateID(_charAccount.ShipState.RepGroup)); // faction
            FLMsgType.AddUnicodeStringLen32(ref omsg, _charAccount.CharName);
            FLMsgType.AddUInt32(ref omsg, _charAccount.Appearance.Head);
            FLMsgType.AddUInt32(ref omsg, _charAccount.Appearance.Body);
            FLMsgType.AddUInt32(ref omsg, _charAccount.Appearance.LeftHand);
            FLMsgType.AddUInt32(ref omsg, _charAccount.Appearance.RightHand);
            FLMsgType.AddUInt32(ref omsg, 0); // accessories count + list
            FLMsgType.AddAsciiStringLen32(ref omsg, movementScript);
            FLMsgType.AddInt32(ref omsg, -1); // behaviourid

            if (roomLocation.Length == 0)
                FLMsgType.AddInt32(ref omsg, -1);
            else
                FLMsgType.AddAsciiStringLen32(ref omsg, roomLocation);

            FLMsgType.AddUInt32(ref omsg, 0x01); // 1 = player, 0 = npc
            FLMsgType.AddUInt32(ref omsg, 0x00); // 1 = sitlow, 0 = stand
            FLMsgType.AddUInt32(ref omsg, FLUtility.CreateID(_charAccount.Appearance.Voice)); // voice

            Context.Sender.Tell(omsg);
        }


        void SendGFCompleteScriptBehaviourList(uint roomid)
        {
            Logger.Debug("tx FLPACKET_SERVER_GFCOMPLETESCRIPTBEHAVIORLIST char {0}", _charAccount.CharName);

            {
                byte[] omsg = { 0x11, 0x02 };
                FLMsgType.AddUInt32(ref omsg, roomid);
                Context.Sender.Tell(omsg);
            }
        }


        void SendGFCompleteAmbientScriptList(uint roomid)
        {
            Logger.Debug("tx FLPACKET_SERVER_GFCOMPLETEAMBIENTSCRIPTLIST char {0}", _charAccount.CharName);
            //TODO: figure out the scripts
            byte[] omsg = { 0x13, 0x02 };
            FLMsgType.AddUInt32(ref omsg, roomid);
            Context.Sender.Tell(omsg);
        }

        #endregion

        public void Handle(RankRequest message)
        {
            byte[] omsg = { 0x1a, 0x01 };
            FLMsgType.AddUInt32(ref omsg, _charAccount.Rank);
            FLMsgType.AddUInt32(ref omsg, 0xffffffff); //dunno
            Context.Sender.Tell(omsg);
        }
    }
}
