using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using FLServer.buf;
using FLServer.DataWorkers;
using FLServer.Munition;
using FLServer.Object;
using FLServer.Object.Base;
using FLServer.Object.Solar;
using FLServer.Old.Object;
using FLServer.Old.Object.Ship;
using FLServer.Physics;
using FLServer.Player;
using FLServer.Ship;

namespace FLServer.Server
{
    /// <summary>
    ///     This class controls a starsystem and everything that happens in it. Each
    ///     system runs in its own thread. When a player is in the system, the player
    ///     is owned by the system thread and comms to this player must be made by
    ///     queuing an event to the player which in turn is processed inside the thread.
    /// </summary>
    public class DPGameRunner  // : Reactor
    {
        /// <summary>
        ///     The starting objid for allocations.
        /// </summary>
        private readonly uint _baseObjid;

        /// <summary>
        ///     The current time in millisconds since system start.
        /// </summary>
        public double CurrentTime;

        /// <summary>
        ///     The ID of the last created object.
        /// </summary>
        private uint _lastAllocatedObjid;

        /// <summary>
        ///     The log file
        /// </summary>
        public ILogController Log;

        /// <summary>
        ///     Any space object including ships, solars that exists in the simulation
        /// </summary>
        public Dictionary<uint, SimObject> Objects = new Dictionary<uint, SimObject>();

        /// <summary>
        ///     Any dynamic (moving) object.
        /// </summary>
        public Dictionary<uint, SimObject> AffObjects = new Dictionary<uint, SimObject>();

        /// <summary>
        ///     The list of all players on the server, not just the players owned by this game runner
        /// </summary>
        public static Dictionary<uint, PlayerListItem> Playerlist = new Dictionary<uint, PlayerListItem>();

        /// <summary>
        ///     A map of fl player IDs to Players. Players in this list are
        ///     owned by this thread.
        ///     back to the thread to deal with timing issues when the player moves
        ///     from one system to another.
        /// </summary>
        public Dictionary<uint, Player.Player> Players = new Dictionary<uint, Player.Player>();

        /// <summary>
        ///     The reference to the owning controller
        /// </summary>
        public DPServer Server;

        /// <summary>
        ///     The reference to system handled by the class
        /// </summary>
        public StarSystem System;


        public event EventHandler<double> RefreshObjs;
        //public event EventHandler<Player.Player> RefreshMonitoredObjs; 

        /// <summary>
        ///     Kick off the game thread
        /// </summary>
        /// <param name="server"></param>
        /// <param name="log"></param>
        /// <param name="baseObjid"></param>
        /// <param name="system"></param>
        public DPGameRunner(DPServer server, ILogController log, uint baseObjid, StarSystem system)
        {
            Server = server;
            Log = log;
            _baseObjid = baseObjid;
            _lastAllocatedObjid = baseObjid;
            System = system;

            foreach (var s in system.Solars)
            {
                Objects[s.Key] = s.Value;
                s.Value.Runner = this;
                if (s.Value.Loadout != null)
                {
                    RefreshObjs += s.Value.HandleTimerEvent;
                    //AddTimer(s.Value);
                }
            }
                

            // Start the game simulation thread
            var gameThread = new Thread(GameThreadRun);
            gameThread.Start();
        }

        public Player.Player GetPlayer(uint playerid)
        {
            if (!Playerlist.ContainsKey(playerid))
            {
                return null;
            }

            return Playerlist[playerid].Player;
        }


        public void LinkPlayer(Player.Player player)
        {
            player.RxMsgToRunner += player_RxMsgToRunner;
            player.PlayerDeleted += player_PlayerDeleted;
            player.RunnerUpdate += player_RunnerUpdate;
        }

        public void player_RunnerUpdate(Player.Player player)
        {
            //var revent = next_event as DPGameRunnerPlayerUpdateEvent;

            // If the player is assigned to this runner, make sure we've got it in our
            // owned player list
            //Player.Player player = revent.player;
            if (player.Runner == this)
            {
                if (!Players.ContainsKey(player.FLPlayerID))
                {
                    Log.AddLog(LogType.GENERAL, "Player control gained runner={0} flplayerid={1}",
                        System.Nickname, player.FLPlayerID);
                    
                    Players.Add(player.FLPlayerID, player);
                    if (player.Ship.Objid != 0)
                        AffObjects[player.Ship.Objid] = player.Ship;
                    Objects[player.Ship.Objid] = player.Ship;
                    player.Runner = this;
                    player.Ship.Runner = this;
                    RefreshObjs += player.Ship.HandleTimerEvent;
                    //AddTimer(player.Ship);
                }
            }
            // If the player is not assigned to this runner, make sure it's not in our
            // owned player list.
            else
            {
                if (Players.ContainsKey(player.FLPlayerID))
                {

                    Log.AddLog(LogType.GENERAL, "Player control lost runner={0} flplayerid={1}", System.Nickname,
                        player.FLPlayerID);
                    player.PlayerDeleted -= player_PlayerDeleted;
                    player.RunnerUpdate -= player_RunnerUpdate;
                    player.RxMsgToRunner -= player_RxMsgToRunner;
                    if (player.Ship.Objid != 0)
                        AffObjects.Remove(player.Ship.Objid);
                    Objects.Remove(player.Ship.Objid);
                    //DelTimer(player.Ship);
                    RefreshObjs -= player.Ship.HandleTimerEvent;
                    Players.Remove(player.FLPlayerID);
                }
            }

            var update = !Playerlist.ContainsKey(player.FLPlayerID) ? new PlayerListItem() : Playerlist[player.FLPlayerID];

            update.Player = player;
            update.FlPlayerID = player.FLPlayerID;
            update.Name = player.Name;
            update.Rank = player.Ship.Rank;
            update.System = player.Ship.System;
            update.Group = player.Group;
            update.GroupInvited = player.GroupInvited;
            Playerlist[player.FLPlayerID] = update;

            // Notify all owned players of the player list update
            foreach (Player.Player p in Players.Values)
                Packets.SendPlayerListUpdate(p, update);
        }

