using System;
using System.Collections.Generic;
using System.Linq;
using FLServer.Object;
using FLServer.Object.Base;
using FLServer.Old.Object;
using FLServer.Old.Object.Ship;
using FLServer.Physics;
using FLServer.Player;
using FLServer.Server;
using FLServer.Ship;

namespace FLServer.Chat
{
    internal class Chat
    {
        public static void Process(Player.Player from, uint to, string chat)
        {
            if (ProcessAdminCommands(from, to, chat))
                return;

            if (ProcessUserCommands(from, to, chat))
                return;

            switch (to)
            {
                case 0: //console
                    break;
                case 0x10000: // universe chat
                    SendChatToUniverse(from, chat);
                    break;
                case 0x10001: // system chat
                    SendChatToLocal(from, chat);
                    break;
                case 0x10002: // local chat
                case 0x10003: // group chat
                    SendChatToGroup(from, chat);
                    break;
                    // ReSharper disable RedundantCaseLabel
                case 0x20000: // custom cmds
                case 0x20001:
                case 0x20010:
                case 0x20100:
                    // ReSharper restore RedundantCaseLabel
                default: // private chat
                    SendChatToPrivate(from, to, chat);
                    break;
            }
        }

        public static void SendChatToPlayer(Player.Player player, string chat)
        {
            var rdl = new Rdl();
            rdl.AddTRA(0xFFFFFF00, 0xFFFFFFFF);
            rdl.AddText(chat);
            SendChatToPlayer(player, rdl);
        }

        public static void SendEcho(Player.Player player, string chat)
        {
            var rdl = new Rdl();
            rdl.AddTRA(0x05FF3802, 0xFFFFFFFF);
            rdl.AddText(chat);
            SendChatToPlayer(player, rdl);
        }

        public static void SendChatToPlayer(Player.Player player, Rdl rdl)
        {
            byte[] omsg = {0x05, 0x01};
            FLMsgType.AddInt32(ref omsg, rdl.GetBytes().Length);
            FLMsgType.AddArray(ref omsg, rdl.GetBytes());
            FLMsgType.AddUInt32(ref omsg, 0);
            FLMsgType.AddUInt32(ref omsg, 0);
            player.SendMsgToClient(omsg);
        }

        #region "SendTo..."

        /// <summary>
        /// Sends text to everyone in scanrange.
        /// </summary>
        /// <param name="from"></param>
        /// <param name="chat"></param>
        public static void SendChatToLocal(Player.Player from, string chat)
        {
            var rdl = new Rdl();
            rdl.AddTRA(0xFFFFFF00, 0xFFFFFFFF);
            rdl.AddText(from.Name + ": ");
            rdl.AddTRA(0xFF8F4000, 0xFFFFFFFF);
            rdl.AddText(chat);
            var bu = new List<SimObject>(from.Ship.ScanBucket);
            foreach (var ship in @bu.OfType<Old.Object.Ship.Ship>().Where(ship => ship.player != null && ship.player != from))
            {
                SendChatToPlayer(ship.player, rdl);
            }
            SendChatToPlayer(from, rdl);
        }

        /// <summary>
        /// Sends text to everyone in system.
        /// </summary>
        /// <param name="from"></param>
        /// <param name="chat"></param>
        public static void SendChatToSystem(Player.Player from, string chat)
        {
            var rdl = new Rdl();
            rdl.AddTRA(0xFFFFFF00, 0xFFFFFFFF);
            rdl.AddText(from.Name + ": ");
            rdl.AddTRA(0xFFE95700, 0xFFFFFFFF);
            rdl.AddText(chat);
            foreach (var play in @from.Runner.Players.Where(play => play.Value.Ship.System == @from.Ship.System))
            {
                SendChatToPlayer(play.Value, rdl);
            }
        }

        public static void SendDeathMessage(Player.Player from, DeathCause cause)
        {
            //if (!from.Ship.IsDestroyed)
            //{
                //universe
                var rdl = new Rdl();
                rdl.AddTRA(0x19198C00, 0xFFFFFFFF);
                rdl.AddText(from.Name + " died because of " + cause);

                var rdlSystem = new Rdl();
                rdlSystem.AddTRA(0x0062FF01, 0xFFFFFFFF);
                rdlSystem.AddText(from.Name + " died because of " + cause);

                foreach (var player in DPGameRunner.Playerlist)
                {
                    if (player.Value.Player.Settings[@"senddeath"] && player.Value.System != from.Ship.System)
                    {
                        SendChatToPlayer(player.Value.Player, rdl);
                    }

                    if (player.Value.Player.Settings[@"sendsystemdeath"] && player.Value.System == from.Ship.System)
                    {
                        SendChatToPlayer(player.Value.Player, rdlSystem);
                    }

                }

            //}
        }

