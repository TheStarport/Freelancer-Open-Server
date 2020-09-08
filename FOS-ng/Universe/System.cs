using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FLServer.Object;
using FLServer.Ship;
using FOS_ng.DirectplayServer;
using FOS_ng.Logging;
using FOS_ng.Objects;
using FOS_ng.Objects.Ship;
using FOS_ng.Objects.Solar;
using FOS_ng.Player;

namespace FOS_ng.Universe
{
    public class System
    {

        /// <summary>
        ///     The starting objid for allocations.
        /// </summary>
        private readonly uint _baseObjid;

        /// <summary>
        ///     The ID of the last created object.
        /// </summary>
        private uint _lastAllocatedObjid;

        /// <summary>
        /// Players in current system.
        /// </summary>
        public Dictionary<uint, Player.Player> SysPlayers = new Dictionary<uint, Player.Player>();


        /// <summary>
        ///     Any space object including ships, solars that exists in the simulation.
        /// </summary>
        public Dictionary<uint, SimObject> Objects = new Dictionary<uint, SimObject>();

        /// <summary>
        ///     Any dynamic (moving) object.
        /// </summary>
        public Dictionary<uint, SimObject> AffObjects = new Dictionary<uint, SimObject>();

        public readonly string Nickname;

        public System(uint baseObjID)
        {
            _baseObjid = baseObjID;
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
                Logger.AddLog(LogType.FLMsg, "error: maximum object count exceeded in {0}", Nickname);
                return;
            }


            // Find an unused object ID.
            if (_lastAllocatedObjid > _baseObjid + 10000)
                obj.Objid = _baseObjid;

            while (Objects.ContainsKey(_lastAllocatedObjid))
                _lastAllocatedObjid++;

            obj.Objid = _lastAllocatedObjid;


            if (!(obj is Solar))
                AffObjects.Add(obj.Objid, obj);

            // Allocate the object id and add this to the object list
            Objects.Add(obj.Objid, obj);

            foreach (Player.Player player in SysPlayers.Values)
                CheckIfShouldCreateDestroyObject(player, obj);

            AddTimer(obj);
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

            if (obj is Solar)
                return;

            var shouldMonitor = true;
            //if (obj is Ship.Ship && (obj as Ship.Ship).Basedata != null)
                //shouldMonitor = false;
            //else 
            if (player.Ship.Basedata != null)
                shouldMonitor = false;
            else if (!player.Ship.ScanBucket.Contains(obj))
                shouldMonitor = false;

            if (shouldMonitor)
            {
                if (player.MonitoredObjs.ContainsKey(obj.Objid)) return;

                player.MonitoredObjs[obj.Objid] = obj;
                if (obj is Ship)
                {
                    player.SendMsgToClient(BuildCreateShip(obj as Ship.Ship));
                    player.SendSetReputation(obj as Ship.Ship);
                }
                else if (obj is Missile)
                    player.SendMsgToClient(BuildCreateMissile(obj as Missile));
                else if (obj is CounterMeasure)
                    player.SendMsgToClient(BuildCreateCounterMeasure(obj as CounterMeasure));
                else if (obj is Loot)
                    player.SendMsg(Packets.CreateLoot(player.Ship, obj as Loot));
            }
            else
            {
                if (!player.MonitoredObjs.ContainsKey(obj.Objid)) return;
                player.MonitoredObjs.Remove(obj.Objid);
                player.SendMsg(Packets.DestroyObj(obj.Objid, true));
            }
        }
    }
}