        void player_PlayerDeleted(object sender, Player.Player e)
        {
            //var revent = next_event as DPGameRunnerPlayerDeletedEvent;

            // fixme: might crash if the player leaves
            if (!Players.ContainsKey(e.FLPlayerID)) return;

            var player = Players[e.FLPlayerID];

            Players.Remove(e.FLPlayerID);
            if (player.Ship.Objid != 0)
                DelSimObject(player.Ship);

            if (Playerlist.ContainsKey(e.FLPlayerID))
                Playerlist.Remove(e.FLPlayerID);

            foreach (var item in Playerlist.Values)
                Packets.SendPlayerListDepart(item.Player, player);
        }


        private double _lastUpdateTime;
        private void OnRefreshObjects(object o)
        {
            var curTime = GameTime();
            //var delta = curTime - LastUpdateTime;
            if (RefreshObjs != null) RefreshObjs(this, curTime - _lastUpdateTime);
            _lastUpdateTime = curTime;
            _refreshTimer.Change(100, Timeout.Infinite);
        }

        private Timer _refreshTimer;


        /// <summary>
        ///     Start and run the simulation for this system.
        /// </summary>
        public void GameThreadRun()
        {

            _refreshTimer = new Timer(OnRefreshObjects, null, 100, Timeout.Infinite);
            Server.Shutdown += Server_Shutdown;
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            CurrentTime = Utilities.GetTime();

            //AddTimer(new UpdateMonitoredObjs(this));
            //AddTimer(new SpawnBackgroundNPCs(this));

            //bool running = true;
            //while (running)
            //{
            //    // Calculate the delta time.
            //    double delta = Utilities.GetTime() - CurrentTime;
            //    CurrentTime += delta;

            //    // Call the reactor to return the next event the process
            //    // and run any timer functions.
            //    ReactorEvent next_event = Run(CurrentTime, delta);
            //    if (next_event is ReactorShutdownEvent)
            //    {
            //        running = false;
            //    }
            //    else if (next_event is DPGRAddCash)
            //    {
            //        var revent = next_event as DPGRAddCash;
            //        if (revent.player.Runner != this)
            //        {
            //            Log.AddLog(LogType.FL_MSG, "Warning: requeued rx msg onto changed runner");
            //            revent.player.Runner.AddEvent(revent);
            //        }
            //        else
            //        {
            //            revent.player.Money += revent.cash;
            //            Packets.SendSetMoney(revent.player);
            //        }
            //    }
            //    else if (next_event is DPGRSetCash)
            //    {
            //        var revent = next_event as DPGRSetCash;
            //        if (revent.player.Runner != this)
            //        {
            //            Log.AddLog(LogType.FL_MSG, "Warning: requeued rx msg onto changed runner");
            //            revent.player.Runner.AddEvent(revent);
            //        }
            //        else
            //        {
            //            revent.player.Money = revent.cash;
            //            Packets.SendSetMoney(revent.player);
            //        }
            //    }
            //    else if (next_event is DPGRBeam)
            //    {
            //        var revent = next_event as DPGRBeam;
            //        if (revent.Player.Runner != this)
            //        {
            //            Log.AddLog(LogType.FL_MSG, "Warning: requeued rx msg onto changed runner");
            //            revent.Player.Runner.AddEvent(revent);
            //        }
            //        else
            //        {
            //            revent.Player.MonitoredObjs.Clear();
            //            revent.Player.Ship.Basedata = revent.TargetBase;
            //            revent.Player.Ship.RespawnBasedata = revent.TargetBase;
            //            revent.Player.Ship.System = UniverseDB.FindSystem(revent.TargetBase.SystemID);
            //            revent.Player.Ship.IsDestroyed = false;
            //            Packets.SendServerLand(revent.Player, revent.Player.Ship, 0, revent.Player.Ship.Basedata.BaseID);
            //            revent.Player.Update();
            //        }
            //    }
            //}
        }

        void Server_Shutdown(object sender, EventArgs e)
        {
            //TODO: stop pipe
        }