        public static void SendChatToUniverse(Player.Player from, string chat)
        {
            var rdl = new Rdl();
            rdl.AddTRA(0xFFFFFF00, 0xFFFFFFFF);
            rdl.AddText(from.Name + ": ");
            rdl.AddTRA(0x7C7C7C00, 0xFFFFFFFF);
            rdl.AddText(chat);
            foreach (var play in DPGameRunner.Playerlist)
            {
                SendChatToPlayer(play.Value.Player, rdl);
            }
        }

        public static void SendChatToGroup(Player.Player from, string chat)
        {
            if (from.Group == null)
            {
                return;
            }

            var rdl = new Rdl();
            rdl.AddTRA(0xFFFFFF00, 0xFFFFFFFF);
            rdl.AddText(from.Name + ": ");
            rdl.AddTRA(0xFF7BFF00, 0xFFFFFFFF);
            rdl.AddText(chat);

            foreach (Player.Player toplayer in from.Group.Members)
            {
                SendChatToPlayer(toplayer, rdl);
            }
        }

        private static void SendChatToPrivate(Player.Player from, uint to, string chat)
        {
            if (DPGameRunner.Playerlist.ContainsKey(to))
            {
                Player.Player toplayer = DPGameRunner.Playerlist[to].Player;

                var rdl = new Rdl();
                rdl.AddTRA(0xFFFFFF00, 0xFFFFFFFF);
                rdl.AddText(from.Name + ": ");
                rdl.AddTRA(0x19BD3A00, 0xFFFFFFFF);
                rdl.AddText(chat);
                SendChatToPlayer(from, rdl);
                SendChatToPlayer(toplayer, rdl);
            }
        }

        #endregion

        public static Player.Player FindActivePlayerByCharname(DPGameRunner server, string arg, int searchMode)
        {
            arg = arg.ToLowerInvariant();

            if (searchMode == 0)
            {
                uint id;
                if (!uint.TryParse(arg, out id))
                    return null;

                if (!DPGameRunner.Playerlist.ContainsKey(id))
                    return null;

                return DPGameRunner.Playerlist[id].Player;
            }

            // Search for an exact match
            foreach (DPGameRunner.PlayerListItem player in DPGameRunner.Playerlist.Values)
            {
                if (player.Name.ToLowerInvariant() == arg)
                    return player.Player;
            }

            // Search for a partial match if requested
            if (searchMode == 1)
            {
                var matches =
                    DPGameRunner.Playerlist.Values.Where(player => player.Name.ToLowerInvariant().StartsWith(arg)).ToList();

                if (matches.Count == 1)
                    return matches[0].Player;
            }

            return null;
        }

