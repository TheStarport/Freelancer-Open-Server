using System.IO;
using FLServer.DataWorkers;
using FLServer.Player;

namespace FLServer
{
    internal class DPCLoginState : IPlayerState
    {
        private static DPCLoginState _instance;

        public string StateName()
        {
            return "login-state";
        }

        public void EnterState(Player.Player player)
        {
            // Send a connection acknowledgement to inform the client of the flplayerid.
            {
                // FLPACKET_SERVER_CONNECTRESPONSE
                byte[] omsg = {0x01, 0x02};
                FLMsgType.AddUInt32(ref omsg, player.FLPlayerID);
                player.SendMsgToClient(omsg);
            }

            Packets.SendMiscObjUpdate(player, Player.Player.MiscObjUpdateType.NEWS, player.Runner.Server.server_news);
        }

        public void RxMsgFromClient(Player.Player player, byte[] msg)
        {
            if (msg[0] == 0x01 && msg.Length == 1)
            {
                // Keepalive
                byte[] omsg = {0xFF};
                player.SendMsgToClient(omsg);
            }
            else if (msg[0] == 0x01 && msg.Length == 75)
            {
                // Save login information.
                int pos = 3;
                string accountid = FLMsgType.GetUnicodeStringLen16(msg, ref pos);

                //string accDirPath = player.Runner.Server.AcctPath + Path.DirectorySeparatorChar +
                //                    FLMsgType.FLNameToFile(accountid);
                // If the account directory does not exist, create it and save the account id file.
                //if (!Directory.Exists(accDirPath))
                //    Directory.CreateDirectory(accDirPath);
                //FLUtility.WriteAccountID(accDirPath, accountid);


                byte[] omsg = {0x02, 0x02};
                // If the account is banned kick the player.

                var accs = Old.CharacterDB.Database.GetAccount(accountid);
                //TODO: check if banning works; possibly make separate table for ID bans
                bool isbanned = false;
                if (accs != null)
                    if (accs[0].IsBanned)
                        isbanned = true;
                //if (File.Exists(accDirPath + Path.DirectorySeparatorChar + "banned"))
                //{


                //    FLMsgType.AddUInt8(ref omsg, FLMsgType.MSG_TYPE_LOGIN_REPORT_TYPE_BANNED);
                //}
                    // If the account is already logged in, reject the login
                    // fixme: this is not thread safe 
                //else
                if (isbanned) FLMsgType.AddUInt8(ref omsg, FLMsgType.MSG_TYPE_LOGIN_REPORT_TYPE_BANNED);
                else
                if (player.Runner.Server.FindPlayerByAccountID(accountid) != null)
                {
                    FLMsgType.AddUInt8(ref omsg, FLMsgType.MSG_TYPE_LOGIN_REPORT_TYPE_INUSE);
                }
                else
                {
                    FLMsgType.AddUInt8(ref omsg, FLMsgType.MSG_TYPE_LOGIN_REPORT_TYPE_OKAY);
                }
                player.AccountID = accountid;
                player.SendMsgToClient(omsg);
            }
            else if (msg[0] == 0x05 && msg[1] == 0x03)
            {
                // char info request
                player.SaveCharFile();
                player.SetState(DPCSelectingCharacterState.Instance());
            }
            else
            {
                // Unexpected packet. Log and ignore it.
                player.Log.AddLog(LogType.FL_MSG, "Unexpected message: client rx", player.DPSess, msg);
            }
        }

        public static DPCLoginState Instance()
        {
            return _instance ?? (_instance = new DPCLoginState());
        }
    }


    internal class DPCSelectingCharacterState : IPlayerState
    {
        private static DPCSelectingCharacterState _instance;

        public string StateName()
        {
            return "selecting-char-state";
        }