        void player_RxMsgToRunner(Player.Player sender, byte[] message)
        {
            //TODO: move to player?
            //var revent = next_event as DPGameRunnerRxMsgEvent;

            // Process a message from this player. If the message isn't for this runner because
            // the runner ownership changes, requeue in the right place.
            if (sender.Runner != this) return;
            sender.RxMsgFromClient(message);
        }

        /// <summary>
        ///     Send a dplay message to a client.
        /// </summary>
        /// <param name="player"></param>
        /// <param name="pkt"></param>
        public void SendMessage(Player.Player player, byte[] pkt)
        {
            Server.SendMessage(player.DPSess, pkt);
        }



        public double GameTime()
        {
            return CurrentTime;
        }

        /// <summary>
        ///     An NPC or player ship or armed solar has been created. Allocate an object ID and add it
        ///     to the object list.
        /// </summary>
        /// <param name="obj"></param>
        public void CreateSimObject(SimObject obj)
        {
            if (Objects.Count > 10000)
            {
                Log.AddLog(LogType.FL_MSG, "error: maximum object count exceeded in {0}",System.Nickname);
                return;
            }


                // Find an unused object ID.
                if (_lastAllocatedObjid > _baseObjid + 10000)
                    obj.Objid = _baseObjid;

                while (Objects.ContainsKey(_lastAllocatedObjid))
                    _lastAllocatedObjid++;

                obj.Objid = _lastAllocatedObjid;
            

            if (!(obj is Object.Solar.Solar))
                AffObjects.Add(_lastAllocatedObjid, obj);

            // Allocate the object id and add this to the object list
            Objects.Add(_lastAllocatedObjid, obj);

            foreach (Player.Player player in Players.Values)
                CheckIfShouldCreateDestroyObject(player, obj);

            RefreshObjs += obj.HandleTimerEvent;
            //AddTimer(obj);
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
                player.SendMsgToClient(BuildDestroyObj(obj, true));
            }

            AffObjects.Remove(obj.Objid);
            Objects.Remove(obj.Objid);
            obj.Objid = 0;
            RefreshObjs -= obj.HandleTimerEvent;
            //DelTimer(obj);
        }

        /// <summary>
        ///     For the specified player, check the single objects and spawn or destroy
        ///     any objects in range of the player.
        /// </summary>
        /// <param name="player"></param>
        /// <param name="obj"></param>
        public void CheckIfShouldCreateDestroyObject(Player.Player player, SimObject obj)
        {
            // Ignore the player's own ship or an uninitialised ship
            if (player.Ship.Objid == obj.Objid || player.Ship.Objid == 0)
                return;

            if (obj is Object.Solar.Solar)
                return;

            var shouldMonitor = true;
            if (obj is Old.Object.Ship.Ship && (obj as Old.Object.Ship.Ship).Basedata != null)
                shouldMonitor = false;
            else if (player.Ship.Basedata != null)
                shouldMonitor = false;
                // TODO: use bucket?
            else if (!player.Ship.ScanBucket.Contains(obj))
                shouldMonitor = false;

            if (shouldMonitor)
            {
                if (player.MonitoredObjs.ContainsKey(obj.Objid)) return;

                player.MonitoredObjs[obj.Objid] = obj;
                if (obj is Old.Object.Ship.Ship)
                {
                    player.SendMsgToClient(BuildCreateShip(obj as Old.Object.Ship.Ship));
                    Packets.SendSetReputation(player, obj as Old.Object.Ship.Ship);
                }
                else if (obj is Missile)
                    player.SendMsgToClient(BuildCreateMissile(obj as Missile));
                else if (obj is CounterMeasure)
                    player.SendMsgToClient(BuildCreateCounterMeasure(obj as CounterMeasure));
                else if (obj is Loot)
                    Packets.SendCreateLoot(player, player.Ship, obj as Loot);
            }
            else
            {
                if (!player.MonitoredObjs.ContainsKey(obj.Objid)) return;
                player.MonitoredObjs.Remove(obj.Objid);
                player.SendMsgToClient(BuildDestroyObj(obj, true));
            }
        }

        /// <summary>
        ///     For the specified player, check all simulation objects and spawn or destroy
        ///     any objects in range of the player.
        /// </summary>
        /// <param name="player"></param>
        public void CheckMonitoredObjsList(Player.Player player)
        {
            foreach (var obj in Objects.Values)
            {
                CheckIfShouldCreateDestroyObject(player, obj);
            }

            // Remove any inactive sim objects from monitoring.
            var deleted_objs = new List<uint>();
            foreach (var obj in player.MonitoredObjs.Values)
            {
                if (!Objects.ContainsKey(obj.Objid))
                {
                    deleted_objs.Add(obj.Objid);
                    player.SendMsgToClient(BuildDestroyObj(obj, true));
                }
            }

            foreach (uint objid in deleted_objs)
            {
                player.MonitoredObjs.Remove(objid);
            }
        }

        public SimObject FindObject(uint objid)
        {
            if (Objects.ContainsKey(objid))
                return Objects[objid];
            return null;
        }

