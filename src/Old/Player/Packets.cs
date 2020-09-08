using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FLServer.DataWorkers;
using FLServer.Object.Base;
using FLServer.Object.Solar;
using FLServer.Old.CharacterDB;
using FLServer.Physics;
using FLServer.Server;
using FLServer.Ship;
using Ionic.Zlib;

namespace FLServer.Player
{
    public class Packets
    {
        private Player _player;

        public Packets(Player player)
        {
            _player = player;
        }

        public static void SendMiscObjUpdate(Player player, Player.MiscObjUpdateType update, string msg)
        {
            switch (update)
            {
                case Player.MiscObjUpdateType.NEWS: //  news?
                {
                    player.Log.AddLog(LogType.FL_MSG, "tx FLPACKET_SERVER_MISCOBJUPDATE type=news msg=\"{0}\"", msg);
                    byte[] omsg = {0x54, 0x02, 0x10, 0x00};
                    FLMsgType.AddUnicodeStringLen16(ref omsg, msg);
                    player.SendMsgToClient(omsg);
                    return;
                }
            }
        }

        public static void SendMiscObjUpdate(Player player, Player.MiscObjUpdateType update, params uint[] values)
        {
            switch (update)
            {
                case Player.MiscObjUpdateType.RANK:
                {
                    player.Log.AddLog(LogType.FL_MSG, "tx FLPACKET_SERVER_MISCOBJUPDATE type=rank playerid={0} rank={1}",
                        values[0], values[1]);
                    byte[] omsg = {0x54, 0x02, 0x44, 0x00};
                    FLMsgType.AddUInt32(ref omsg, values[0]);
                    FLMsgType.AddUInt16(ref omsg, values[1]);
                    player.SendMsgToClient(omsg);
                    return;
                }
                case Player.MiscObjUpdateType.SYSTEM:
                {
                    player.Log.AddLog(LogType.FL_MSG,
                        "tx FLPACKET_SERVER_MISCOBJUPDATE type=system playerid={0} systemid={1}",
                        values[0], values[1]);
                    byte[] omsg = {0x54, 0x02, 0x84, 0x00};
                    FLMsgType.AddUInt32(ref omsg, values[0]);
                    FLMsgType.AddUInt32(ref omsg, values[1]);
                    player.SendMsgToClient(omsg);
                    return;
                }
                case Player.MiscObjUpdateType.GROUP:
                {
                    player.Log.AddLog(LogType.FL_MSG,
                        "tx FLPACKET_SERVER_MISCOBJUPDATE type=group playerid={0} group={1}",
                        values[0], values[1]);
                    byte[] omsg = {0x54, 0x02, 0x05, 0x00};
                    FLMsgType.AddUInt32(ref omsg, values[0]);
                    FLMsgType.AddUInt32(ref omsg, values[1]);
                    player.SendMsgToClient(omsg);
                    return;
                }
                case Player.MiscObjUpdateType.UNK2:
                {
                    player.Log.AddLog(LogType.FL_MSG, "tx FLPACKET_SERVER_MISCOBJUPDATE type=unknown2 objid={0}",
                        values[0]);
                    byte[] omsg = {0x54, 0x02, 0x28, 0x00};
                    FLMsgType.AddUInt32(ref omsg, values[0]);
                    FLMsgType.AddInt32(ref omsg, -1); // faction?
                    player.SendMsgToClient(omsg);
                    return;
                }
                case Player.MiscObjUpdateType.UNK3:
                {
                    player.Log.AddLog(LogType.FL_MSG, "tx FLPACKET_SERVER_MISCOBJUPDATE type=unknown3 objid={0}",
                        values[0]);
                    byte[] omsg = {0x54, 0x02, 0x09, 0x00};
                    FLMsgType.AddUInt32(ref omsg, values[0]);
                    FLMsgType.AddUInt32(ref omsg, 0);
                    player.SendMsgToClient(omsg);
                    return;
                }
                default:
                    return;
            }
        }

        /// <summary>
        ///     Send an update to the player list when a player has selected a
        ///     character, changed system or rank. Is this only valid if a
        ///     character has been selected
        /// </summary>
        /// <param name="playerto"></param>
        /// <param name="player"></param>
        public static void SendPlayerListUpdate(Player playerto, DPGameRunner.PlayerListItem player)
        {
            if (player.Name != null)
            {
                // Send Group
                SendMiscObjUpdate(playerto, Player.MiscObjUpdateType.GROUP, player.FlPlayerID,
                    player.Group == null ? 0 : player.Group.ID);

                // Send rank
                SendMiscObjUpdate(playerto, Player.MiscObjUpdateType.RANK, player.FlPlayerID, player.Rank);

                // Send system
                SendMiscObjUpdate(playerto, Player.MiscObjUpdateType.SYSTEM, player.FlPlayerID, player.System.SystemID);

                // TODO: ?
                // Send affiliation/faction information
                //{
                //    log.AddLog(String.Format("tx FLPACKET_SERVER_MISCOBJUPDATE type=faction playerid={0} faction={1}", player.flplayerid, 0));
                //    byte[] omsg = { 0x54, 0x02, 0x24, 0x00 };
                //    FLMsgType.AddUInt32(ref omsg, player.flplayerid);
                //    FLMsgType.AddUInt32(ref omsg, 0xFFFFFFFF); // player.ship.faction.factionid);
                //    SendMsgToClient(omsg);
                //}
            }
        }

        /// <summary>
        ///     Send an update to the player list when a player has left the server.
        /// </summary>
        /// <param name="playerto"></param>
        /// <param name="player"></param>
        public static void SendPlayerListDepart(Player playerto, Player player)
        {
            playerto.Log.AddLog(LogType.FL_MSG, "tx FLPACKET_SERVER_PLAYERLIST playerid={0}", player.FLPlayerID);
            {
                byte[] omsg = {0x52, 0x02};
                FLMsgType.AddUInt32(ref omsg, 2); // command: 1 = new, 2 = depart
                FLMsgType.AddUInt32(ref omsg, player.FLPlayerID);
                FLMsgType.AddUInt8(ref omsg, 0);
                FLMsgType.AddUInt8(ref omsg, 0);
                playerto.SendMsgToClient(omsg);
            }
        }