        /// <summary>
        ///     Process an admin command.
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <param name="chat"></param>
        /// <returns>Returns true if the command was processed and further processing should be stopped</returns>
        public static bool ProcessAdminCommands(Player.Player from, uint to, string chat)
        {
            // If this is an admin command and the player has admin permissions
            // then dispatch
            if (chat[0] == '.')
            {
                // Echo the chat back to the player.
                SendEcho(from, chat);

                string[] args = chat.Split(' ');

                // Figure out if the charname argument for the command is for
                // a partial, FL ID match or exact match.
                int searchMode = 2;
                if (args.Length > 0 && args[0].EndsWith("&"))
                {
                    args[0] = args[0].Substring(0, args[0].Length - 1);
                    searchMode = 1;
                }
                else if (args.Length > 0 && args[0].EndsWith("$"))
                {
                    args[0] = args[0].Substring(0, args[0].Length - 1);
                    searchMode = 0;
                }

                // Process the command.
                if (args.Length == 3 && args[0] == ".beam")
                {
                    Player.Player player = FindActivePlayerByCharname(from.Runner, args[1], searchMode);
                    if (player == null)
                    {
                        SendEcho(from, "ERR charname not found");
                        return true;
                    }

                    string basename = args[2];
                    BaseData basedata = UniverseDB.FindBase(basename);
                    if (basedata == null)
                    {
                        SendEcho(from, "ERR base not found");
                        return true;
                    }

                    if (player.Ship.Basedata != null)
                    {
                        SendEcho(from, "ERR player not in space");
                        return true;
                    }

                    //TODO: make it beam
                    //player.Runner.AddEvent(new DPGRBeam(player, basedata));
                    SendEcho(from, "OK"); // fixme: need feedback.
                }
                else if (args.Length == 3 && args[0] == ".addcash")
                {
                    Player.Player player = FindActivePlayerByCharname(from.Runner, args[1], searchMode);
                    if (player == null)
                    {
                        SendEcho(from, "ERR charname not found");
                        return true;
                    }

                    int money;
                    if (!Int32.TryParse(args[2], out money))
                    {
                        SendEcho(from, "ERR invalid money");
                        return true;
                    }

                    //TODO: make it so
                    //player.Runner.AddEvent(new DPGRAddCash(player, money));
                    SendEcho(from, "OK");
                    // fixme: SendChatToPlayer(from, "OK cash=" + player.money);
                }
                else if (args.Length == 3 && args[0] == ".setcash")
                {
                    Player.Player player = FindActivePlayerByCharname(from.Runner, args[1], searchMode);
                    if (player == null)
                    {
                        SendEcho(from, "ERR charname not found");
                        return true;
                    }

                    int money = 0;
                    if (!Int32.TryParse(args[2], out money))
                    {
                        SendEcho(from, "ERR invalid money count");
                        return true;
                    }

                    //TODO: make it so
                    //player.Runner.AddEvent(new DPGRSetCash(player, money));
                    SendEcho(from, "OK");
                    // fixme: SendChatToPlayer(from, "OK cash=" + player.money);
                }
                else if (args.Length == 3 && args[0] == ".setcash")
                {
                    Player.Player player = FindActivePlayerByCharname(from.Runner, args[1], searchMode);
                    if (player == null)
                    {
                        SendEcho(from, "ERR charname not found");
                        return true;
                    }

                    int money = Int32.Parse(args[2]);
                    player.Money += money;
                    Packets.SendSetMoney(player);

                    SendEcho(from, "OK cash=" + player.Money);
                }
                else if (args.Length == 3 && args[0] == ".getrep")
                {
                    Player.Player player = FindActivePlayerByCharname(from.Runner, args[1], searchMode);
                    if (player == null)
                    {
                        SendEcho(from, "ERR charname not found");
                        return true;
                    }

                    string factionname = args[2];
                    Faction faction = UniverseDB.FindFaction(factionname);
                    if (faction == null)
                    {
                        SendEcho(from, "ERR not found faction=" + factionname);
                        return true;
                    }

                    float attitude = player.Ship.GetAttitudeTowardsFaction(faction);
                    SendEcho(from, "OK faction=" + factionname + " rep=" + attitude);
                }
                else if (args.Length == 4 && args[0] == ".setrep")
                {
                    Player.Player player = FindActivePlayerByCharname(from.Runner, args[1], searchMode);
                    if (player == null)
                    {
                        SendEcho(from, "ERR charname not found");
                        return true;
                    }

                    string factionname = args[2];
                    Faction faction = UniverseDB.FindFaction(factionname);
                    if (faction == null)
                    {
                        SendEcho(from, "ERR not found faction=" + factionname);
                        return true;
                    }

                    float attitude = 0;
                    if (!float.TryParse(args[3], out attitude))
                    {
                        SendEcho(from, "ERR invalid rep=" + args[3]);
                        return true;
                    }

                    player.Ship.SetReputation(faction, attitude);
                    SendEcho(from,
                        "OK faction=" + faction.Nickname + " rep=" + player.Ship.GetAttitudeTowardsFaction(faction));
                }
                else if (args.Length == 3 && args[0] == ".setrept")
                {
                    var player = FindActivePlayerByCharname(from.Runner, args[1], searchMode);
                    if (player == null)
                    {
                        SendEcho(from, "ERR charname not found");
                        return true;
                    }

                    float attitude = 0;
                    if (!float.TryParse(args[2], out attitude))
                    {
                        SendEcho(from, "ERR invalid rep=" + args[3]);
                        return true;
                    }

                    var solar = UniverseDB.FindSolar(player.Ship.TargetObjID);
                    if (solar != null)
                    {
                        SendEcho(from, "OK solar=" + solar.Faction + " rep=" + attitude);
                        player.Ship.SetReputation(solar.Faction, attitude);
                        return true;
                    }

                    SendEcho(from, "ERR only solar's supported cause I was lazy");
                }
                else if (args.Length == 2 && args[0] == ".kill")
                {
                    Player.Player player = FindActivePlayerByCharname(from.Runner, args[1], searchMode);
                    if (player == null)
                    {
                        SendEcho(from, "ERR charname not found");
                        return true;
                    }

                    if (player.Ship.Basedata != null)
                    {
                        SendEcho(from, "ERR player not in space");
                        return true;
                    }

                    player.Ship.Destroy(DeathCause.Command);
                    SendEcho(from, "OK");
                }
                else if (args.Length == 4 && args[0] == ".move")
                {
                    float x = 0;
                    float y = 0;
                    float z = 0;
                    if (!float.TryParse(args[1], out x)
                        || !float.TryParse(args[2], out y)
                        || !float.TryParse(args[3], out z))
                    {
                        SendEcho(from, String.Format("ERR invalid position={0:0} {1:0} {2:0}", x, y, z));
                        return true;
                    }

                    var dummyAction = new LaunchInSpaceAction
                    {
                        Position = new Vector(x, y, z),
                        Orientation = Quaternion.MatrixToQuaternion(@from.Ship.Orientation)
                    };
                    from.Ship.CurrentAction = dummyAction;
                    Packets.SendServerLaunch(@from);
                    from.Ship.CurrentAction = null;
                    SendEcho(from, String.Format("OK position={0:0} {1:0} {2:0}", x, y, z));
                }
                else if (args.Length == 1 && args[0] == ".help")
                {
                    Packets.SendInfocardUpdate(@from, 500000, "Admin Commands");
                    Packets.SendInfocardUpdate(@from, 500001, from.Runner.Server.AdminHelpMsg);
                    Packets.SendPopupDialog(@from, new FLFormatString(500000), new FLFormatString(500001),
                        Player.Player.PopupDialogButtons.POPUPDIALOG_BUTTONS_CENTER_OK);
                }
                else
                {
                    SendEcho(from, "ERR command invalid, type .help for valid commands");
                }
                return true;
            }
            return false;
        }