        public Old.Object.Ship.Ship FindShip(uint objid)
        {
            if (!Objects.ContainsKey(objid))
                return null;
            return (Old.Object.Ship.Ship)AffObjects[objid];
        }

       
        /// <summary>
        ///     FLPACKET_COMMON_SETTARGET
        ///     TODO: If the target is a player, send a notification to the player to turn on/off
        ///     the missile lock alert.
        /// </summary>
        public void NotifyOnSetTarget(uint objid, uint target_objid, uint target_subobjid)
        {
            byte[] omsg = {0x04, 0x01};
            FLMsgType.AddUInt32(ref omsg, objid);
            FLMsgType.AddUInt32(ref omsg, target_objid);
            FLMsgType.AddUInt32(ref omsg, target_subobjid);

            var ship = FindShip(target_objid);
            if (ship != null && ship.player != null)
            {
                ship.player.SendMsgToClient(omsg);
            }
        }


        public void NotifyOnSetHitPoints(uint objid, uint hpid, float hitPts, bool destroyed)
        {
            Log.AddLog(LogType.FL_MSG, "tx FLPACKET_SERVER_DAMAGEOBJ objid={0}", objid);

            byte[] omsg = {0x05, 0x02};
            FLMsgType.AddUInt32(ref omsg, objid);
            FLMsgType.AddUInt8(ref omsg, 0); // padding or dunno
            FLMsgType.AddUInt32(ref omsg, 0); // sender playerid
            FLMsgType.AddUInt32(ref omsg, 0); // sender objid
            FLMsgType.AddUInt16(ref omsg, 1); // count
            FLMsgType.AddUInt16(ref omsg, hpid);
            FLMsgType.AddUInt8(ref omsg, destroyed ? 2u : 0u); // fixme: come back!! was 2u
            FLMsgType.AddFloat(ref omsg, hitPts);

            foreach (Player.Player player in Players.Values)
            {
                if (player.MonitoredObjs.ContainsKey(objid))
                {
                    player.SendMsgToClient(omsg);
                }
                else if (player.Ship.Objid == objid)
                {
                    player.SendMsgToClient(omsg);
                }
            }
        }

        public void NotifyOnSetHitPoints(uint objid, List<DamageListItem> dmg_items)
        {
            Log.AddLog(LogType.FL_MSG, "tx FLPACKET_SERVER_DAMAGEOBJ objid={0}", objid);

            byte[] omsg = {0x05, 0x02};
            FLMsgType.AddUInt32(ref omsg, objid);
            FLMsgType.AddUInt8(ref omsg, 0); // padding or dunno
            FLMsgType.AddUInt32(ref omsg, 0); // sender playerid
            FLMsgType.AddUInt32(ref omsg, 0); // sender objid
            FLMsgType.AddUInt16(ref omsg, (uint) dmg_items.Count);
            foreach (var dmgItem in dmg_items)
            {
                FLMsgType.AddUInt16(ref omsg, dmgItem.hpid);
                FLMsgType.AddUInt8(ref omsg, dmgItem.destroyed ? 2u : 0u);
                FLMsgType.AddFloat(ref omsg, dmgItem.hit_pts);
            }

            foreach (Player.Player player in Players.Values)
            {
                if (player.MonitoredObjs.ContainsKey(objid))
                {
                    player.SendMsgToClient(omsg);
                }
                else if (player.Ship.Objid == objid)
                {
                    player.SendMsgToClient(omsg);
                }
            }
        }

        public void NotifyOnObjDestroy(SimObject obj)
        {
            Log.AddLog(LogType.FL_MSG2, "tx FLPACKET_SERVER_DESTROYOBJECT objid={0}", obj.Objid);

            byte[] omsg = BuildDestroyObj(obj, false);
            foreach (var player in Players.Values)
            {
                if (player.MonitoredObjs.ContainsKey(obj.Objid))
                {
                    player.SendMsgToClient(omsg);
                }
                else if (player.Ship.Objid == obj.Objid)
                {
                    player.SendMsgToClient(omsg);
                }
            }
        }

        /// <summary>
        /// Sends position, orientation, Throttle and UpdateTime of a SimObject.
        /// </summary>
        /// <param name="obj">Object to send</param>
        public void NotifyOnObjUpdate(SimObject obj)
        {
            Log.AddLog(LogType.FL_MSG2, "tx FLPACKET_COMMON_UPDATEOBJECT");

            byte[] omsg = {0x01, 0x01};
            FLMsgType.AddUInt8(ref omsg, 0xFF); // dunno
            FLMsgType.AddUInt32(ref omsg, obj.Objid);
            FLMsgType.AddFloat(ref omsg, (float) obj.Position.x);
            FLMsgType.AddFloat(ref omsg, (float) obj.Position.y);
            FLMsgType.AddFloat(ref omsg, (float) obj.Position.z);
            Quaternion q = Quaternion.MatrixToQuaternion(obj.Orientation);
            FLMsgType.AddInt8(ref omsg, (int) (q.I*127));
            FLMsgType.AddInt8(ref omsg, (int) (q.J*127));
            FLMsgType.AddInt8(ref omsg, (int) (q.K*127));
            FLMsgType.AddInt8(ref omsg, (int) (q.W*127));
            FLMsgType.AddInt8(ref omsg, (int) (obj.Throttle*127));
            FLMsgType.AddFloat(ref omsg, (float) obj.UpdateTime);

            foreach (Player.Player player in Players.Values)
            {
                if (player.MonitoredObjs.ContainsKey(obj.Objid))
                {
                    player.SendMsgToClient(omsg);
                }
            }
        }