        /// <summary>
        ///     Send an update to the player list when a player has joined the server or selected a char
        /// </summary>
        public static void SendPlayerListJoin(Player playerto, Player player, bool hide)
        {
            playerto.Log.AddLog(LogType.FL_MSG, "tx FLPACKET_SERVER_PLAYERLIST playerid={0}", player.FLPlayerID);
            // Player list and character name
            {
                byte[] omsg = {0x52, 0x02};
                FLMsgType.AddUInt32(ref omsg, 1); // 1 = new, 2 = depart
                FLMsgType.AddUInt32(ref omsg, player.FLPlayerID); // player id
                FLMsgType.AddUInt8(ref omsg, hide ? 1u : 0u); // hide 1 = yes, 0 = no
                FLMsgType.AddUnicodeStringLen8(ref omsg, player.Name + "\0");
                playerto.SendMsgToClient(omsg);
            }
        }

        /// <summary>
        ///     Send the current player list information to this player for
        ///     all player online.
        /// </summary>
        /// <param name="player"></param>
        public static void SendCompletePlayerList(Player player)
        {
            // FLPACKET_SERVER_PLAYERLIST
            player.Log.AddLog(LogType.FL_MSG, "tx FLPACKET_SERVER_PLAYERLIST");

            // Reset the player list. I'm not certain that this is necessary.
            {
                byte[] omsg = {0x52, 0x02};
                FLMsgType.AddUInt32(ref omsg, 0);
                FLMsgType.AddUInt32(ref omsg, 0);
                FLMsgType.AddUInt8(ref omsg, 0);
                FLMsgType.AddUInt8(ref omsg, 0);
                player.SendMsgToClient(omsg);
            }

            // Send player status to the player for all players including this one.
            foreach (DPGameRunner.PlayerListItem item in DPGameRunner.Playerlist.Values)
            {
                SendPlayerListJoin(player, item.Player, true);
                SendPlayerListUpdate(player, item);
            }
        }


        public static void SendSetVisitedState(Player player)
        {
            player.Log.AddLog(LogType.FL_MSG, "tx FLPACKET_COMMON_SET_VISITED_STATE");

            byte[] omsg = {0x13, 0x01};
            FLMsgType.AddInt32(ref omsg, 4 + (player.Visits.Count*5));
            FLMsgType.AddInt32(ref omsg, player.Visits.Count);
            foreach (var pi in player.Visits)
            {
                FLMsgType.AddUInt32(ref omsg, pi.Key);
                FLMsgType.AddUInt8(ref omsg, pi.Value);
            }

            player.SendMsgToClient(omsg);
        }

        public static void SendSetMissionLog(Player player)
        {
            player.Log.AddLog(LogType.FL_MSG, "tx FLPACKET_COMMON_SET_MISSION_LOG");

            byte[] omsg = {0x19, 0x01};
            FLMsgType.AddUInt32(ref omsg, 8); // cnt * 4 
            FLMsgType.AddUInt32(ref omsg, 1); // 1 
            FLMsgType.AddUInt32(ref omsg, 0); // 0
            player.SendMsgToClient(omsg);
        }

        public static void SendSetInterfaceState(Player player)
        {
            player.Log.AddLog(LogType.FL_MSG, "tx FLPACKET_COMMON_SET_INTERFACE_STATE");

            byte[] omsg = {0x1c, 0x01};
            FLMsgType.AddUInt32(ref omsg, 1); // cnt
            FLMsgType.AddUInt32(ref omsg, 3); // state 3
            player.SendMsgToClient(omsg);
        }

        /// <summary>
        ///     Set the attitude and faction of an object with respect to this solar
        /// </summary>
        public static void SendSetReputation(Player player, Object.Solar.Solar solar)
        {
            float attitude = 0;
            if (solar.Faction.FactionID != 0xFFFFFFFF)
            {
                attitude = player.Ship.GetAttitudeTowardsFaction(solar.Faction);
            }

            player.Log.AddLog(LogType.FL_MSG,
                "tx FLPACKET_SERVER_SETREPUTATION solar.objid={0} faction={1} attitude={2}",
                solar.Objid, solar.Faction.FactionID, attitude);

            byte[] omsg = {0x29, 0x02, 0x01};
            FLMsgType.AddUInt32(ref omsg, solar.Objid);
            FLMsgType.AddUInt32(ref omsg, solar.Faction.FactionID);
            FLMsgType.AddFloat(ref omsg, attitude);
            player.SendMsgToClient(omsg);
        }

        /// <summary>
        ///     Set the attitude and faction of an object with respect to this player's ship
        /// </summary>
        public static void SendSetReputation(Player player, Old.Object.Ship.Ship ship)
        {
            byte[] omsg = {0x29, 0x02};
            // If the ship doesn't know about this faction then add it
            // with the default faction affiliaton from the initialworld
            // settings
            if (!player.Ship.Reps.ContainsKey(ship.faction))
                player.Ship.Reps[ship.faction] = 0.0f; //fixme solar.faction.default_rep;

            float attitude = player.Ship.Reps[ship.faction];

            player.Log.AddLog(LogType.FL_MSG,
                "tx FLPACKET_SERVER_SETREPUTATION solar.objid={0} faction={1} attitude={2}",
                ship.Objid, ship.faction.Nickname, attitude);

            FLMsgType.AddUInt8(ref omsg, 0x01);
            FLMsgType.AddUInt32(ref omsg, ship.Objid);
            FLMsgType.AddUInt32(ref omsg, ship.faction.FactionID);
            FLMsgType.AddFloat(ref omsg, attitude);
            player.SendMsgToClient(omsg);
        }

