using System;
using System.Collections.Generic;
using FLServer.Old.CharacterDB;
using FLServer.Old.Object;
using FLServer.Player.PlayerExtensions;
using FLServer.Server;

namespace FLServer.Player
{
    public interface IPlayerState
    {
        string StateName();
        void EnterState(Player player);
        void RxMsgFromClient(Player player, byte[] msg);
    };


    public class Player : CharacterData
    {
        public delegate void ERunnerUpdate(Player sender);

        public delegate void RunnerRxMsg(Player sender, byte[] message);


        

        public enum ChatCommand
        {
// ReSharper disable InconsistentNaming
            GROUPINVITEREQUEST = 0,
            GROUPLEAVEREQUEST = 1,
            GROUPINVITATIONACCEPTEDREQUEST = 2,
            GROUPINVITATIONSENT = 3,
            GROUPINVITATIONRECEIVED = 4,
            GROUPJOINED = 5,
            NEWGROUPMEMBER = 6,
            GROUPLEFT = 7,
            GROUPMEMBERLEFT = 8
// ReSharper restore InconsistentNaming
        }

        public enum MiscObjUpdateType
        {
// ReSharper disable InconsistentNaming
            RANK,
            SYSTEM,
            GROUP,
            UNK2,
            UNK3,
            NEWS,
// ReSharper restore InconsistentNaming
        }

        public enum PopupDialogButtons
        {
// ReSharper disable InconsistentNaming
            POPUPDIALOG_BUTTONS_LEFT_YES = 1,
            POPUPDIALOG_BUTTONS_CENTER_NO = 2,
            POPUPDIALOG_BUTTONS_RIGHT_LATER = 4,
            POPUPDIALOG_BUTTONS_CENTER_OK = 8
// ReSharper restore InconsistentNaming
        }

        /// <summary>
        ///     The accountid of the player.
        /// </summary>
        public string AccountID;

        /// <summary>
        ///     The ID of this player on the proxy server.
        /// </summary>
        public Session DPSess;

        /// <summary>
        ///     The FL player ID.
        /// </summary>
        public uint FLPlayerID;

        /// <summary>
        ///     The player's Group.
        /// </summary>
        public Group Group;

        /// <summary>
        ///     The Group the player is currently invited to.
        /// </summary>
        public Group GroupInvited;

        /// <summary>
        ///     The log message receiver.
        /// </summary>
        public ILogController Log;

        /// <summary>
        ///     We send notification of state changes to this player for these objects.
        /// </summary>
        public Dictionary<uint, SimObject> MonitoredObjs = new Dictionary<uint, SimObject>();

        /// <summary>
        ///     The connection to the proxy freelancer server.
        /// </summary>
        public volatile DPGameRunner Runner;

        /// <summary>
        ///     The state of the connection.
        /// </summary>
        private IPlayerState _state;


        /// <summary>
        ///     At object creation time we assume that the freelancer player has connected to the
        ///     proxy server and is expecting the normal freelancer server login sequence. This
        ///     class manages this message exchange until they select a character at which point
        ///     the controller will establish a connection to a slave freelancer server and
        ///     forward traffic between the two.
        /// </summary>
        /// <param name="dplayid"></param>
        /// <param name="log"></param>
        /// <param name="flplayerid"></param>
        /// <param name="runner"></param>
        public Player(Session dplayid, ILogController log, uint flplayerid, DPGameRunner runner)
        {
            DPSess = dplayid;
            Log = log;
            FLPlayerID = flplayerid;
            Runner = runner;
            Ship = new Old.Object.Ship.Ship(runner) {player = this};
            Wgrp = new WeaponGroup();

            _state = DPCLoginState.Instance();
            _state.EnterState(this);
        }

        public event RunnerRxMsg RxMsgToRunner;


        public virtual void OnRxMsgToRunner(byte[] message)
        {
            RunnerRxMsg handler = RxMsgToRunner;
            if (handler != null) handler(this, message);
        }

        public event ERunnerUpdate RunnerUpdate;

        protected virtual void OnRunnerUpdate()
        {
            ERunnerUpdate handler = RunnerUpdate;
            if (handler != null) handler(this);
        }

        public event EventHandler<Player> PlayerDeleted;

        public virtual void OnPlayerDeleted()
        {
            EventHandler<Player> handler = PlayerDeleted;
            if (handler != null) handler(null, this);
        }

        public void SetState(IPlayerState newstate)
        {
            if (_state != newstate)
            {
                Log.AddLog(LogType.GENERAL, "change state: old={0} new={1}", _state.StateName(), newstate.StateName());
                _state = newstate;
                _state.EnterState(this);
                if (_state.StateName() == "in-base-state")
                {
                    Ship.IsDestroyed = false;
                }
            }
        }

        public void RxMsgFromClient(byte[] msg)
        {
            _state.RxMsgFromClient(this, msg);
        }

        // FLPACKET_COMMON_SET_VISITED_STATE

        // FLPACKET_COMMON_SET_MISSION_LOG

        // FLPACKET_COMMON_SET_INTERFACE_STATE

        // FLPACKET_SERVER_SETREPUTATION

        // FLPACKET_SERVER_SETREPUTATION


        // FLPACKET_SERVER_GFCOMPLETECHARLIST

        // FLPACKET_SERVER_GFCOMPLETESCRIPTBEHAVIORLIST

        // FLPACKET_SERVER_GFCOMPLETEAMBIENTSCRIPTLIST

        public void SendMsgToClient(byte[] msg)
        {
            Runner.SendMessage(this, msg);
        }

        public void Update()
        {
            OnRunnerUpdate();
            //Runner.Server.AddEvent(new DPGameRunnerPlayerUpdateEvent(this));
        }

        public void SendWeaponGroup()
        {
            //TODO: send weap group
            // wgrp
        }

        public void OnCharacterSelected(bool sameChar, bool firstLogin)
        {
            Packets.SendCompletePlayerList(this);

            if (sameChar || firstLogin)
            {
                Packets.SendPlayerListJoin(this, this, false);
                foreach (DPGameRunner.PlayerListItem playerListItem in DPGameRunner.Playerlist.Values)
                {
                    Packets.SendPlayerListJoin(playerListItem.Player, this, playerListItem.FlPlayerID == FLPlayerID);
                }
            }
            else
            {
                foreach (DPGameRunner.PlayerListItem playerListItem in DPGameRunner.Playerlist.Values)
                {
                    if (FLPlayerID != playerListItem.FlPlayerID)
                        Packets.SendPlayerListDepart(playerListItem.Player, this);
                    Packets.SendPlayerListJoin(playerListItem.Player, this, false);
                }
            }

            Packets.SendSetVisitedState(this);

            Packets.SendSetMissionLog(this);
            Packets.SendSetInterfaceState(this);
            SendWeaponGroup(); // dunno if position is right
            Packets.SendInitSetReputation(this);
            Packets.SendCharSelectVerified(this);
            Packets.SendMiscObjUpdate(this, MiscObjUpdateType.UNK2, 0);
            SetState(DPCInBaseState.Instance());


            if ((Runner.Server.IntroMsg != null) && firstLogin)
            {
                Packets.SendInfocardUpdate(this, 500000, "Welcome to Discovery");

                string intro = Runner.Server.IntroMsg.Replace("$$player$$", Name);
                Packets.SendInfocardUpdate(this, 500001, intro);

                Packets.SendPopupDialog(this, new FLFormatString(500000), new FLFormatString(500001),
                    PopupDialogButtons.POPUPDIALOG_BUTTONS_CENTER_OK);
            }
        }
    }
}