        /// <summary>
        /// Sends cruse state of a ship.
        /// </summary>
        /// <param name="ship">Ship</param>
        /// <param name="state">Cruise on/off</param>
        /// <param name="dunno">TODO: PROT: dunno</param>
        public void NotifyOnActivateCruise(Old.Object.Ship.Ship ship, bool state, uint dunno)
        {
            byte[] omsg = {0x0E, 0x01};
            FLMsgType.AddUInt32(ref omsg, ship.Objid);
            FLMsgType.AddUInt8(ref omsg, state ? 1u : 0u);
            FLMsgType.AddUInt8(ref omsg, dunno);

            foreach (Player.Player player in Players.Values)
            {
                if (player.MonitoredObjs.ContainsKey(ship.Objid))
                {
                    player.SendMsgToClient(omsg);
                }
            }
        }

        /// <summary>
        /// Sends thruster state of a ship.
        /// </summary>
        /// <param name="ship">Ship</param>
        /// <param name="state">Thruster on/off</param>
        public void NotifyOnActivateThrusters(Old.Object.Ship.Ship ship, bool state)
        {
            byte[] omsg = {0x15, 0x01};
            FLMsgType.AddUInt32(ref omsg, ship.Objid);
            FLMsgType.AddUInt8(ref omsg, state ? 1u : 0u);

            foreach (Player.Player player in Players.Values)
            {
                if (player.MonitoredObjs.ContainsKey(ship.Objid))
                {
                    player.SendMsgToClient(omsg);
                }
            }
        }

        public void NotifyOnGoTradelane(Old.Object.Ship.Ship ship, uint ring1, uint ring2, uint dunno)
        {
            byte[] omsg = {0x0F, 0x01};
            FLMsgType.AddUInt32(ref omsg, ship.Objid);
            FLMsgType.AddUInt32(ref omsg, ring1);
            FLMsgType.AddUInt32(ref omsg, ring2);
            FLMsgType.AddUInt8(ref omsg, dunno);

            foreach (Player.Player player in Players.Values)
            {
                if (player.MonitoredObjs.ContainsKey(ship.Objid))
                {
                    player.SendMsgToClient(omsg);
                }
            }
        }

        public void NotifyOnStopTradelane(Old.Object.Ship.Ship ship, uint ring1, uint ring2)
        {
            byte[] omsg = {0x10, 0x01};
            FLMsgType.AddUInt32(ref omsg, ship.Objid);
            FLMsgType.AddUInt32(ref omsg, ring1);
            FLMsgType.AddUInt32(ref omsg, ring2);

            foreach (Player.Player player in Players.Values)
            {
                if (player.MonitoredObjs.ContainsKey(ship.Objid))
                {
                    player.SendMsgToClient(omsg);
                }
            }
        }

        /* public void NotifyOnServerLand(Ship ship, uint baseid, uint solarid)
        {
            log.AddLog(LogType.FL_MSG, "tx FLPACKET_SERVER_LAND objid={0} targetid={1} baseid={2}", ship.objid, solarid, baseid));

            byte[] omsg = { 0x0B, 0x02 };
            FLMsgType.AddUInt32(ref omsg, ship.objid);
            FLMsgType.AddUInt32(ref omsg, solarid);
            FLMsgType.AddUInt32(ref omsg, baseid);

            foreach (Player player in players.Values)
            {
                if (player.monitored_objs.ContainsKey(ship.objid))
                {
                    player.SendMsgToClient(omsg);
                }
            }
        } */

        public void NotifyOnShipFiring(Old.Object.Ship.Ship ship, Vector targetPosition, List<uint> hpids)
        {
            byte[] omsg = {0x02, 0x01};
            FLMsgType.AddUInt32(ref omsg, ship.Objid);

            FLMsgType.AddUInt8(ref omsg, 0);
            FLMsgType.AddFloat(ref omsg, (float) targetPosition.x);
            FLMsgType.AddFloat(ref omsg, (float) targetPosition.y);
            FLMsgType.AddFloat(ref omsg, (float) targetPosition.z);
            FLMsgType.AddUInt8(ref omsg, (uint) hpids.Count);
            foreach (var hpid in hpids)
                FLMsgType.AddUInt16(ref omsg, hpid);

            foreach (Player.Player player in Players.Values)
            {
                if (player.MonitoredObjs.ContainsKey(ship.Objid))
                {
                    player.SendMsgToClient(omsg);
                }
            }
        }