        /// <summary>
        /// </summary>
        /// <param name="player"></param>
        public static void SendInitSetReputation(Player player)
        {
            byte[] omsg = {0x29, 0x02};
            player.Log.AddLog(LogType.FL_MSG, "tx FLPACKET_SERVER_SETREPUTATION self");
            FLMsgType.AddUInt8(ref omsg, 0x01);
            FLMsgType.AddUInt32(ref omsg, player.FLPlayerID);
            FLMsgType.AddUInt32(ref omsg, 0);
            FLMsgType.AddFloat(ref omsg, 0); // rep
            player.SendMsgToClient(omsg);
        }

        public static void SendPopupDialog(Player player, FLFormatString caption, FLFormatString message,
            Player.PopupDialogButtons buttons)
        {
            byte[] omsg = {0x1B, 0x01};
            player.Log.AddLog(LogType.FL_MSG, "tx FLPACKET_COMMON_POP_UP_DIALOG");
            FLMsgType.AddArray(ref omsg, caption.GetBytes());
            FLMsgType.AddArray(ref omsg, message.GetBytes());
            FLMsgType.AddUInt32(ref omsg, (uint) buttons);
            player.SendMsgToClient(omsg);
        }

        /// <summary>
        ///     Send a chat message to the client.
        /// </summary>
        /// <param name="player"></param>
        /// <param name="command"></param>
        public static void SendChat(Player player, byte[] command)
        {
            byte[] omsg = {0x05, 0x01};
            FLMsgType.AddInt32(ref omsg, command.Length);
            FLMsgType.AddArray(ref omsg, command);
            FLMsgType.AddUInt32(ref omsg, 0);
            FLMsgType.AddUInt32(ref omsg, 0);
            player.SendMsgToClient(omsg);
        }

        public static void SendChatCommand(Player player, Player.ChatCommand command, uint playerId)
        {
            byte[] omsg = {0x05, 0x01, 0x08, 0x00, 0x00, 0x00};

            FLMsgType.AddUInt32(ref omsg, (uint) command);
            FLMsgType.AddUInt32(ref omsg, playerId);
            FLMsgType.AddUInt32(ref omsg, player.FLPlayerID);
            FLMsgType.AddUInt32(ref omsg, 0x10004);

            player.SendMsgToClient(omsg);
        }

        /// <summary>
        ///     Send an infocard update to the client using the dsace protocol.
        /// </summary>
        /// <param name="player"></param>
        /// <param name="id"></param>
        /// <param name="text"></param>
        public static void SendInfocardUpdate(Player player, uint id, string text)
        {
            var isrc = new byte[0];
            FLMsgType.AddUInt32(ref isrc, 0); // reset all cards flag
            FLMsgType.AddUInt32(ref isrc, 1); // number of infocards
            FLMsgType.AddUInt32(ref isrc, id); // id number
            FLMsgType.AddUnicodeStringLen32(ref isrc, text); // unicode text array with size as uint

            // Compress the infocard list
            byte[] idest;
            using (var ms = new MemoryStream())
            {
                using (var zs = new ZlibStream(ms, CompressionMode.Compress))
                    zs.Write(isrc, 0, isrc.Length);
                idest = ms.ToArray();
            }

            // Pack the compressed infocards into the dsac command.
            var command = new byte[0];
            FLMsgType.AddUInt32(ref command, 0xD5AC);
            FLMsgType.AddUInt32(ref command, 0x01);
            FLMsgType.AddUInt32(ref command, (uint) idest.Length);
            FLMsgType.AddUInt32(ref command, (uint) isrc.Length);
            FLMsgType.AddArray(ref command, idest);
            SendChat(player, command);
        }

        /// <summary>
        ///     FLPACKET_SERVER_CHARSELECTVERIFIED
        /// </summary>
        /// <param name="player"></param>
        public static void SendCharSelectVerified(Player player)
        {
            player.Log.AddLog(LogType.FL_MSG, "tx FLPACKET_SERVER_CHARSELECTVERIFIED");

            byte[] omsg = {0x08, 0x02};
            FLMsgType.AddUInt32(ref omsg, player.FLPlayerID);
            FLMsgType.AddDouble(ref omsg, player.Runner.GameTime());
            player.SendMsgToClient(omsg);
        }

        /// <summary>
        ///     FLPACKET_COMMON_REQUEST_PLAYER_STATS
        /// </summary>
        /// <param name="player"></param>
        public static void SendPlayerStats(Player player)
        {
            player.Log.AddLog(LogType.FL_MSG, "tx FLPACKET_COMMON_REQUEST_PLAYER_STATS");

            byte[] omsg = {0x18, 0x01};
            FLMsgType.AddInt32(ref omsg, (13*4) + (player.Ship.Reps.Count*8) + (player.Kills.Count*8)); //

            FLMsgType.AddUInt32(ref omsg, 4); // rm_completed
            FLMsgType.AddUInt32(ref omsg, 0); // u_dword
            FLMsgType.AddUInt32(ref omsg, 2); // rm_failed
            FLMsgType.AddUInt32(ref omsg, 0); // u_dword
            FLMsgType.AddFloat(ref omsg, 10000.0f); // total_time_played
            FLMsgType.AddUInt32(ref omsg, 6); // systems_visited
            FLMsgType.AddUInt32(ref omsg, 5); // bases_visited
            FLMsgType.AddUInt32(ref omsg, 4); // holes_visited
            FLMsgType.AddInt32(ref omsg, player.Kills.Count); // kills_cnt
            FLMsgType.AddUInt32(ref omsg, player.Ship.Rank); // rank
            FLMsgType.AddUInt32(ref omsg, (UInt32) player.Money); // current_worth
            FLMsgType.AddUInt32(ref omsg, 0); // dunno
            FLMsgType.AddInt32(ref omsg, player.Ship.Reps.Count);
            foreach (var pi in player.Kills)
            {
                FLMsgType.AddUInt32(ref omsg, pi.Key);
                FLMsgType.AddUInt32(ref omsg, pi.Value);
            }
            foreach (var pi in player.Ship.Reps)
            {
                FLMsgType.AddUInt32(ref omsg, pi.Key.FactionID);
                FLMsgType.AddFloat(ref omsg, pi.Value);
            }

            player.SendMsgToClient(omsg);
        }

