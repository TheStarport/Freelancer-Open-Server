using System;
using System.Collections.Generic;
using Akka.Actor;
using Akka.Event;
using FLServer.Actors.Player.Chat;
using FLServer.Actors.Player.State.Base;
using FLServer.CharDB;
using FLServer.DataWorkers;

namespace FLServer.Actors.Player.State
{
    partial class State :UntypedActor
    {
        private uint _flPlayerID;
        private ActorRef _dPlaySession;
        private string _accountID;
        readonly LoggingAdapter _log = Akka.Event.Logging.GetLogger(Context);
        protected override void OnReceive(object message)
        {
            var num = Context.Parent.Path.Name.Split('/')[1];
            _flPlayerID = uint.Parse(num);

            if (((byte[])message)[0] == 0x01 && ((byte[])message).Length == 1)
            {
                // Keepalive
                //byte[] omsg = { 0xFF };
                Sender.Tell(new byte[] { 0xFF });
            }

            //var dplayTask = Sender.Ask<LocalActorRef>("GetDPlaySession");
            //dplayTask.Wait(50);

            _dPlaySession = Sender;
            Context.Become(LoginState,false);
            _log.Debug("New state id {0} login", _flPlayerID);

        }

        /// <summary>
        /// Session init; ban and multilogin checks
        /// </summary>
        /// <param name="message"></param>
        private void LoginState(object message)
        {
            var msg = (byte[]) message;
            if (msg[0] == 0x01 && msg.Length == 1)
            {
                // Keepalive
                //byte[] omsg = { 0xFF };
                Context.Sender.Tell(new byte[] { 0xFF });
            }
            else if (msg[0] == 0x01 && msg.Length == 75)
            {
                // Save login information.
                var pos = 3;
                var accountid = FLMsgType.GetUnicodeStringLen16(msg, ref pos);

                byte[] omsg = { 0x02, 0x02 };
            //    // If the account is banned kick the player.
                _accountID = accountid;
                var accs = Database.GetAccount(accountid);
            //    //TODO: check if banning works; possibly make separate table for ID bans
                var isbanned = false;
                if (accs != null)
                    if (accs[0].IsBanned)
                        isbanned = true;
            //    // If the account is already logged in or banned, reject the login


                var findAccID =
                    Context.ActorSelection("user/server/player/*").Ask<bool>(new PlayerActor.CheckAccountID(accountid),TimeSpan.FromMilliseconds(100));
                //findAccID.Wait(250);

                var accOnline = false;
                //TODO: heeeavy
                try
                {
                    if (findAccID.Result)
                        accOnline = true;
                }
                catch { }

                if (isbanned) 
                    //TODO: send and close session
                    FLMsgType.AddUInt8(ref omsg, FLMsgType.MSG_TYPE_LOGIN_REPORT_TYPE_BANNED);
                else
                    if (accOnline)
                    {
                        FLMsgType.AddUInt8(ref omsg, FLMsgType.MSG_TYPE_LOGIN_REPORT_TYPE_INUSE);
                    }
                    else
                    {
                        FLMsgType.AddUInt8(ref omsg, FLMsgType.MSG_TYPE_LOGIN_REPORT_TYPE_OKAY);
                    }
                Context.Parent.Tell(new PlayerActor.SetAccountID(accountid,_flPlayerID));
                
                Context.Sender.Tell(omsg);
            }
            else if (msg[0] == 0x05 && msg[1] == 0x03)
            {
                // char info request
                //player.SaveCharFile();
                SendCharInfoResponse();
                Context.Become(SelectCharState,false);
                _log.Debug("New state id {0} select-char", _flPlayerID);
            }
            else
            {
                // Unexpected packet. Log and ignore it.
                _log.Warn("Unexpected message from FLID {0} in LoginState: {1}",_flPlayerID,msg);
                //player.Log.AddLog(LogType.FL_MSG, "Unexpected message: client rx", player.DPSess, msg);
            }
        }