        /// <summary>
        ///     Send a FLPACKET_SERVER_ACTIVATEOBJECT command to the player to
        ///     activate or deactivate an animation or effect on an object.
        /// </summary>
        /// <param name="objid">The object id</param>
        /// <param name="activate">Activate or deactivate the animation/effect</param>
        /// <param name="index">
        ///     The index of the animation starting from 0. Most
        ///     objects only have a single animation to trigger
        /// </param>
        // fixme: this should be a notify
        public void SendActivateObject(DockingObject obj, bool activate, uint index)
        {
            byte[] omsg = {0x0A, 0x02};
            FLMsgType.AddUInt32(ref omsg, obj.Solar.Objid);
            FLMsgType.AddUInt8(ref omsg, (activate ? 1u : 0u));
            FLMsgType.AddUInt32(ref omsg, index);

            // fixme: send activate only if any player is close to this solar.
            foreach (Player.Player player in Players.Values)
            {
                player.SendMsgToClient(omsg);
            }
        }

        // FLPACKET_SERVER_CREATESHIP
        public byte[] BuildCreateShip(Old.Object.Ship.Ship ship)
        {
            Log.AddLog(LogType.FL_MSG, "tx FLPACKET_SERVER_CREATESHIP objid={0}", ship.Objid);

            byte[] omsg = {0x04, 0x02};
            FLMsgType.AddUInt32(ref omsg, ship.Objid);
            FLMsgType.AddUInt16(ref omsg, ship.Arch.SmallID);
            FLMsgType.AddUInt32(ref omsg, 0);
            FLMsgType.AddUInt32(ref omsg, ship.player != null ? ship.player.FLPlayerID : 0);
            FLMsgType.AddUInt32(ref omsg, ship.com_body);
            FLMsgType.AddUInt32(ref omsg, ship.com_head);

            FLMsgType.AddUInt8(ref omsg, (uint) ship.Accessories.Count);
            foreach (uint accessory in ship.Accessories)
                FLMsgType.AddUInt32(ref omsg, accessory);

            FLMsgType.AddUInt32(ref omsg, ship.voiceid);

            FLMsgType.AddFloat(ref omsg, (float) ship.Position.x);
            FLMsgType.AddFloat(ref omsg, (float) ship.Position.y);
            FLMsgType.AddFloat(ref omsg, (float) ship.Position.z);

            Quaternion q = Quaternion.MatrixToQuaternion(ship.Orientation);
            FLMsgType.AddInt8(ref omsg, (int) (q.I*127));
            FLMsgType.AddInt8(ref omsg, (int) (q.J*127));
            FLMsgType.AddInt8(ref omsg, (int) (q.K*127));
            FLMsgType.AddInt8(ref omsg, (int) (q.W*127));

            FLMsgType.AddUInt8(ref omsg, (uint) (ship.Health*255));

            FLMsgType.AddUInt16(ref omsg, (uint) (ship.Items.Count));
            foreach (ShipItem item in ship.Items.Values)
            {
                byte flag = 0;

                if (item.mounted)
                    flag |= 0x01;

                if (item.mission)
                    flag |= 0x02;

                if (item.count == 1)
                    flag |= 0x80;
                else
                    flag |= 0x04;

                if (item.health == 1.0f)
                    flag |= 0x40;

                if (item.hpname.Length > 0)
                    flag |= 0x10;
                else
                    flag |= 0x20;

                FLMsgType.AddUInt8(ref omsg, flag);

                if (item.count != 1)
                    FLMsgType.AddUInt32(ref omsg, item.count);

                if (item.health != 1.0f)
                    FLMsgType.AddUInt8(ref omsg, (uint) (item.health*255));

                FLMsgType.AddUInt16(ref omsg, item.arch.SmallID);
                FLMsgType.AddUInt8(ref omsg, item.hpid);

                if (item.hpname.Length > 0)
                    FLMsgType.AddAsciiStringLen8(ref omsg, item.hpname + "\0");
            }

            FLMsgType.AddUInt8(ref omsg, (uint) (ship.cols.Count));
            foreach (CollisionGroup col in ship.cols)
            {
                FLMsgType.AddUInt8(ref omsg, col.id);
                FLMsgType.AddUInt8(ref omsg, (uint) (col.health*col.max_hit_pts*255));
            }

            FLMsgType.AddUInt8(ref omsg, (ship.player != null) ? 4u : 0u); // flag
            FLMsgType.AddFloat(ref omsg, 0); // x
            FLMsgType.AddFloat(ref omsg, 0); // y
            FLMsgType.AddFloat(ref omsg, 0); // z
            FLMsgType.AddInt8(ref omsg, 0);
            FLMsgType.AddUInt16(ref omsg, 0); // dunno?
            FLMsgType.AddUInt8(ref omsg, ship.Rank);

            if (ship.player != null)
            {
                FLMsgType.AddUInt8(ref omsg, ship.player.FLPlayerID);
                FLMsgType.AddUInt16(ref omsg, 0);
                FLMsgType.AddUnicodeStringLen8(ref omsg, ship.player.Name);
            }
            else
            {
                var patrol_name = new FLFormatString(0x3f20);
                patrol_name.AddString(0x3016b);
                patrol_name.AddString(0x4074);
                patrol_name.AddString(0x30401);
                patrol_name.AddNumber(0x09);
                FLMsgType.AddArray(ref omsg, patrol_name.GetBytes());

                var ship_name = new FLFormatString(0x3f21);
                ship_name.AddString(0x301a4);
                ship_name.AddString(0x37bac);
                ship_name.AddString(0x37c2b);
                FLMsgType.AddArray(ref omsg, ship_name.GetBytes());
            }

            // The faction associated with the ship. For player ships this can be
            // -1 but for NPCs it needs to be set to a faction ID or the NPC will
            // not have a name shown in space or in the radar/scanner
            FLMsgType.AddUInt32(ref omsg, ship.faction.FactionID);

            // The reputation with reference to the faction..but it doesn't seem to
            // do much
            FLMsgType.AddInt8(ref omsg, -127);
            return omsg;
        }