        /// <summary>
        ///     FLPACKET_SERVER_SETSTARTROOM
        /// </summary>
        /// <param name="player"></param>
        /// <param name="baseid"></param>
        /// <param name="roomid"></param>
        public static void SendSetStartRoom(Player player, uint baseid, uint roomid)
        {
            player.Log.AddLog(LogType.FL_MSG, "tx FLPACKET_SERVER_SETSTARTROOM");
            byte[] omsg = {0x0d, 0x02};
            FLMsgType.AddUInt32(ref omsg, baseid);
            FLMsgType.AddUInt32(ref omsg, roomid);
            player.SendMsgToClient(omsg);
        }

        /// <summary>
        ///     FLPACKET_SERVER_GFMISSIONVENDORWHYEMPTY
        /// </summary>
        /// <param name="player"></param>
        public static void SendGFMissionVendorWhyEmpty(Player player)
        {
            player.Log.AddLog(LogType.FL_MSG, "tx FLPACKET_SERVER_GFMISSIONVENDORWHYEMPTY");
            byte[] omsg = {0x5a, 0x02};
            FLMsgType.AddUInt8(ref omsg, 0); // reason
            player.SendMsgToClient(omsg);
        }

        /// <summary>
        ///     FLPACKET_SERVER_GFCOMPLETEMISSIONCOMPUTERLIST
        /// </summary>
        /// <param name="player"></param>
        /// <param name="baseid"></param>
        public static void SendGFCompleteMissionComputerList(Player player, uint baseid)
        {
            player.Log.AddLog(LogType.FL_MSG, "tx FLPACKET_SERVER_GFCOMPLETEMISSIONCOMPUTERLIST");

            byte[] omsg = {0x10, 0x02};
            FLMsgType.AddUInt32(ref omsg, baseid);
            player.SendMsgToClient(omsg);
        }

        /// <summary>
        ///     FLPACKET_SERVER_GFCOMPLETENEWSBROADCASTLIST
        /// </summary>
        /// <param name="player"></param>
        /// <param name="baseid"></param>
        public static void SendGFCompleteNewsBroadcastList(Player player, uint baseid)
        {
            player.Log.AddLog(LogType.FL_MSG, "tx FLPACKET_SERVER_NEWS");
            BaseData bd = UniverseDB.FindBase(baseid);

            uint newsid = 0;
            foreach (NewsItem ni in bd.News)
            {
                byte[] omsg = {0x1E, 0x02};
                FLMsgType.AddUInt32(ref omsg, 40 + (uint) ni.Logo.Length); // size goes here

                FLMsgType.AddUInt32(ref omsg, newsid++);
                FLMsgType.AddUInt32(ref omsg, baseid);
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

                player.SendMsgToClient(omsg);
            }

            // Send "news list complete" message
            {
                byte[] omsg = {0x0e, 0x02};
                FLMsgType.AddUInt32(ref omsg, baseid);
                player.SendMsgToClient(omsg);
            }
        }

        public static void SendPlayerGFUpdateChar(Player player, uint roomid, uint charid)
        {
            byte[] omsg = {0x26, 0x02};

            const string movementScript = "scripts\\extras\\player_fidget.thn";
            const string roomLocation = "";

            FLMsgType.AddUInt32(ref omsg,
                74 + (uint) player.Name.Length + (uint) movementScript.Length + (uint) roomLocation.Length);
            FLMsgType.AddUInt32(ref omsg, charid);
            FLMsgType.AddUInt32(ref omsg, 0x01); // 1 = player, 0 = npc
            FLMsgType.AddUInt32(ref omsg, roomid);
            FLMsgType.AddUInt32(ref omsg, 0); // npc name
            FLMsgType.AddUInt32(ref omsg, player.Ship.faction.FactionID); // faction
            FLMsgType.AddUnicodeStringLen32(ref omsg, player.Name);
            FLMsgType.AddUInt32(ref omsg, player.Ship.com_head);
            FLMsgType.AddUInt32(ref omsg, player.Ship.com_body);
            FLMsgType.AddUInt32(ref omsg, player.Ship.com_lefthand);
            FLMsgType.AddUInt32(ref omsg, player.Ship.com_righthand);
            FLMsgType.AddUInt32(ref omsg, 0); // accessories count + list
            FLMsgType.AddAsciiStringLen32(ref omsg, movementScript);
            FLMsgType.AddInt32(ref omsg, -1); // behaviourid

            if (roomLocation.Length == 0)
                FLMsgType.AddInt32(ref omsg, -1);
            else
                FLMsgType.AddAsciiStringLen32(ref omsg, roomLocation);

            FLMsgType.AddUInt32(ref omsg, 0x01); // 1 = player, 0 = npc
            FLMsgType.AddUInt32(ref omsg, 0x00); // 1 = sitlow, 0 = stand
            FLMsgType.AddUInt32(ref omsg, player.Ship.voiceid); // voice

            player.SendMsgToClient(omsg);
        }

        public static void SendNPCGFUpdateChar(Player player, BaseCharacter ch, uint charid)
        {
            byte[] omsg = {0x26, 0x02};

            FLMsgType.AddUInt32(ref omsg, 68 + (uint) ch.FidgetScript.Length + (uint) ch.RoomLocation.Length);
            FLMsgType.AddUInt32(ref omsg, charid);
            FLMsgType.AddUInt32(ref omsg, 0); // 1 = player, 0 = npc
            FLMsgType.AddUInt32(ref omsg, ch.RoomID);
            FLMsgType.AddUInt32(ref omsg, ch.IndividualName); // npc name
            FLMsgType.AddUInt32(ref omsg, ch.Faction.FactionID); // faction
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

            player.SendMsgToClient(omsg);
        }