        /// <summary>
        /// Character selection state.
        /// </summary>
        /// <param name="message"></param>
        private void SelectCharState(object message)
        {
            var msg = (byte[]) message;
            if (msg[0] == 0x01 && msg.Length == 1)
            {
                // Keepalive
                //byte[] omsg = { 0xFF };
                Context.Sender.Tell(new byte[] { 0xFF });
            }
            else if (msg[0] == 0x05 && msg[1] == 0x03)
            {
                // FLPACKET_CLIENT_REQUESTCHARINFO
                //player.Log.AddLog(LogType.FL_MSG, "FLPACKET_CLIENT_REQUESTCHARINFO");
                SendCharInfoResponse();
            }
            else if (msg[0] == 0x06 && msg[1] == 0x03)
            {
                //TODO: SelectCharacter
                // FLPACKET_CLIENT_SELECTCHARACTER
                //var nameBefore = player.Name;
                var pos = 2;
                var charfilename = FLMsgType.GetAsciiStringLen16(msg, ref pos);

                var acct = Database.GetOneAccount(_accountID, charfilename);

                //var result = player.LoadCharFile(acct, player.Log);

                if (acct == null)
                {
                    _log.Error("Cannot load character id={0} name={1}",
                        _accountID,charfilename);
                    return;
                }

                //player.Log.AddLog(LogType.GENERAL,
                    //"FLPACKET_CLIENT_SELECTCHARACTER charfilename={0} name={1} system={2}", charfilename, player.Name,
                    //player.Ship.System.Nickname);
                //if (player.Ship != null && player.Ship.Objid != 0)
                    //player.Runner.DelSimObject(player.Ship);

                _log.Debug("Char selected: {0}",_accountID);
                Context.Parent.Tell(new PlayerActor.CharSelected(acct,_flPlayerID),Context.Sender);
                _baseRef = Context.ActorOf<BaseState>("base");
                SendVisitedState(acct.Visits);
                SendMissionLog();
                SendInterfaceState();
                SendInitSetReputation();
                SendCharSelectVerified();

                //SendMiscObjUpdate UNK 2
                //TODO: figure out what that means
                byte[] omsg = { 0x54, 0x02, 0x28, 0x00 };
                FLMsgType.AddUInt32(ref omsg, 0);
                FLMsgType.AddInt32(ref omsg, -1); // faction?
                Sender.Tell(omsg);

                //do we really need enum it again?
                SendCharInfoResponse();

                //TODO: infocard update
                //if ((Runner.Server.IntroMsg != null) && firstLogin)
                //{
                //    Packets.SendInfocardUpdate(this, 500000, "Welcome to Discovery");

                //    string intro = Runner.Server.IntroMsg.Replace("$$player$$", Name);
                //    Packets.SendInfocardUpdate(this, 500001, intro);

                //    Packets.SendPopupDialog(this, new FLFormatString(500000), new FLFormatString(500001),
                //        PopupDialogButtons.POPUPDIALOG_BUTTONS_CENTER_OK);
                //}

                Context.ActorSelection("../chat").Tell(new ConsoleMessage(ServerUtils.WelcomeMessage.Replace(@"$$player$$",acct.CharName)),Context.Sender);

                Become(InBaseState,false);

                //player.Update();
            }
            else if (msg[0] == 0x39 && msg[1] == 0x03)
            {
                //player.Log.AddLog(LogType.FL_MSG, "FLPACKET_CLIENT_CREATENEWCHAR");
                // New character
                var pos = 2;
                var charname = FLMsgType.GetUnicodeStringLen16(msg, ref pos);

                //TODO: do nothing when charname exists?
                //BUG: client hangs
                if (Database.CheckIfNameOccupied(charname)) 
                    return;

                Database.AddAccount(_accountID, charname);

                SendCharInfoResponse();
            }
            else if (msg[0] == 0x3a && msg[1] == 0x03)
            {
                //player.Log.AddLog(LogType.FL_MSG, "FLPACKET_CLIENT_DESTROYCHAR");

                // Delete character
                var pos = 2;
                var charfile = FLMsgType.GetAsciiStringLen16(msg, ref pos);

                Database.DelAccount(_accountID, charfile);

                SendCharInfoResponse();
            }
            else
            {
                // Unexpected packet. Log and ignore it.
                _log.Warn("Unexpected message from FLID {0} in SelectCharState: {1}", _flPlayerID, BitConverter.ToString(msg));
            }
        }