        private byte[] BuildCreateMissile(Missile missile)
        {
            Log.AddLog(LogType.FL_MSG, "tx FLPACKET_SERVER_CREATEGUIDED objid={0}", missile.Objid);

            byte[] omsg = {0x37, 0x02};
            FLMsgType.AddUInt16(ref omsg, missile.munition_arch.SmallID);
            FLMsgType.AddUInt32(ref omsg, missile.owner_objid);
            FLMsgType.AddUInt16(ref omsg, missile.hpid);
            FLMsgType.AddUInt32(ref omsg, missile.target_objid); // missile.target_objid
            FLMsgType.AddUInt16(ref omsg, missile.target_subobjid); // missile.target_subobjid
            FLMsgType.AddUInt32(ref omsg, missile.Objid);
            FLMsgType.AddFloat(ref omsg, (float) missile.Position.x);
            FLMsgType.AddFloat(ref omsg, (float) missile.Position.y);
            FLMsgType.AddFloat(ref omsg, (float) missile.Position.z);
            Quaternion q = Quaternion.MatrixToQuaternion(missile.Orientation);
            FLMsgType.AddInt8(ref omsg, (int) (q.I*127));
            FLMsgType.AddInt8(ref omsg, (int) (q.J*127));
            FLMsgType.AddInt8(ref omsg, (int) (q.K*127));
            FLMsgType.AddInt8(ref omsg, (int) (q.W*127));
            return omsg;
        }

        private byte[] BuildCreateCounterMeasure(CounterMeasure cm)
        {
            Log.AddLog(LogType.FL_MSG, "tx FLPACKET_SERVER_CREATECOUNTER objid={0}", cm.Objid);

            byte[] omsg = {0x2D, 0x02};

            FLMsgType.AddUInt32(ref omsg, cm.munition_arch.ArchetypeID);
            FLMsgType.AddUInt32(ref omsg, cm.Objid);
            FLMsgType.AddUInt32(ref omsg, 0);
            FLMsgType.AddUInt32(ref omsg, cm.owner_objid);
            FLMsgType.AddUInt16(ref omsg, cm.hpid);
            Quaternion q = Quaternion.MatrixToQuaternion(cm.Orientation);
            FLMsgType.AddFloat(ref omsg, (float) q.I);
            FLMsgType.AddFloat(ref omsg, (float) q.J);
            FLMsgType.AddFloat(ref omsg, (float) q.K);
            FLMsgType.AddFloat(ref omsg, (float) q.W);
            FLMsgType.AddFloat(ref omsg, (float) cm.Position.x);
            FLMsgType.AddFloat(ref omsg, (float) cm.Position.y);
            FLMsgType.AddFloat(ref omsg, (float) cm.Position.z);

            FLMsgType.AddFloat(ref omsg, (float) cm.velocity.x);
            FLMsgType.AddFloat(ref omsg, (float) cm.velocity.y);
            FLMsgType.AddFloat(ref omsg, (float) cm.velocity.z);

            FLMsgType.AddFloat(ref omsg, (float) cm.angular_velocity.x);
            FLMsgType.AddFloat(ref omsg, (float) cm.angular_velocity.y);
            FLMsgType.AddFloat(ref omsg, (float) cm.angular_velocity.z);
            return omsg;
        }

        // FLPACKET_SERVER_DESTROYSHIP
        /// <summary>
        ///     Build a destroy ship packet.
        /// </summary>
        /// <param name="ship"></param>
        /// <param name="delete">If delete, just remove the object. If false, make it explode.</param>
        /// <returns></returns>
        public byte[] BuildDestroyObj(SimObject obj, bool delete)
        {
            Log.AddLog(LogType.FL_MSG, "tx FLPACKET_SERVER_DESTROYOBJECT objid={0}", obj.Objid);

            byte[] omsg = {0x06, 0x02};
            FLMsgType.AddUInt32(ref omsg, obj.Objid);
            FLMsgType.AddUInt8(ref omsg, (delete ? 0u : 1u));
            return omsg;
        }

        /// <summary>
        ///     Information to be shown in the player list.
        ///     The name, rank and system fields may be accessed from any thread.
        ///     The player reference is safe to access but the name, rank and other contents
        ///     apart from the "runner" is not safe to access except by the owning runner.
        /// </summary>
        public class PlayerListItem
        {
            public uint FlPlayerID;
            public Group Group;
            public Group GroupInvited;
            public string Name;
            public Player.Player Player;
            public uint Rank;
            public StarSystem System;
        }