        public static void SendGFCompleteCharList(Player player, uint roomid)
        {
            player.Log.AddLog(LogType.FL_MSG, "tx FLPACKET_SERVER_GFCOMPLETECHARLIST");

            // Send the player 'trent' character
            uint charid = 1;
            SendPlayerGFUpdateChar(player, roomid, charid++);

            // Send the barman, dealers, etc. These have fixed locations and
            // fidget scripts
            foreach (BaseCharacter ch in player.Ship.Basedata.Chars.Values)
            {
                if (ch.RoomID == roomid && ch.Type != null)
                {
                    SendNPCGFUpdateChar(player, ch, charid);
                    SendNPCGFUpdateScripts(player, ch, charid);
                    charid++;
                }
            }

            // Send the barflies

            // Send "char list complete message"
            {
                byte[] omsg = {0x0f, 0x02};
                FLMsgType.AddUInt32(ref omsg, roomid);
                player.SendMsgToClient(omsg);
            }
        }

        public static void SendNPCGFUpdateScripts(Player player, BaseCharacter npc, uint charid)
        {
            List<string> scripts = News.GetScriptsForNPCInteraction(player, npc);

            //int scriptSize = 0;
            //foreach (string script in scripts)
            //scriptSize += 8 + script.Length;
            int scriptSize = scripts.Sum(script => 8 + script.Length);

            byte[] omsg = {0x1A, 0x02};
            FLMsgType.AddUInt32(ref omsg, 56 + (uint) scriptSize); // size
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

            player.SendMsgToClient(omsg);
        }

        public static void SendGFCompleteScriptBehaviourList(Player player, uint roomid)
        {
            player.Log.AddLog(LogType.FL_MSG, "tx FLPACKET_SERVER_GFCOMPLETESCRIPTBEHAVIORLIST");

            {
                byte[] omsg = {0x11, 0x02};
                FLMsgType.AddUInt32(ref omsg, roomid);
                player.SendMsgToClient(omsg);
            }
        }

        public static void SendGFCompleteAmbientScriptList(Player player, uint roomid)
        {
            player.Log.AddLog(LogType.FL_MSG, "tx FLPACKET_SERVER_GFCOMPLETEAMBIENTSCRIPTLIST");

            byte[] omsg = {0x13, 0x02};
            FLMsgType.AddUInt32(ref omsg, roomid);
            player.SendMsgToClient(omsg);
        }

        /// <summary>
        ///     FLPACKET_SERVER_SETADDITEM
        /// </summary>
        /// <param name="player"></param>
        /// <param name="goodid"></param>
        /// <param name="hpid"></param>
        /// <param name="count"></param>
        /// <param name="health"></param>
        /// <param name="mounted"></param>
        /// <param name="hpname"></param>
        public static void SendAddItem(Player player, uint goodid, uint hpid, uint count, float health, bool mounted,
            string hpname)
        {
            player.Log.AddLog(LogType.FL_MSG,
                "tx FLPACKET_SERVER_SETADDITEM goodid={0} hpid={1} count={2} health={3} mounted={4} hpname={5}",
                goodid, hpid, count, health, mounted, hpname);

            byte[] omsg = {0x2E, 0x02};
            FLMsgType.AddUInt32(ref omsg, goodid);
            FLMsgType.AddUInt32(ref omsg, hpid);
            FLMsgType.AddUInt32(ref omsg, count);
            FLMsgType.AddFloat(ref omsg, health);
            FLMsgType.AddUInt32(ref omsg, (mounted ? 1u : 0u));
            FLMsgType.AddUInt16(ref omsg, 0);
            if (hpname.Length > 0)
                FLMsgType.AddAsciiStringLen32(ref omsg, hpname + "\0");
            else if (mounted)
                FLMsgType.AddAsciiStringLen32(ref omsg, "");
            else
                FLMsgType.AddAsciiStringLen32(ref omsg, "BAY\0");

            player.SendMsgToClient(omsg);
        }

        /// <summary>
        ///     FLPACKET_SERVER_SETREMOVEITEM
        /// </summary>
        /// <param name="player"></param>
        /// <param name="item"></param>
        public static void SendRemoveItem(Player player, ShipItem item)
        {
            player.Log.AddLog(LogType.FL_MSG, "tx FLPACKET_SERVER_SETREMOVEITEM hpid={0}", item.hpid);
            byte[] omsg = {0x2F, 0x02};
            FLMsgType.AddUInt32(ref omsg, item.hpid);
            FLMsgType.AddUInt32(ref omsg, 0);
            FLMsgType.AddUInt32(ref omsg, 0);
            player.SendMsgToClient(omsg);
        }

        /// <summary>
        ///     FLPACKET_SERVER_SETEQUIPMENT
        /// </summary>
        /// <param name="player"></param>
        /// <param name="items"></param>
        public static void SendSetEquipment(Player player, Dictionary<uint, ShipItem> items)
        {
            player.Log.AddLog(LogType.FL_MSG, "tx FLPACKET_SERVER_SETEQUIPMENT count={0}", items.Count);
            byte[] omsg = {0x24, 0x02};
            FLMsgType.AddUInt16(ref omsg, (uint) items.Count);
            foreach (ShipItem item in items.Values)
            {
                FLMsgType.AddUInt32(ref omsg, item.count);
                FLMsgType.AddFloat(ref omsg, item.health);
                FLMsgType.AddUInt32(ref omsg, item.arch.ArchetypeID);
                FLMsgType.AddUInt16(ref omsg, item.hpid);
                FLMsgType.AddUInt16(ref omsg, (item.mounted ? 1u : 0u));
                if (item.hpname.Length > 0)
                    FLMsgType.AddAsciiStringLen16(ref omsg, item.hpname + "\0");
                else if (item.mounted)
                    FLMsgType.AddAsciiStringLen16(ref omsg, "");
                else
                    FLMsgType.AddAsciiStringLen16(ref omsg, "BAY\0");
            }
            player.SendMsgToClient(omsg);
        }

        /// <summary>
        ///     FLPACKET_SERVER_SETCASH
        /// </summary>
        /// <param name="player"></param>
        public static void SendSetMoney(Player player)
        {
            player.Log.AddLog(LogType.FL_MSG, "tx FLPACKET_SERVER_SETCASH money={0}", player.Money);
            byte[] omsg = {0x30, 0x02};
            if (player.Money > 2000000000)
                FLMsgType.AddInt32(ref omsg, 2000000000);
            else
                FLMsgType.AddInt32(ref omsg, player.Money);
            player.SendMsgToClient(omsg);
        }