        /// <summary>
        ///     Send the current player list and list of characters this player
        ///     has in their account.
        /// </summary>
        /// <param name="player"></param>
        public void EnterState(Player.Player player)
        {
            player.PlayerAccount = null;
            Packets.SendCharInfoRequestResponse(player);
        }

        public void RxMsgFromClient(Player.Player player, byte[] msg)
        {
            if (msg[0] == 0x01 && msg.Length == 1)
            {
                // Keepalive
                byte[] omsg = {0xFF};
                player.SendMsgToClient(omsg);
            }
            else if (msg[0] == 0x05 && msg[1] == 0x03)
            {
                // FLPACKET_CLIENT_REQUESTCHARINFO
                player.Log.AddLog(LogType.FL_MSG, "FLPACKET_CLIENT_REQUESTCHARINFO");
                Packets.SendCharInfoRequestResponse(player);
            }
            else if (msg[0] == 0x06 && msg[1] == 0x03)
            {
                // FLPACKET_CLIENT_SELECTCHARACTER
                var nameBefore = player.Name;
                var pos = 2;
                var charfilename = FLMsgType.GetAsciiStringLen16(msg, ref pos);

                var acct = Old.CharacterDB.Database.GetOneAccount(player.AccountID, charfilename);

                var result = player.LoadCharFile(acct, player.Log);

                if (result != null)
                {
                    player.Log.AddLog(LogType.ERROR, "error: cannot load character accdir={0} charfile={1} reason={2}",
                        acct.CharName, result);
                    return;
                }

                player.Log.AddLog(LogType.GENERAL,
                    "FLPACKET_CLIENT_SELECTCHARACTER charfilename={0} name={1} system={2}", charfilename, player.Name,
                    player.Ship.System.Nickname);
                if (player.Ship != null && player.Ship.Objid != 0)
                    player.Runner.DelSimObject(player.Ship);

                player.OnCharacterSelected(nameBefore == player.Name, nameBefore == null);
                player.Update();
            }
            else if (msg[0] == 0x39 && msg[1] == 0x03)
            {
                player.Log.AddLog(LogType.FL_MSG, "FLPACKET_CLIENT_CREATENEWCHAR");
                // New character
                int pos = 2;
                string charname = FLMsgType.GetUnicodeStringLen16(msg, ref pos);

                //TODO: do nothing when charname exists?
                if (Old.CharacterDB.Database.GetAccount(@"CharName", charname) != null) return;

                Old.CharacterDB.Database.AddAccount(player.AccountID, charname);

                //string charfile = player.Runner.Server.AcctPath +
                //                  Path.DirectorySeparatorChar + FLMsgType.FLNameToFile(player.AccountID) +
                //                  Path.DirectorySeparatorChar + FLMsgType.FLNameToFile(charname) + ".fl";
                //if (!File.Exists(charfile))
                //{
                //    var file =
                //        new FLDataFile(player.Runner.Server.AcctPath + Path.DirectorySeparatorChar + "default.fl", true);
                //    file.AddSetting("Player", "name", new object[] {FLUtility.EncodeUnicodeHex(charname)});
                //    file.SaveSettings(charfile, false);
                //}

                Packets.SendCharInfoRequestResponse(player);
            }
            else if (msg[0] == 0x3a && msg[1] == 0x03)
            {
                player.Log.AddLog(LogType.FL_MSG, "FLPACKET_CLIENT_DESTROYCHAR");

                // Delete character
                var pos = 2;
                var charfile = FLMsgType.GetAsciiStringLen16(msg, ref pos);

                Old.CharacterDB.Database.DelAccount(player.AccountID, charfile);

                Packets.SendCharInfoRequestResponse(player);
            }
            else
            {
                // Unexpected packet. Log and ignore it.
                player.Log.AddLog(LogType.ERROR, "Unexpected message: client rx", player.DPSess, msg);
            }
        }

        public static DPCSelectingCharacterState Instance()
        {
            return _instance ?? (_instance = new DPCSelectingCharacterState());
        }
    }
}