        /// <summary>
        ///     This class checks each spawn zone in the system and spawns NPCs
        ///     if the zone doesn't have the specified amount and players are
        ///     in the area.
        /// </summary>
        private class SpawnBackgroundNPCs : ReactorTimer
        {
            private readonly DPGameRunner runner;
            private Random rand = new Random();

            public SpawnBackgroundNPCs(DPGameRunner runner)
            {
                this.runner = runner;
                ExpireAfter(0.5);
            }

            public override void HandleTimerEvent(double deltaSeconds)
            {
                if (runner.System == UniverseDB.FindSystem("li01"))
                {
                    //TODO: AI debug here
                    for (int i = 0; i < 1; i++)
                    {
                        var npc = new Old.Object.Ship.Ship(runner);
                        npc.AI = new AI.DebugAI(npc);
                        npc.Arch = ArchetypeDB.Find(FLUtility.CreateID("dsy_csv"));
                        if (npc.Arch == null)
                            return;

                        npc.Position = new Vector(-30000 + i*300, i*100, -25000);
                        //npc.orientation = ;
                        npc.Rank = 20;
                        npc.System = runner.System;
                        npc.Health = 1.0f;
                        npc.faction = UniverseDB.FindFaction("fc_wild");
                        Loadout loadout = UniverseDB.FindLoadout("fc_j_ge_csv_loadout01");
                        if (loadout != null)
                        {
                            uint hpid = 34;
                            foreach (ShipItem item in loadout.Items)
                            {
                                var new_item = new ShipItem();
                                new_item.arch = item.arch;
                                new_item.count = item.count;
                                new_item.health = 1.0f;
                                new_item.hpid = hpid++;
                                new_item.hpname = item.hpname;
                                new_item.mounted = item.mounted;
                                npc.Items.Add(new_item.hpid, new_item);
                            }
                        }
                        npc.InitialiseEquipmentSimulation();
                        runner.CreateSimObject(npc);
                    }
                }
                //    int total = 0;
                //    if (runner.players.Count > 0)
                //    {
                //        if (delta_seconds > 1.5)
                //            runner.log.AddLog(LogType.FL_MSG, "bad delta " + delta_seconds);

                //        // wow, this'll really suck if there are lots of NPCs
                //        foreach (Zone z in runner.system.zones)
                //        {
                //            if (z.shape != null && z.density > 0)
                //            {
                //                while (z.interference < z.density) // borrow this
                //                {
                //                    Ship npc = new Ship(runner);
                //                    npc.position = z.shape.position;
                //                    npc.orientation = z.shape.orientation;
                //                    npc.rank = 20;
                //                    npc.arch = ArchetypeDB.Find(FLUtility.CreateID("dsy_csv"));
                //                    npc.system = runner.system;
                //                    npc.health = 1.0f;
                //                    runner.CreateSimObject(npc);

                //                    z.interference++;
                //                    total++;
                //                }
                //            }
                //        }

                //        int working_npcs = 0;
                //        foreach (SimObject o in runner.objects.Values)
                //        {
                //            if (o.health > 0)
                //            {
                //                working_npcs++;

                //                foreach (Player player in runner.players.Values)
                //                {
                //                    if (player.ship != o)
                //                    {
                //                        Vector position = player.ship.position;
                //                        position.x += rand.Next(100);
                //                        position.z += rand.Next(100);
                //                        o.SetUpdateObject(position, player.ship.orientation, 1.0f, 0);
                //                    }
                //                }
                //            }
                //        }

                //        runner.log.AddLog(LogType.GENERAL, "system={0} npcs={1} objects={2} running={3}",
                //            runner.system.nickname, total, runner.objects.Count, working_npcs));

                //    }

                //    ExpireAfter(1);
            }
        }

        /// <summary>
        ///     This class checks for objects coming into or leaving range of players
        ///     and setting the flags to determine if events should be passed to
        ///     players.
        /// </summary>
        private class UpdateMonitoredObjs : ReactorTimer
        {
            private readonly DPGameRunner _runner;

            public UpdateMonitoredObjs(DPGameRunner runner)
            {
                _runner = runner;
                ExpireAfter(0.2);
            }

            public override void HandleTimerEvent(double deltaSeconds)
            {
                foreach (Player.Player player in _runner.Players.Values.Where(player => player.Runner == _runner))
                {
                    _runner.CheckMonitoredObjsList(player);
                }

                ExpireAfter(0.2);
            }
        }
    }

    internal class DPGRSetCash : ReactorEvent
    {
        public Int32 cash;
        public Player.Player player;

        public DPGRSetCash(Player.Player player, Int32 cash)
        {
            this.player = player;
            this.cash = cash;
        }
    }

    internal class DPGRAddCash : ReactorEvent
    {
        public Int32 cash;
        public Player.Player player;

        public DPGRAddCash(Player.Player player, Int32 cash)
        {
            this.player = player;
            this.cash = cash;
        }
    }

    internal class DPGRBeam : ReactorEvent
    {
        public Player.Player Player;
        public BaseData TargetBase;

        public DPGRBeam(Player.Player player, BaseData target_base)
        {
            Player = player;
            TargetBase = target_base;
        }
    }
}