        /// <summary>
        ///     FLPACKET_SERVER_REQUESTCREATESHIPRESP
        /// </summary>
        /// <param name="playerto"></param>
        /// <param name="ship"></param>
        public static void SendCreateShipResponse(Player playerto, Old.Object.Ship.Ship ship)
        {
            playerto.Log.AddLog(LogType.FL_MSG, "tx FLPACKET_SERVER_REQUESTCREATESHIPRESP objid={0}", ship.Objid);
            byte[] omsg = {0x27, 0x02};
            FLMsgType.AddUInt8(ref omsg, 1); // dunno
            FLMsgType.AddUInt32(ref omsg, ship.Objid);
            playerto.SendMsgToClient(omsg);
        }

        /// <summary>
        ///     FLPACKET_SERVER_LAUNCH
        /// </summary>
        /// <param name="player"></param>
        public static void SendServerLaunch(Player player)
        {
            {
                Vector eulerRot = Matrix.MatrixToEulerDeg(player.Ship.Orientation);
                player.Log.AddLog(LogType.FL_MSG, "tx FLPACKET_SERVER_SERVERLAUNCH objid={0} position={1} orient={2}",
                    player.Ship.Objid, player.Ship.Position, eulerRot);

                // If we're spawning in a base solar, send the solar id
                if (player.Ship.CurrentAction is LaunchFromBaseAction)
                {
                    var action = player.Ship.CurrentAction as LaunchFromBaseAction;

                    player.Ship.Position = action.Position;
                    player.Ship.Orientation = Quaternion.QuaternionToMatrix(action.Orientation);

                    byte[] omsg = {0x07, 0x02};
                    FLMsgType.AddUInt32(ref omsg, player.Ship.Objid);
                    FLMsgType.AddUInt32(ref omsg, action.DockingObj.Solar.Objid);
                    FLMsgType.AddUInt32(ref omsg, action.DockingObj.Index);
                    FLMsgType.AddFloat(ref omsg, (float) player.Ship.Position.x);
                    FLMsgType.AddFloat(ref omsg, (float) player.Ship.Position.y);
                    FLMsgType.AddFloat(ref omsg, (float) player.Ship.Position.z);
                    Quaternion q = Quaternion.MatrixToQuaternion(player.Ship.Orientation);
                    FLMsgType.AddFloat(ref omsg, (float) q.W);
                    FLMsgType.AddFloat(ref omsg, (float) q.I);
                    FLMsgType.AddFloat(ref omsg, (float) q.J);
                    FLMsgType.AddFloat(ref omsg, (float) q.K);
                    player.SendMsgToClient(omsg);

                    action.DockingObj.Activate(player.Runner, player.Ship);
                }
                    // Otherwise we're spawning in a space or at a jump/moor point
                else if (player.Ship.CurrentAction is LaunchInSpaceAction)
                {
                    var action = player.Ship.CurrentAction as LaunchInSpaceAction;

                    player.Ship.Position = action.Position;
                    player.Ship.Orientation = Quaternion.QuaternionToMatrix(action.Orientation);

                    byte[] omsg = {0x07, 0x02};
                    FLMsgType.AddUInt32(ref omsg, player.Ship.Objid);
                    FLMsgType.AddUInt32(ref omsg, 0);
                    FLMsgType.AddInt32(ref omsg, -1);
                    FLMsgType.AddFloat(ref omsg, (float) player.Ship.Position.x);
                    FLMsgType.AddFloat(ref omsg, (float) player.Ship.Position.y);
                    FLMsgType.AddFloat(ref omsg, (float) player.Ship.Position.z);
                    FLMsgType.AddFloat(ref omsg, (float) action.Orientation.W);
                    FLMsgType.AddFloat(ref omsg, (float) action.Orientation.I);
                    FLMsgType.AddFloat(ref omsg, (float) action.Orientation.J);
                    FLMsgType.AddFloat(ref omsg, (float) action.Orientation.K);
                    player.SendMsgToClient(omsg);
                }
            }

            {
                byte[] omsg = {0x54, 0x02, 0x09, 0x00}; // flag (objid + dunno)
                FLMsgType.AddUInt32(ref omsg, player.Ship.Objid);
                FLMsgType.AddUInt32(ref omsg, 0); // dunnno
                player.SendMsgToClient(omsg);
            }

            {
                byte[] omsg = {0x54, 0x02, 0x28, 0x00}; // flag (faction + objid)
                FLMsgType.AddUInt32(ref omsg, player.Ship.Objid);
                FLMsgType.AddFloat(ref omsg, 0); // faction
                player.SendMsgToClient(omsg);
            }


            player.Ship.Basedata = null;
        }

        public static void SendServerRequestReturned(Player player, Old.Object.Ship.Ship ship, DockingObject dockingObj)
        {
            player.Log.AddLog(LogType.FL_MSG, "tx FLPACKET_SERVER_REQUEST_RETURNED");

            byte[] omsg = {0x44, 0x02};
            FLMsgType.AddUInt32(ref omsg, ship.Objid);
            if (dockingObj != null)
            {
                if (dockingObj.Type == DockingPoint.DockingSphere.TRADELANE_RING)
                    FLMsgType.AddUInt8(ref omsg, 2); // type? 0 is used for docking, 1 for something? else
                else
                    FLMsgType.AddUInt8(ref omsg, 0); // type? 0 is used for docking, 1 for something? else

                FLMsgType.AddUInt8(ref omsg, 4); // 4 = dock, 3 = wait, 5 = denied?
                FLMsgType.AddUInt8(ref omsg, dockingObj.Index); // docking point
            }
            else
            {
                FLMsgType.AddUInt8(ref omsg, 0); // type? 0 is used for docking, 1 for something? else
                FLMsgType.AddUInt8(ref omsg, 0);
                // Response: 5 is dock, 0 is denied target hostile, 2 is denied too big (hostile takes priority), 3 is queue, 4 is proceed after queue; 0, 1 don't actually give a message, 2 gives a generic "denied" message
                FLMsgType.AddUInt8(ref omsg, 255); // docking point
            }
            player.SendMsgToClient(omsg);
        }