        private static bool TextToBool(string text)
        {
            if (text == "on" | text == "true" | text == "enable")
            {
                return true;
            }

            return false;
        }

        public static bool ProcessUserCommands(Player.Player from, uint to, string chat)
        {
            // If this is a player command then process it
            if (chat[0] == '/')
            {
                var args = chat.Split(' ');
                if (args[0] == "/s")
                {
                    SendChatToSystem(from,chat.Substring(3));
                    return true;
                }

                // Echo the chat back to the player.
                SendEcho(from, chat);


                if (args.Length == 1 && args[0] == "/ping")
                {
                    SendEcho(from, "OK " + from.Runner.Server.GetConnectionInformation(from));
                }
                else if (args.Length == 1 && args[0] == "/pos")
                {
                    SendEcho(from, String.Format("OK pos = {0} {1} {2}",
                        from.Ship.Position.x, from.Ship.Position.y, from.Ship.Position.z));
                }
                else if (args.Length == 3 && args[0] == "/deathmsg")
                {
                    if (args[1] == "sys")
                    {
                        from.Settings[@"sendsystemdeath"] = TextToBool(args[2]);
                        SendEcho(from, "OK System death messages enabled: " + from.Settings[@"senddeath"]);
                    }
                    else if (args[1] == "uni")
                    {
                        from.Settings[@"senddeath"] = TextToBool(args[2]);
                        SendEcho(from, "OK Universe death messages enabled: " + from.Settings[@"senddeath"]);
                    }
                    else
                    {
                        SendEcho(from, "ERR Wrong arguments, type /help for valid commands");
                    }
                }
                else if (args.Length == 1 && args[0] == "/help")
                {
                    Packets.SendInfocardUpdate(@from, 500000, "User Commands");
                    Packets.SendInfocardUpdate(@from, 500001, from.Runner.Server.UserHelpMsg);
                    Packets.SendPopupDialog(@from, new FLFormatString(500000), new FLFormatString(500001),
                        Player.Player.PopupDialogButtons.POPUPDIALOG_BUTTONS_CENTER_OK);
                }
                else if (args.Length == 2 && args[0] == "/rename")
                {
                    from.PlayerAccount.CharName = args[1];
                    SendEcho(from, "OK Char renamed. Please relogin.");
                    from.SaveCharFile();
                }
                else
                {
                    SendEcho(from, "ERR command invalid, type /help for valid commands");
                }


                return true;
            }
            return false;
        }
    }
}