        /// <summary>
        /// Sends character list for current account.
        /// </summary>
        private void SendCharInfoResponse()
        {
            
            var accs = Database.GetAccount(_accountID);

                byte[] omsg = {0x03, 0x02};
                FLMsgType.AddUInt8(ref omsg, 0); // chars
                if (accs != null)
                    foreach (var acct in accs)
                    {
                        //try
                        //{

                            FLMsgType.AddAsciiStringLen16(ref omsg, FLMsgType.FLNameToFile(acct.CharName));
                            FLMsgType.AddUInt16(ref omsg, 0);
                            FLMsgType.AddUnicodeStringLen16(ref omsg, acct.CharName);
                            FLMsgType.AddUnicodeStringLen16(ref omsg, "");
                            FLMsgType.AddUInt32(ref omsg, 0);
                            FLMsgType.AddUInt32(ref omsg, 0);
                            FLMsgType.AddUInt32(ref omsg, 0);
                            FLMsgType.AddUInt32(ref omsg, acct.Ship);
                            if (acct.Money > 2000000000)
                                FLMsgType.AddInt32(ref omsg, 2000000000);
                            else
                                FLMsgType.AddInt32(ref omsg, acct.Money);

                        //TODO: ideally we should check if base, system and equip exists
                            FLMsgType.AddUInt32(ref omsg, FLUtility.CreateID(acct.System));
                            if (acct.ShipState.Base != null)
                                FLMsgType.AddUInt32(ref omsg, FLUtility.CreateID(acct.ShipState.Base));
                            else
                                FLMsgType.AddUInt32(ref omsg, 0);
                            FLMsgType.AddUInt32(ref omsg, 0);
                            FLMsgType.AddUInt32(ref omsg, FLUtility.CreateID(acct.Appearance.Voice));
                            FLMsgType.AddUInt32(ref omsg, acct.Rank);
                            FLMsgType.AddUInt32(ref omsg, 0);
                            FLMsgType.AddFloat(ref omsg, acct.ShipState.Hull);
                            FLMsgType.AddUInt32(ref omsg, 0);
                            FLMsgType.AddUInt32(ref omsg, 0);
                            FLMsgType.AddUInt32(ref omsg, 0);

                            FLMsgType.AddUInt8(ref omsg, 1);
                            FLMsgType.AddUInt32(ref omsg, acct.Appearance.Body);
                            FLMsgType.AddUInt32(ref omsg, acct.Appearance.Head);
                            FLMsgType.AddUInt32(ref omsg, acct.Appearance.LeftHand);
                            FLMsgType.AddUInt32(ref omsg, acct.Appearance.RightHand);
                            FLMsgType.AddUInt32(ref omsg, 0);

                            FLMsgType.AddUInt8(ref omsg, 1);
                            FLMsgType.AddUInt32(ref omsg, acct.Appearance.Body);
                            FLMsgType.AddUInt32(ref omsg, acct.Appearance.Head);
                            FLMsgType.AddUInt32(ref omsg, acct.Appearance.LeftHand);
                            FLMsgType.AddUInt32(ref omsg, acct.Appearance.RightHand);
                            FLMsgType.AddUInt32(ref omsg, 0);

                            FLMsgType.AddUInt8(ref omsg, (uint) acct.Equipment.Count);
                            uint hpid = 2;
                            foreach (var item in acct.Equipment)
                            {
                                //item.count?
                                FLMsgType.AddUInt32(ref omsg, 1);
                                FLMsgType.AddFloat(ref omsg, item.Health);
                                FLMsgType.AddUInt32(ref omsg, item.Arch);
                                FLMsgType.AddUInt16(ref omsg, hpid);
                                hpid++;
                                //item.mounted
                                FLMsgType.AddUInt16(ref omsg, (1u));
                                if (item.HpName != "")
                                    FLMsgType.AddAsciiStringLen16(ref omsg, item.HpName + "\0");
                                else
                                    FLMsgType.AddAsciiStringLen16(ref omsg, "BAY\0");
                            }

                            FLMsgType.AddUInt32(ref omsg, 0);

                            omsg[2]++;
                        //}
                        //catch (Exception e)
                        //{
                        //    _log.Error("Corrupt character when processing charinforequest '{0}'",
                        //        e.Message);
                        //}
                    }
                FLMsgType.AddUInt32(ref omsg, 0);
                Context.Sender.Tell(omsg);
        }

        private void SendVisitedState(IReadOnlyCollection<KeyValuePair<uint, uint>> visits)
        {
            byte[] omsg = { 0x13, 0x01 };
            FLMsgType.AddInt32(ref omsg, 4 + (visits.Count * 5));
            FLMsgType.AddInt32(ref omsg, visits.Count);
            foreach (var pi in visits)
            {
                FLMsgType.AddUInt32(ref omsg, pi.Key);
                FLMsgType.AddUInt8(ref omsg, pi.Value);
            }

            Context.Sender.Tell(omsg);
        }

        private void SendMissionLog()
        {
            byte[] omsg = { 0x19, 0x01 };
            FLMsgType.AddUInt32(ref omsg, 8); // cnt * 4 
            FLMsgType.AddUInt32(ref omsg, 1); // 1 
            FLMsgType.AddUInt32(ref omsg, 0); // 0
            Context.Sender.Tell(omsg);
        }


        public void SendInitSetReputation()
        {
            byte[] omsg = { 0x29, 0x02 };
            
            FLMsgType.AddUInt8(ref omsg, 0x01);
            FLMsgType.AddUInt32(ref omsg, _flPlayerID);
            FLMsgType.AddUInt32(ref omsg, 0);
            FLMsgType.AddFloat(ref omsg, 0); // rep
            Context.Sender.Tell(omsg);
        }

        public void SendCharSelectVerified()
        {
            byte[] omsg = { 0x08, 0x02 };
            FLMsgType.AddUInt32(ref omsg, _flPlayerID);
            //TODO: not sure if server start time or game time
            FLMsgType.AddDouble(ref omsg, ServerUtils.GameTime());
            Sender.Tell(omsg);
        }

        public void SendInterfaceState()
        {
            byte[] omsg = { 0x1c, 0x01 };
            FLMsgType.AddUInt32(ref omsg, 1); // cnt
            FLMsgType.AddUInt32(ref omsg, 3); // state 3
            Context.Sender.Tell(omsg);
        }
    }
}