        /// <summary>
        ///     FLPACKET_SERVER_USE_ITEM
        /// </summary>
        /// <param name="player"></param>
        /// <param name="objid"></param>
        /// <param name="hpid"></param>
        /// <param name="count"></param>
        public static void SendUseItem(Player player, uint objid, uint hpid, uint count)
        {
            player.Log.AddLog(LogType.FL_MSG, "tx[{0}] FLPACKET_SERVER_USE_ITEM objid={1} hpid={2} count={3}",
                player.FLPlayerID,
                objid, hpid, count);

            byte[] omsg = {0x51, 0x02};
            FLMsgType.AddUInt32(ref omsg, objid);
            FLMsgType.AddUInt16(ref omsg, hpid);
            FLMsgType.AddUInt16(ref omsg, count);
            player.SendMsgToClient(omsg);
        }

        /// <summary>
        ///     FLPACKET_SERVER_LAND
        /// </summary>
        /// <param name="player"></param>
        /// <param name="ship"></param>
        /// <param name="solarid"></param>
        /// <param name="baseid"></param>
        public static void SendServerLand(Player player, Old.Object.Ship.Ship ship, uint solarid, uint baseid)
        {
            player.Log.AddLog(LogType.FL_MSG, "tx FLPACKET_SERVER_LAND objid={0} targetid={1} baseid={2}", ship.Objid,
                solarid,
                baseid);

            byte[] omsg = {0x0B, 0x02};
            FLMsgType.AddUInt32(ref omsg, ship.Objid);
            FLMsgType.AddUInt32(ref omsg, solarid);
            FLMsgType.AddUInt32(ref omsg, baseid);
            player.SendMsgToClient(omsg);
        }

        /// <summary>
        ///     FLPACKET_SERVER_SYSTEM_SWITCH_OUT
        /// </summary>
        /// <param name="player"></param>
        /// <param name="ship"></param>
        /// <param name="solar"></param>
        public static void SendSystemSwitchOut(Player player, Old.Object.Ship.Ship ship, Object.Solar.Solar solar)
        {
            player.Log.AddLog(LogType.FL_MSG, "tx FLPACKET_SERVER_SYSTEM_SWITCH_OUT objid={0} solar={1}", ship.Objid,
                solar.Objid);

            byte[] omsg = {0x21, 0x02};
            FLMsgType.AddUInt32(ref omsg, ship.Objid);
            FLMsgType.AddUInt32(ref omsg, solar.Objid);
            player.SendMsgToClient(omsg);
        }

        /// <summary>
        ///     FLPACKET_SERVER_SYSTEM_SWITCH_IN
        /// Send when client arrives to the system.
        /// </summary>
        /// <param name="player"></param>
        /// <param name="ship"></param>
        public static void SendSystemSwitchIn(Player player, Old.Object.Ship.Ship ship)
        {
            player.Log.AddLog(LogType.FL_MSG, "tx FLPACKET_SERVER_SYSTEM_SWITCH_IN objid={0}", ship.Objid);

            byte[] omsg = {0x22, 0x02};
            FLMsgType.AddUInt32(ref omsg, ship.Objid);
            FLMsgType.AddUInt32(ref omsg, 64);
            FLMsgType.AddFloat(ref omsg, (float) ship.Position.x);
            FLMsgType.AddFloat(ref omsg, (float) ship.Position.y);
            FLMsgType.AddFloat(ref omsg, (float) ship.Position.z);

            Quaternion q = Quaternion.MatrixToQuaternion(ship.Orientation);
            FLMsgType.AddFloat(ref omsg, (float) q.W);
            FLMsgType.AddFloat(ref omsg, (float) q.I);
            FLMsgType.AddFloat(ref omsg, (float) q.J);
            FLMsgType.AddFloat(ref omsg, (float) q.K);

            player.SendMsgToClient(omsg);

            SendMiscObjUpdate(player, Player.MiscObjUpdateType.SYSTEM, player.FLPlayerID, player.Ship.System.SystemID);

            SendMiscObjUpdate(player, Player.MiscObjUpdateType.UNK3, player.Ship.Objid);
            SendMiscObjUpdate(player, Player.MiscObjUpdateType.UNK2, player.Ship.Objid);
        }

        /// <summary>
        ///     FLPACKET_SERVER_SETHULLSTATUS
        /// </summary>
        /// <param name="playerto"></param>
        /// <param name="ship"></param>
        public static void SendSetHullStatus(Player playerto, Old.Object.Ship.Ship ship)
        {
            playerto.Log.AddLog(LogType.FL_MSG, "tx FLPACKET_SERVER_SETHULLSTATUS objid={0}", ship.Objid);

            byte[] omsg = {0x49, 0x02};
            FLMsgType.AddFloat(ref omsg, ship.Health);
            playerto.SendMsgToClient(omsg);
        }

        /// <summary>
        ///     FLPACKET_SERVER_CREATELOOT
        /// </summary>
        /// <param name="player"></param>
        /// <param name="parentShip"></param>
        /// <param name="loot"></param>
        public static void SendCreateLoot(Player player, Old.Object.Ship.Ship parentShip, Old.Object.Loot loot)
        {
            player.Log.AddLog(LogType.FL_MSG, "tx FLPACKET_SERVER_CREATELOOT parent objid={0} loot objid={1}",
                parentShip.Objid,
                loot.Objid);

            byte[] omsg = {0x28, 0x02};
            FLMsgType.AddUInt32(ref omsg, parentShip.Objid); //TODO: Figure out if parent is necessary / what it means
            FLMsgType.AddInt8(ref omsg, 5); //TODO: Reverse meaning
            FLMsgType.AddInt8(ref omsg, 1); //Array size seems to always be one, needs further investigation
            //Array starts, length is count above
            FLMsgType.AddUInt32(ref omsg, loot.Objid);
            FLMsgType.AddUInt16(ref omsg, loot.LootContent.SmallID);
            FLMsgType.AddFloat(ref omsg, loot.LootContentHealth*loot.LootContent.HitPts);
            FLMsgType.AddUInt16(ref omsg, loot.LootContentQuantity);
            FLMsgType.AddUInt16(ref omsg, loot.Arch.SmallID); //Usually a loot crate
            FLMsgType.AddFloat(ref omsg, loot.Health*loot.Arch.HitPts);
            FLMsgType.AddFloat(ref omsg, 0.0f); //TODO: Reverse meaning
            FLMsgType.AddFloat(ref omsg, (float) loot.Position.x);
            FLMsgType.AddFloat(ref omsg, (float) loot.Position.y);
            FLMsgType.AddFloat(ref omsg, (float) loot.Position.z);
            FLMsgType.AddUInt8(ref omsg, loot.MissionFlag1 ? 1u : 0u);
            FLMsgType.AddUInt8(ref omsg, loot.MissionFlag2 ? 1u : 0u);
            player.SendMsgToClient(omsg);
        }

