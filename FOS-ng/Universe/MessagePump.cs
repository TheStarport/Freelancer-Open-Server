using System.Collections.Generic;
using FOS_ng.DirectplayServer;
using FOS_ng.Logging;
using FOS_ng.Objects;
using FOS_ng.Player;


namespace FOS_ng.Universe
{


    public class MessagePump
    {
        /// <summary>
        ///     A map of fl player IDs to Players.
        /// </summary>
        public Dictionary<uint, Player.Player> Players = new Dictionary<uint, Player.Player>();

        private Server _server;

        public MessagePump(Server server)
        {
            _server = server;
            Player.Player.MPump = this;
            // Kick off the game runner threads. We have one thread per system. New
            // players who have not selected a character are assigned to a random
            // runner thread.            
            uint baseObjid = 1;
            
            foreach (var starSystem in UniverseDB.Systems.Values)
            {
                _runners[starSystem] = new DPGameRunner(this, _log, baseObjid, starSystem);
                baseObjid += 10000;
            }
        }

        /// <summary>
        /// De-facto it's a new\logged on player handler.
        /// </summary>
        /// <param name="player"></param>
        public async void PlayerUpdate(Player.Player player)
        {
            Players.Add(player.FLPlayerID, player);
            //if (player.Ship.Objid != 0)
                //Systems[player.SysNickname].AffObjects[player.Ship.Objid] = player.Ship;
            //Objects[player.Ship.Objid] = player.Ship;
            //player.Ship.Runner = this;
            //AddTimer(player.Ship);
        }

        public void SendToClient(Session sess, byte[] msg)
        {
            _server.Dplay.SendTo(sess, msg);
        }

        public async void PlayerDelete(uint flPlayerID)
        {

            // fixme: might crash if the player leaves
            if (!Players.ContainsKey(flPlayerID)) return;

            var player = Players[flPlayerID];

            Players.Remove(flPlayerID);
            if (player.Ship.Objid != 0)
                DelSimObject(player.Ship);

            if (Players.ContainsKey(flPlayerID))
                Players.Remove(flPlayerID);

            foreach (var item in Players.Values)
                item.SendMsg(Packets.PlayerListDepart(player.FLPlayerID));
        }

        /// <summary>
        ///     A player/NPC has docked or left the server. Update and remove the
        ///     ship from any players monitoring it and remove it from the object list.
        /// </summary>
        public void DelSimObject(SimObject obj)
        {
            foreach (var player in Players.Values)
            {
                if (!player.MonitoredObjs.ContainsKey(obj.Objid)) continue;
                player.MonitoredObjs.Remove(obj.Objid);
                player.SendMsg(Packets.DestroyObj(obj.Objid, true));
            }

            obj.System.AffObjects.Remove(obj.Objid);
            obj.System.Objects.Remove(obj.Objid);
            obj.Objid = 0;
        }

        public async void MessageFromClient(uint dPlayID,byte[] msg)
        {

            if (!Players.ContainsKey(dPlayID)) return;

            var player = Players[dPlayID];

            //var revent = next_event as DPGameRunnerRxMsgEvent;

            // Process a message from this player. If the message isn't for this runner because
            // the runner ownership changes, requeue in the right place.
            if (revent.player.Runner != this)
            {
                Logger.AddLog(LogType.FLMsg, "Warning: requeued rx msg onto changed runner");
                revent.player.Runner.AddEvent(revent);
            }
            else
            {
                revent.player.RxMsgFromClient(revent.msg);
            }
        }
    }
}