        /// <summary>
        ///     Process a charinforequest from a player.
        /// </summary>
        /// <param name="player"></param>
        public static void SendCharInfoRequestResponse(Player player)
        {
            List<Account> accs = Database.GetAccount(player.AccountID);
            //string accdir_path = Runner.Server.AcctPath + Path.DirectorySeparatorChar +
            //FLMsgType.FLNameToFile(AccountID);
            try
            {
                byte[] omsg = {0x03, 0x02};
                FLMsgType.AddUInt8(ref omsg, 0); // chars
                //foreach (var path in Directory.GetFiles(accdir_path, "??-????????.fl"))
                if (accs != null)
                    foreach (Account acct in accs)
                    {
                        try
                        {
                            var dummy = new CharacterData {Ship = new Old.Object.Ship.Ship(null)};

                            // If the charfile is not valid ignore it.
                            string result = dummy.LoadCharFile(acct, player.Log);
                            if (result != null)
                            {
                                player.Log.AddLog(LogType.ERROR, "error: " + result);
                                continue;
                            }

                            FLMsgType.AddAsciiStringLen16(ref omsg, FLMsgType.FLNameToFile(acct.CharName));
                            FLMsgType.AddUInt16(ref omsg, 0);
                            FLMsgType.AddUnicodeStringLen16(ref omsg, acct.CharName);
                            FLMsgType.AddUnicodeStringLen16(ref omsg, "");
                            FLMsgType.AddUInt32(ref omsg, 0);
                            FLMsgType.AddUInt32(ref omsg, 0);
                            FLMsgType.AddUInt32(ref omsg, 0);
                            FLMsgType.AddUInt32(ref omsg, acct.Ship);
                            if (dummy.Money > 2000000000)
                                FLMsgType.AddInt32(ref omsg, 2000000000);
                            else
                                FLMsgType.AddInt32(ref omsg, acct.Money);

                            FLMsgType.AddUInt32(ref omsg, dummy.Ship.System.SystemID);
                            if (dummy.Ship.Basedata != null)
                                FLMsgType.AddUInt32(ref omsg, dummy.Ship.Basedata.BaseID);
                            else
                                FLMsgType.AddUInt32(ref omsg, 0);
                            FLMsgType.AddUInt32(ref omsg, 0);
                            FLMsgType.AddUInt32(ref omsg, dummy.Ship.voiceid);
                            FLMsgType.AddUInt32(ref omsg, dummy.Ship.Rank);
                            FLMsgType.AddUInt32(ref omsg, 0);
                            FLMsgType.AddFloat(ref omsg, dummy.Ship.Health);
                            FLMsgType.AddUInt32(ref omsg, 0);
                            FLMsgType.AddUInt32(ref omsg, 0);
                            FLMsgType.AddUInt32(ref omsg, 0);

                            FLMsgType.AddUInt8(ref omsg, 1);
                            FLMsgType.AddUInt32(ref omsg, dummy.Ship.com_body);
                            FLMsgType.AddUInt32(ref omsg, dummy.Ship.com_head);
                            FLMsgType.AddUInt32(ref omsg, dummy.Ship.com_lefthand);
                            FLMsgType.AddUInt32(ref omsg, dummy.Ship.com_righthand);
                            FLMsgType.AddUInt32(ref omsg, 0);

                            FLMsgType.AddUInt8(ref omsg, 1);
                            FLMsgType.AddUInt32(ref omsg, dummy.Ship.com_body);
                            FLMsgType.AddUInt32(ref omsg, dummy.Ship.com_head);
                            FLMsgType.AddUInt32(ref omsg, dummy.Ship.com_lefthand);
                            FLMsgType.AddUInt32(ref omsg, dummy.Ship.com_righthand);
                            FLMsgType.AddUInt32(ref omsg, 0);

                            FLMsgType.AddUInt8(ref omsg, (uint) dummy.Ship.Items.Count);
                            foreach (ShipItem item in dummy.Ship.Items.Values)
                            {
                                FLMsgType.AddUInt32(ref omsg, item.count);
                                FLMsgType.AddFloat(ref omsg, item.health);
                                FLMsgType.AddUInt32(ref omsg, item.arch.ArchetypeID);
                                FLMsgType.AddUInt16(ref omsg, item.hpid);
                                FLMsgType.AddUInt16(ref omsg, (item.mounted ? 1u : 0u));
                                if (item.hpname.Length > 0)
                                    FLMsgType.AddAsciiStringLen16(ref omsg, item.hpname + "\0");
                                else
                                    FLMsgType.AddAsciiStringLen16(ref omsg, "BAY\0");
                            }

                            FLMsgType.AddUInt32(ref omsg, 0);

                            omsg[2]++;
                        }
                        catch (Exception e)
                        {
                            player.Log.AddLog(LogType.ERROR, "error: corrupt file when processing charinforequest '{0}'",
                                e.Message);
                        }
                    }
                FLMsgType.AddUInt32(ref omsg, 0);
                player.SendMsgToClient(omsg);
            }
            catch (Exception e)
            {
                player.Log.AddLog(LogType.ERROR, "error: unable to process charinforequest '{0}'", e.Message);
            }
        }
    }
}