using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FLServer.DataWorkers;
using FLServer.Munition;
using FLServer.Object.Base;
using FLServer.Object.Solar;
using FLServer.Old.Object.Ship;
using FLServer.Player;
using FLServer.Ship;
using FLServer.Physics;
using Archetype = FLServer.DataWorkers.Archetype;

namespace FLServer
{
    internal class DPCInBaseState : IPlayerState
    {
        private static DPCInBaseState _instance;

        public string StateName()
        {
            return "in-base-state";
        }

        public void EnterState(Player.Player player)
        {
            player.MonitoredObjs.Clear();
            Packets.SendCharInfoRequestResponse(player);
        }

        public void RxMsgFromClient(Player.Player player, byte[] msg)
        {
            if (msg.Length == 1 && msg[0] == 0x01)
            {
                // Keepalive
                // fixme: reset the keep alive timer. We kill the connection more quickly than dplay.
                byte[] omsg = {0xFF};
                player.SendMsgToClient(omsg);
            }
            else if (msg.Length >= 2)
            {
                int type = msg[0] << 8 | msg[1];
                switch (type)
                {
                    case 0x0101:
                        RxCommonUpdateObject(player, msg);
                        break;
                    case 0x0201:
                        RxCommonFireWeapon(player, msg);
                        break;
                    case 0x0401:
                        RxSetTarget(player, msg);
                        break;
                    case 0x0501:
                        RxChat(player, msg);
                        break;
                    case 0x0801:
                        RxActivateEquip(player, msg);
                        break;
                    case 0x0E01:
                        RxActivateCruise(player, msg);
                        break;
                    case 0x0F01:
                        RxGoTradelane(player, msg);
                        break;
                    case 0x1001:
                        RxStopTradelane(player, msg);
                        break;
                    case 0x1101:
                        RxSetWeaponsGroup(player, msg);
                        break;
                    case 0x1301:
                        RxSetVisitedState(player, msg);
                        break;
                    case 0x1401:
                        RxJettisionCargo(player, msg);
                        break;
                    case 0x1501:
                        RxActivateThrusters(player, msg);
                        break;
                    case 0x1601:
                        RxRequestBestPath(player, msg);
                        break;
                    case 0x1701:
                        RxRequestNavMap(player, msg);
                        break;
                    case 0x1801:
                        RxRequestPlayerStats(player, msg);
                        break;
                    case 0x1c01:
                        RxSetInterfaceState(player, msg);
                        break;

                    case 0x0303:
                        RxMunitionCollision(player, msg);
                        break;
                    case 0x0403:
                        RxRequestLaunch(player, msg);
                        break;
                    case 0x0503:
                        RxRequestCharInfo(player, msg);
                        break;
                    case 0x0603:
                        // fixme: rx select char, invalid in this state
                        break;
                    case 0x0703:
                        RxEnterBase(player, msg);
                        break;
                    case 0x0803:
                        RxRequestBaseInfo(player, msg);
                        break;
                    case 0x0903:
                        RxRequestLocationInfo(player, msg);
                        break;
                    case 0x0B03:
                        RxSystemSwitchOutComplete(player, msg);
                        break;
                    case 0x0C03:
                        RxObjectCollision(player, msg);
                        break;
                    case 0x0D03:
                        RxExitBase(player, msg);
                        break;
                    case 0x0E03:
                        RxEnterLocation(player, msg);
                        break;
                    case 0x0F03:
                        RxExitLocation(player, msg);
                        break;
                    case 0x1003:
                        RxRequestCreateShip(player, msg);
                        break;
                    case 0x1103:
                        RxGoodSell(player, msg);
                        break;
                    case 0x1203:
                        RxGoodBuy(player, msg);
                        break;
                    case 0x1303:
                        RxGFSelectObject(player, msg);
                        break;
                    case 0x1403:
                        RxMissionResponse(player, msg);
                        break;
                    case 0x1503:
                        RxRequestShipArch(player, msg);
                        break;
                    case 0x1603:
                        RxRequestEquipment(player, msg);
                        break;
                    case 0x1803:
                        RxRequestAddItem(player, msg);
                        break;
                    case 0x1903:
                        RxRequestRemoveItem(player, msg);
                        break;
                    case 0x1B03:
                        RxRequestSetCash(player, msg);
                        break;
                    case 0x1C03:
                        RxRequestChangeCash(player, msg);
                        break;
                    case 0x2F03:
                        RxSetManeuver(player, msg);
                        break;
                    case 0x3103:
                        RxRequestEvent(player, msg);
                        break;
                    case 0x3203:
                        RxRequestCancel(player, msg);
                        break;
                    case 0x3b03:
                        RxRequestSetHullStatus(player, msg);
                        break;
                    case 0x3E03:
                        RxLaunchComplete(player, msg);
                        break;
                    case 0x3F03:
                        RxClientHail(player, msg);
                        break;
                    case 0x4003:
                        RxRequestUseItem(player, msg);
                        break;
                    case 0x4303:
                        RxJumpInComplete(player, msg);
                        break;
                    case 0x4403:
                        RxRequestInvincibility(player, msg);
                        break;
                    default:
                        // Unexpected packet. Log and ignore it.
                        player.Log.AddLog(LogType.ERROR, "unexpected message: client rx {0}", msg);
                        break;
                }
            }
        }

        private static void RxCommonUpdateObject(Player.Player player, byte[] msg)
        {
            // FLPACKET_COMMON_UPDATEOBJECT
            int pos = 2;
            uint state = FLMsgType.GetUInt8(msg, ref pos);
            uint objid = FLMsgType.GetUInt32(msg, ref pos);
            float x = FLMsgType.GetFloat(msg, ref pos);
            float y = FLMsgType.GetFloat(msg, ref pos);
            float z = FLMsgType.GetFloat(msg, ref pos);
            float xr = FLMsgType.GetInt8(msg, ref pos)/127.0f;
            float yr = FLMsgType.GetInt8(msg, ref pos)/127.0f;
            float zr = FLMsgType.GetInt8(msg, ref pos)/127.0f;
            float wr = FLMsgType.GetInt8(msg, ref pos)/127.0f;
            float throttle = FLMsgType.GetInt8(msg, ref pos)/127.0f;
            float update_time = FLMsgType.GetFloat(msg, ref pos);

            //player.log.AddLogDebug(LogType.FL_MSG, "rx FLPACKET_COMMON_UPDATEOBJECT state={0} pos={1},{2},{3} dir={4},{5},{6},{7} throttle={8} update_time={9}",
            //    state, x, y, z, wr, xr, yr, zr, throttle, update_time));

            if (update_time < player.Ship.UpdateTime)
            {
                player.Log.AddLog(LogType.GENERAL, "Packet too old; discarding!");
                return;
            }

            var position = new Vector(x, y, z);
            Matrix orientation = Quaternion.QuaternionToMatrix(new Quaternion(xr, yr, zr, wr));
            player.Ship.SetUpdateObject(position, orientation, throttle, update_time);
        }

        private static void RxCommonFireWeapon(Player.Player player, byte[] msg)
        {
            // FLPACKET_COMMON_FIREWEAPON
            int pos = 2;
            var target_position = new Vector();
            uint objid = FLMsgType.GetUInt32(msg, ref pos);
            uint type = FLMsgType.GetUInt8(msg, ref pos);
            if ((type & 0x01) == 0x01)
                target_position.x = FLMsgType.GetInt8(msg, ref pos)/127.0f*64;
            else if ((type & 0x02) == 0x02)
                target_position.x = FLMsgType.GetInt16(msg, ref pos)/32767.0f*8192;
            else
                target_position.x = FLMsgType.GetFloat(msg, ref pos);
            if ((type & 0x04) == 0x04)
                target_position.y = FLMsgType.GetInt8(msg, ref pos)/127.0f*64;
            else if ((type & 0x08) == 0x08)
                target_position.y = FLMsgType.GetInt16(msg, ref pos)/32767.0f*8192;
            else
                target_position.y = FLMsgType.GetFloat(msg, ref pos);
            if ((type & 0x10) == 0x10)
                target_position.z = FLMsgType.GetInt8(msg, ref pos)/127.0f*64;
            else if ((type & 0x20) == 0x20) // BUG: it's read, but not actually written
                target_position.z = FLMsgType.GetInt16(msg, ref pos)/32767.0f*8192;
            else
                target_position.z = FLMsgType.GetFloat(msg, ref pos);

            player.Log.AddLog(LogType.FL_MSG, "rx FLPACKET_COMMON_FIREWEAPON pos={0},{1},{2}", target_position.x,
                target_position.y, target_position.z);

            uint count;
            var hpids = new List<uint>();
            if ((type & 0x40) == 0x40)
                count = 1;
            else
                count = FLMsgType.GetUInt8(msg, ref pos);
            while (count-- != 0)
            {
                uint hpid = FLMsgType.GetUInt16(msg, ref pos);

                ShipItem item = player.Ship.FindByHpid(hpid);
                if (item == null)
                {
                    player.Log.AddLog(LogType.CHEATING, "{0} fired unmounted weapon", player.Name);
                    //TODO: kick on cheating
                    return;
                }
                    

                if (item.arch is GunArchetype)
                {
                    // If the ammo for this gun has a motor then it's a missile
                    var gun_arch = item.arch as GunArchetype;
                    if (gun_arch.ProjectileArch is MunitionArchetype
                        && (gun_arch.ProjectileArch as MunitionArchetype).MotorArch != null)
                    {
                        var missile = new Missile(player.Runner, player.Ship, gun_arch, item, target_position, hpid);
                        player.Runner.CreateSimObject(missile);

                        if (!(gun_arch.ProjectileArch as MunitionArchetype).RequiresAmmo) continue;

                        // If the weapon has no ammo but has fired, kick and log.
                        if (item.count <= 0)
                        {
                            // fixme: kick and log
                        }
                            // Otherwise use ammo
                        else
                        {
                            item.count--;
                        }
                    }
                        // Otherwise this is just a gun so add the hpid to the list of weapons fired.
                    else
                    {
                        hpids.Add(hpid);
                    }
                }
                else if (item.arch is CounterMeasureDropperArchetype)
                {
                    var cmd_arch = item.arch as CounterMeasureDropperArchetype;
                    if (cmd_arch.ProjectileArch is CounterMeasureArchetype)
                    {
                        var cm_arch = cmd_arch.ProjectileArch as CounterMeasureArchetype;
                        var cm = new CounterMeasure(player.Runner, player.Ship, cmd_arch, item, hpid);
                        player.Runner.CreateSimObject(cm);
                    }
                }
                else if (item.arch is MineDropperArchetype)
                {
                }
                else
                {
                    return; //fixme log and kick
                }
            }

            player.Runner.NotifyOnShipFiring(player.Ship, target_position, hpids);
        }

        private static void RxSetTarget(Player.Player player, byte[] msg)
        {
            // FLPACKET_COMMON_SETTARGET
            int pos = 2;
            uint spaceobjid = FLMsgType.GetUInt32(msg, ref pos);
            uint target_objid = FLMsgType.GetUInt32(msg, ref pos);
            uint target_subobjid = FLMsgType.GetUInt32(msg, ref pos);

            player.Log.AddLog(LogType.FL_MSG,
                "rx FLPACKET_COMMON_SETTARGET spaceobjid={0} target_objid={1} target_subobjid={2}",
                spaceobjid, target_objid, target_subobjid);

            player.Ship.TargetObjID = target_objid;
            player.Ship.target_subobjid = target_subobjid;
        }

        private static void RxActivateEquip(Player.Player player, byte[] msg)
        {
            // FLPACKET_COMMON_ACTIVATEEQUIP
            int pos = 2;
            uint objid = FLMsgType.GetUInt32(msg, ref pos);
            uint hpid = FLMsgType.GetUInt16(msg, ref pos);
            bool state = FLMsgType.GetUInt8(msg, ref pos) != 0;

            ShipItem item = player.Ship.FindByHpid(hpid);
            if (item != null)
                item.activated = state;
        }

        private static void RxActivateCruise(Player.Player player, byte[] msg)
        {
            // FLPACKET_COMMON_ACTIVATECRUISE
            int pos = 2;
            uint objid = FLMsgType.GetUInt32(msg, ref pos);
            bool cruise = FLMsgType.GetUInt8(msg, ref pos) != 0;
            uint dunno = FLMsgType.GetUInt8(msg, ref pos);

            player.Log.AddLog(LogType.FL_MSG, "rx FLPACKET_COMMON_ACTIVATECRUISE objid={0} cruise={1} dunno={2}", objid,
                cruise, dunno);
            player.Runner.NotifyOnActivateCruise(player.Ship, cruise, dunno); // fixme, move into ship (for npcs)
        }

        private static void RxGoTradelane(Player.Player player, byte[] msg)
        {
            // FLPACKET_COMMON_GOTRADELANE
            int pos = 2;
            uint objid = FLMsgType.GetUInt32(msg, ref pos);
            uint ring1 = FLMsgType.GetUInt32(msg, ref pos);
            uint ring2 = FLMsgType.GetUInt32(msg, ref pos);
            uint dunno = FLMsgType.GetUInt8(msg, ref pos);
            player.Log.AddLog(LogType.FL_MSG, "rx FLPACKET_COMMON_GOTRADELANE objid={0} ring1={1} ring2={2} dunno={3}",
                objid, ring1, ring2, dunno);
            player.Runner.NotifyOnGoTradelane(player.Ship, ring1, ring2, dunno);

            if (player.Ship.CurrentAction is TradeLaneAction)
            {
                player.Ship.CurrentAction = null;
            }
        }

        private static void RxStopTradelane(Player.Player player, byte[] msg)
        {
            // FLPACKET_COMMON_STOPTRADELANE
            int pos = 2;
            uint objid = FLMsgType.GetUInt32(msg, ref pos);
            uint ring1 = FLMsgType.GetUInt32(msg, ref pos);
            uint ring2 = FLMsgType.GetUInt32(msg, ref pos);
            player.Log.AddLog(LogType.FL_MSG, "rx FLPACKET_COMMON_STOPTRADELANE objid={0} ring1={1} ring2={2}", objid,
                ring1, ring2);
            player.Runner.NotifyOnStopTradelane(player.Ship, ring1, ring2);
        }

        private static void RxSetWeaponsGroup(Player.Player player, byte[] msg)
        {
            // FLPACKET_COMMON_SET_WEAPON_GROUP
            int pos = 2;
            string wg = FLMsgType.GetAsciiStringLen32(msg, ref pos);
            string[] groups = wg.Split('\n');
            player.Log.AddLog(LogType.FL_MSG, "rx FLPACKET_COMMON_SET_WEAPON_GROUP wg={0}", wg.Replace('\n', ' '));
            player.Wgrp.SetWeaponGroup(groups);
        }

        private static void RxMunitionCollision(Player.Player player, byte[] msg)
        {
            // FLPACKET_CLIENT_MUNCOLLISION
            int pos = 2;
            var munitionid = FLMsgType.GetUInt32(msg, ref pos);
            uint spaceobjid = FLMsgType.GetUInt32(msg, ref pos);
            uint targetobjid = FLMsgType.GetUInt32(msg, ref pos);
            uint subtargetid = FLMsgType.GetUInt16(msg, ref pos);
            float x = FLMsgType.GetFloat(msg, ref pos);
            float y = FLMsgType.GetFloat(msg, ref pos);
            float z = FLMsgType.GetFloat(msg, ref pos);

            player.Log.AddLog(LogType.FL_MSG,
                "rx FLPACKET_CLIENT_MUNCOLLISION munitionid={0} spaceobjid={1} targetobjid={2} subtargetid={3} pos={4},{5},{6}",
                munitionid, spaceobjid, targetobjid, subtargetid, x, y, z);

            var targetObj = player.Runner.FindObject(targetobjid);
            if (targetObj != null)
            {
                var munition = ArchetypeDB.Find(munitionid) as MunitionArchetype;
                if (munition != null)
                {
                    // fixme: confirm that this munition was fired by the shooter.
                    // fixme: record shots fired versus hits (cheaters have high counts)
                    // TODO: fixme: ignore this message and do the whole lot server side Q_Q
                    if (targetObj is Old.Object.Ship.Ship)
                    {
                        var ship = targetObj as Old.Object.Ship.Ship;
                        //TODO: make better distinction
                        if (munition.MotorArch == null)
                        {
                            //gun
                            ship.Damage(subtargetid, munition.EnergyDamage, munition.HullDamage, DeathCause.Gun);
                        }
                        else
                        {
                            //missile
                            ship.Damage(subtargetid, munition.EnergyDamage, munition.HullDamage, DeathCause.Missile);
                        }
                    }
                    else if (targetObj is Old.Object.Loot)
                    {
                        var hitpoints = targetObj.Health*targetObj.Arch.HitPts - munition.HullDamage;

                        if (hitpoints > 0.0f)
                        {
                            player.Runner.NotifyOnSetHitPoints(targetobjid, 1, hitpoints, false);
                            targetObj.Health = hitpoints/targetObj.Arch.HitPts;
                        }
                        else
                        {
                            player.Runner.DelSimObject(targetObj);
                        }
                    }
                    else if (targetObj is Object.Solar.Solar)
                    {
                        ((Object.Solar.Solar) targetObj).Damage(munition.EnergyDamage, munition.HullDamage);
                    }
                }
            }
        }

        private static void RxRequestLaunch(Player.Player player, byte[] msg)
        {
            // FLPACKET_CLIENT_REQUESTLAUNCH
            var pos = 2;
            var objid = FLMsgType.GetUInt32(msg, ref pos);
            if (objid != player.Ship.Objid)
            {
                // fixme: kick for unexpected objid.
                return;
            }

            player.Log.AddLog(LogType.FL_MSG, "rx[{0}] FLPACKET_CLIENT_REQUESTLAUNCH objid={1}", player.FLPlayerID,
                objid);

            player.Ship.InitialiseEquipmentSimulation();

            Packets.SendMiscObjUpdate(player, Player.Player.MiscObjUpdateType.SYSTEM, player.FLPlayerID, player.Ship.System.SystemID);

            // for each solar in the system find the reputation that this
            // player's ship has with respect to the owning faction of the
            // solar
            foreach (var solar in player.Ship.System.Solars.Values)
                Packets.SendSetReputation(player, solar);

            Packets.SendServerLaunch(player);

            Packets.SendMiscObjUpdate(player, Player.Player.MiscObjUpdateType.UNK3, player.Ship.Objid);
            Packets.SendMiscObjUpdate(player, Player.Player.MiscObjUpdateType.UNK2, player.Ship.Objid);
        }

        private void RxRequestCharInfo(Player.Player player, byte[] msg)
        {
            // FLPACKET_CLIENT_REQUESTCHARINFO
            player.Log.AddLog(LogType.FL_MSG, "rx FLPACKET_CLIENT_REQUESTCHARINFO");
            player.SaveCharFile();
            player.SetState(DPCSelectingCharacterState.Instance());
        }

        private static void RxEnterBase(Player.Player player, byte[] msg)
        {
            // FLPACKET_CLIENT_ENTERBASE
            int pos = 2;
            uint baseid = FLMsgType.GetUInt32(msg, ref pos);
            player.Log.AddLog(LogType.FL_MSG, "rx[{0}] FLPACKET_CLIENT_ENTERBASE baseid={1}", player.FLPlayerID, baseid);

            if (player.Ship.Basedata == null
                || baseid != player.Ship.Basedata.BaseID)
            {
                // fixme: kick for unexpected base
            }

            if (player.Ship.Objid != 0)
            {
                player.Runner.DelSimObject(player.Ship);
            }

            Packets.SendSetHullStatus(player, player.Ship);
        }

        private static void RxExitBase(Player.Player player, byte[] msg)
        {
            // FLPACKET_CLIENT_EXITBASE
            int pos = 2;
            uint baseid = FLMsgType.GetUInt32(msg, ref pos);
            player.Log.AddLog(LogType.FL_MSG, "rx FLPACKET_CLIENT_EXITBASE baseid={0}", baseid);

            if (player.Ship.Basedata == null
                || baseid != player.Ship.Basedata.BaseID)
            {
                // fixme: kick for unexpected base
            }
        }

        private static void RxRequestBaseInfo(Player.Player player, byte[] msg)
        {
            // FLPACKET_CLIENT_REQUESTBASEINFO
            int pos = 2;
            uint baseid = FLMsgType.GetUInt32(msg, ref pos);
            uint type = FLMsgType.GetUInt8(msg, ref pos);
            player.Log.AddLog(LogType.FL_MSG, "rx FLPACKET_CLIENT_REQUESTBASEINFO baseid={0} type={1}", baseid, type);

            // TODO: Eventually ignore the client message and send the base info
            // that the server believes the player to be at.

            if (type == 1)
            {
                Packets.SendSetStartRoom(player, baseid, UniverseDB.FindBase(baseid).StartRoomID); // fixme
                Packets.SendGFCompleteMissionComputerList(player, baseid);
                Packets.SendGFCompleteNewsBroadcastList(player, baseid);
            }
        }

        private static void RxRequestLocationInfo(Player.Player player, byte[] msg)
        {
            // FLPACKET_CLIENT_REQUESTLOCATIONINFO
            int pos = 2;
            uint roomid = FLMsgType.GetUInt32(msg, ref pos);
            uint type = FLMsgType.GetUInt8(msg, ref pos);
            player.Log.AddLog(LogType.FL_MSG, "rx FLPACKET_CLIENT_REQUESTLOCATIONINFO roomid={0} type={1}", roomid, type);

            if (type == 1)
            {
                Packets.SendGFCompleteCharList(player, roomid);
                Packets.SendGFCompleteScriptBehaviourList(player, roomid);
                Packets.SendGFCompleteAmbientScriptList(player, roomid);
            }
        }

        private static void RxEnterLocation(Player.Player player, byte[] msg)
        {
            // FLPACKET_CLIENT_ENTERLOCATION
            int pos = 2;
            uint roomid = FLMsgType.GetUInt32(msg, ref pos);
            player.Log.AddLog(LogType.FL_MSG, "rx FLPACKET_CLIENT_ENTERLOCATION roomid={0}", roomid);
        }

        private static void RxExitLocation(Player.Player player, byte[] msg)
        {
            // FLPACKET_CLIENT_ENTERLOCATION
            int pos = 2;
            uint roomid = FLMsgType.GetUInt32(msg, ref pos);
            player.Log.AddLog(LogType.FL_MSG, "rx FLPACKET_CLIENT_ENTERLOCATION roomid={0}", roomid);
        }

        private static void RxRequestCreateShip(Player.Player player, byte[] msg)
        {
            // FLPACKET_CLIENT_REQUESTCREATESHIP
            player.Log.AddLog(LogType.FL_MSG, "rx FLPACKET_CLIENT_REQUESTCREATESHIP");

            // Set starting parameters
            player.Ship.UpdateTime = 0; // fixme?

            if (player.Ship.Objid == 0)
                player.Runner.CreateSimObject(player.Ship);

            if (player.Ship.Basedata != null)
            {
                var action = new LaunchFromBaseAction {DockingObj = player.Ship.Basedata.GetLaunchPoint(player.Ship)};

                // Find the solar and launch point the ship should undock from
                // and record the player's position on launch.
                action.Position = action.DockingObj.Position;

                // If this is a ring or berth then face the ship outwards otherwise
                // face the ship inwards - towards the jump or mooring point.
                Matrix launch_rotation = action.DockingObj.Rotation;
                if (action.DockingObj.Type == DockingPoint.DockingSphere.BERTH
                    || action.DockingObj.Type == DockingPoint.DockingSphere.RING)
                {
                    launch_rotation = Matrix.TurnAround(launch_rotation);
                }

                action.Orientation = Quaternion.MatrixToQuaternion(launch_rotation);
                player.Ship.CurrentAction = action;
            }
                // Otherwise the player was in space so set the spawn position to that of
            else
            {
                var action = new LaunchInSpaceAction
                {
                    Position = player.Ship.Position,
                    Orientation = Quaternion.MatrixToQuaternion(player.Ship.Orientation)
                };
                player.Ship.CurrentAction = action;
            }

            Packets.SendCreateShipResponse(player, player.Ship);
        }


        private static void RxSetVisitedState(Player.Player player, byte[] msg)
        {
            // FLPACKET_COMMON_SET_VISITED_STATE
            int pos = 2;
            uint size = FLMsgType.GetUInt32(msg, ref pos);
            uint cnt = FLMsgType.GetUInt32(msg, ref pos);

            player.Log.AddLog(LogType.FL_MSG, "rx FLPACKET_COMMON_SET_VISITED_STATE cnt={0}", cnt);
            while (cnt-- > 0)
            {
                uint id = FLMsgType.GetUInt32(msg, ref pos);
                uint mask = FLMsgType.GetUInt8(msg, ref pos);

                // FIXME: check visit is valid (kick for cheating if visit doesn't exist)
                // FIXME: add to character file.
            }
        }

        private static void RxJettisionCargo(Player.Player player, byte[] msg)
        {
            // FLPACKET_COMMON_JETTISONCARGO
            int pos = 2;
            uint objid = FLMsgType.GetUInt32(msg, ref pos);
            uint hpid = FLMsgType.GetUInt16(msg, ref pos);
            uint count = FLMsgType.GetUInt32(msg, ref pos);
            //Seems to be 4 bytes - needs to be confirmed, since sent packets have only 2

            player.Log.AddLog(LogType.FL_MSG, "rx FLPACKET_COMMON_JETTISONCARGO objid={0} hpid={1} count={2}", objid,
                hpid, count);


            ShipItem jettisonedItem = player.Ship.FindByHpid(hpid);
            if (jettisonedItem != null)
            {
                if (count > jettisonedItem.count)
                    return; //TODO: Kick for cheating
                if (jettisonedItem.mounted)
                    return; //TODO: Kick for cheating

                Archetype lootCrate;
                if (jettisonedItem.arch.LootAppearance != "")
                {
                    lootCrate = ArchetypeDB.Find(FLUtility.CreateID(jettisonedItem.arch.LootAppearance));
                }
                else
                {
                    lootCrate = ArchetypeDB.Find(2699420296);
                }


                Vector position = player.Ship.Position;
                position.y -= player.Ship.Arch.Radius + 10; //Spawn 10m + radius below ship
                var newLoot = new Old.Object.Loot(lootCrate, 1.0f, jettisonedItem.arch, jettisonedItem.health, (ushort)count,
                    false, false, position);
                player.Runner.CreateSimObject(newLoot);

                jettisonedItem.count -= count;

                if (jettisonedItem.count == 0) player.Ship.Items.Remove(hpid);

                Packets.SendSetEquipment(player, player.Ship.Items);
            }
            //TODO: Kick for cheating in else case
        }

        private static void RxActivateThrusters(Player.Player player, byte[] msg)
        {
            // FLPACKET_COMMON_ACTIVATETHRUSTERS
            int pos = 2;
            uint objid = FLMsgType.GetUInt32(msg, ref pos);
            bool activated = FLMsgType.GetUInt8(msg, ref pos) != 0;

            player.Log.AddLog(LogType.FL_MSG, "rx FLPACKET_COMMON_ACTIVATETHRUSTERS objid={0} activated={1}", objid,
                activated);
            player.Runner.NotifyOnActivateThrusters(player.Ship, activated); // fixme, move into ship (for npcs)
        }

        private static void RxRequestPlayerStats(Player.Player player, byte[] msg)
        {
            player.Log.AddLog(LogType.FL_MSG, "rx FLPACKET_COMMON_REQUEST_PLAYER_STATS");

            // FLPACKET_COMMON_REQUEST_PLAYER_STATS
            Packets.SendPlayerStats(player);
        }

        private static void RxSetManeuver(Player.Player player, byte[] msg)
        {
            // FLPACKET_CLIENT_SETMANEUVER
            int pos = 2;
            uint spaceobjid = FLMsgType.GetUInt32(msg, ref pos);
            uint targetobjid = FLMsgType.GetUInt32(msg, ref pos);
            uint maneuver = FLMsgType.GetUInt32(msg, ref pos);

            player.Log.AddLog(LogType.FL_MSG,
                "rx FLPACKET_CLIENT_SETMANEUVER spaceobjid={0} targetobjid={1} maneuver={2}",
                spaceobjid, targetobjid, maneuver);
        }

        private static void RxRequestBestPath(Player.Player player, byte[] msg)
        {
            // FLPACKET_COMMON_REQUEST_BEST_PATH
            int pos = 2;

            uint size = FLMsgType.GetUInt32(msg, ref pos);
            uint spaceobjid = FLMsgType.GetUInt32(msg, ref pos);
            uint waypointcount = FLMsgType.GetUInt32(msg, ref pos);
            uint failed = FLMsgType.GetUInt32(msg, ref pos);

            player.Log.AddLog(LogType.FL_MSG,
                "rx FLPACKET_COMMON_REQUEST_BEST_PATH size={0} spaceobjid={1} waypoints={2}", size, spaceobjid,
                waypointcount);

            var waypoints = new Waypoint[waypointcount];
            for (int a = 0; a < waypointcount; a++)
            {
                var wp_pos = new Vector();
                wp_pos.x = FLMsgType.GetFloat(msg, ref pos);
                wp_pos.y = FLMsgType.GetFloat(msg, ref pos);
                wp_pos.z = FLMsgType.GetFloat(msg, ref pos);

                uint target = FLMsgType.GetUInt32(msg, ref pos);
                uint systemid = FLMsgType.GetUInt32(msg, ref pos);

                waypoints[a] = new Waypoint(wp_pos, target, systemid);
            }

            ComputeBestPath(player, spaceobjid, waypoints);
        }

        private static void ComputeBestPath(Player.Player player, uint spaceobjid, Waypoint[] waypoints)
        {
            // We're assuming that the start waypoint is n-2 and the end waypoint is n-1
            // where n is the number of waypoints

            var path = new List<Waypoint>();
            Waypoint origin = waypoints[waypoints.Length - 2];
            Waypoint destination = waypoints[waypoints.Length - 1];

            uint[] system_path = UniverseDB.FindBestLegalPath(origin.SystemID, destination.SystemID);
            if (system_path == null)
            {
                byte[] omsg = {0x16, 0x01};
                FLMsgType.AddUInt32(ref omsg, 3*sizeof (uint));
                FLMsgType.AddUInt32(ref omsg, spaceobjid);
                FLMsgType.AddUInt32(ref omsg, 0);
                FLMsgType.AddUInt32(ref omsg, 1); // mark failed best path

                player.SendMsgToClient(omsg);
                return;
            }

            uint destgate = 0;
            for (int a = 0; a < system_path.Length; a++)
            {
                StarSystem system = UniverseDB.FindSystem(system_path[a]);

                // Add the origin gate if we're in a new system
                Vector origpos = origin.Position;
                if (a > 0)
                {
                    Object.Solar.Solar origgate = UniverseDB.FindSolar(destgate);
                    origpos = origgate.Position;
                    path.Add(new Waypoint(origpos, destgate, system_path[a]));
                }

                // Are we in the destination system? If so, try to get closer to destination waypoint
                // Otherwise, find the gate
                Object.Solar.Solar destsolar = null;
                Vector destpos = destination.Position;
                if (system_path[a] != destination.SystemID)
                {
                    destsolar = system.FindGateTo(system_path[a + 1]);
                    destgate = destsolar.DestinationObjid;
                    destpos = destsolar.Position;
                }

                // Calculate the best path
                List<uint> sys_path = system.FindBestPath(origpos, destpos);
                if (sys_path != null)
                {
                    foreach (uint e in sys_path)
                    {
                        Object.Solar.Solar s = system.Solars[e];
                        path.Add(new Waypoint(s.Position, e, system.SystemID));
                    }
                }

                path.Add(new Waypoint(destpos, destsolar != null ? destsolar.Objid : 0, system_path[a]));
            }

            {
                byte[] omsg = {0x16, 0x01};
                FLMsgType.AddUInt32(ref omsg, (uint) (3*sizeof (uint) + path.Count*(3*sizeof (float) + 2*sizeof (uint))));
                FLMsgType.AddUInt32(ref omsg, spaceobjid);
                FLMsgType.AddUInt32(ref omsg, (uint) path.Count);
                FLMsgType.AddUInt32(ref omsg, 0);

                foreach (Waypoint wp in path)
                {
                    FLMsgType.AddFloat(ref omsg, (float) wp.Position.x);
                    FLMsgType.AddFloat(ref omsg, (float) wp.Position.y);
                    FLMsgType.AddFloat(ref omsg, (float) wp.Position.z);

                    FLMsgType.AddUInt32(ref omsg, wp.ObjID);
                    FLMsgType.AddUInt32(ref omsg, wp.SystemID);
                }

                player.SendMsgToClient(omsg);
            }
        }

        private static void RxRequestNavMap(Player.Player player, byte[] msg)
        {
            player.Log.AddLog(LogType.FL_MSG, "rx Send_FLPACKET_COMMON_REQUEST_NAVMAP");
        }

        private static void RxSetInterfaceState(Player.Player player, byte[] msg)
        {
            player.Log.AddLog(LogType.FL_MSG, "rx Send_FLPACKET_COMMON_SET_INTERFACE_STATE");
        }

        private static void RxRequestEvent(Player.Player player, byte[] msg)
        {
            // FLPACKET_CLIENT_REQUEST_EVENT
            int pos = 2;
            uint flag = FLMsgType.GetUInt8(msg, ref pos); // 0 -> jh/jg, 2 -> tradelane, 1 -> join group
            uint spaceobjid = FLMsgType.GetUInt32(msg, ref pos);
            uint targetobjid = FLMsgType.GetUInt32(msg, ref pos);
            uint ringobjid = FLMsgType.GetUInt32(msg, ref pos);
            uint dunno3 = FLMsgType.GetUInt8(msg, ref pos);

            player.Log.AddLog(LogType.FL_MSG,
                "rx FLPACKET_CLIENT_REQUEST_EVENT flag={0} spaceobjid={1} targetobjid={2} ringobjid={3} dunno3={4}",
                flag, spaceobjid, targetobjid, ringobjid, dunno3);

            // FIXME: If this is a docking request, verify that the ship is near to the docking point.

            if (player.Ship.Objid != spaceobjid)
            {
                // fixme: log and kick player
                return;
            }

            Object.Solar.Solar solar = UniverseDB.FindSolar(targetobjid);
            if (solar == null)
            {
                // fixme: log and kick player
                return;
            }

            // fixme: check distance to solar (relies on updating the client to send position more frequently).

            // This is a base docking request
            if (solar.BaseData != null)
            {
                DockingObject docking_obj = solar.GetDockingPoint(player.Ship);
                if (docking_obj == null)
                {
                    player.Log.AddLog(LogType.ERROR, "error: no docking object");
                    Packets.SendServerRequestReturned(player, player.Ship, null);
                    return;
                }

                var action = new DockAction();
                action.DockingObj = docking_obj;
                player.Ship.CurrentAction = action;

                docking_obj.Activate(player.Runner, player.Ship);
                Packets.SendServerRequestReturned(player, player.Ship, docking_obj);
            }
                // This is a jump hole/gate docking request
            else if ((solar.Arch.Type == Archetype.ObjectType.JUMP_GATE ||
                      solar.Arch.Type == Archetype.ObjectType.JUMP_HOLE) &&
                     solar.DestinationObjid != 0)
            {
                DockingObject docking_obj = solar.GetDockingPoint(player.Ship);
                if (docking_obj == null)
                {
                    player.Log.AddLog(LogType.ERROR, "error: no docking object");
                    Packets.SendServerRequestReturned(player, player.Ship, null);
                    return;
                }

                var action = new JumpAction();
                action.DockingObj = docking_obj;
                action.DestinationSolar = UniverseDB.FindSolar(docking_obj.Solar.DestinationObjid);
                player.Ship.CurrentAction = action;

                docking_obj.Activate(player.Runner, player.Ship);
                Packets.SendServerRequestReturned(player, player.Ship, docking_obj);
            }
                // This is a tradelane
            else if (solar.Arch.Type == Archetype.ObjectType.TRADELANE_RING)
            {
                DockingObject docking_obj = solar.GetDockingPoint(player.Ship);
                if (docking_obj == null)
                    player.Log.AddLog(LogType.ERROR, "error: no docking object");

                var action = new TradeLaneAction();
                action.DockingObj = docking_obj;
                player.Ship.CurrentAction = action;

                docking_obj.Activate(player.Runner, player.Ship);
                Packets.SendServerRequestReturned(player, player.Ship, docking_obj);
            }
                // Otherwise this solar doesn't support the event request
            else
            {
                Packets.SendServerRequestReturned(player, player.Ship, null);
            }
        }

        private static void RxRequestCancel(Player.Player player, byte[] msg)
        {
            // FLPACKET_CLIENT_REQUEST_CANCEL
            int pos = 2;
            uint dunno1 = FLMsgType.GetUInt8(msg, ref pos);
            uint spaceobjid = FLMsgType.GetUInt32(msg, ref pos);
            uint targetobjid = FLMsgType.GetUInt32(msg, ref pos);
            uint dunno2 = FLMsgType.GetUInt8(msg, ref pos);

            player.Log.AddLog(LogType.FL_MSG,
                "rx FLPACKET_CLIENT_REQUEST_CANCEL dunno1={0} spaceobjid={1} targetobjid={2} dunno2={3}",
                dunno1, spaceobjid, targetobjid, dunno2);

            // FIXME: Check that this is for the player ship.
            if (player.Ship.CurrentAction is DockAction)
            {
                var action = player.Ship.CurrentAction as DockAction;
                action.DockingObj.Deactivate(player.Runner);
                player.Ship.CurrentAction = null;
            }
            else if (player.Ship.CurrentAction is JumpAction)
            {
                var action = player.Ship.CurrentAction as JumpAction;
                action.DockingObj.Deactivate(player.Runner);
                player.Ship.CurrentAction = null;
            }
            else if (player.Ship.CurrentAction is TradeLaneAction)
            {
                var action = player.Ship.CurrentAction as TradeLaneAction;
                action.DockingObj.Deactivate(player.Runner);
                player.Ship.CurrentAction = null;
            }
        }

        /// <summary>
        ///     The client will request hull status to be set when:
        ///     - dying (hull=0.0f)
        ///     - repairing the hull(hull=1.0f)
        /// </summary>
        /// <param name="player"></param>
        /// <param name="msg"></param>
        private static void RxRequestSetHullStatus(Player.Player player, byte[] msg)
        {
            int pos = 2;
            float health = FLMsgType.GetFloat(msg, ref pos);
            // If we're in a base, not buying a ship package and the health is 1.0 then assume that this is a
            // repair action and charge the player appropriately.
            if (player.Ship.Basedata != null && player.Ship.CurrentAction == null && health == 1.0f)
            {
                Good ship_hull = UniverseDB.FindGoodByArchetype(player.Ship.Arch);
                if (ship_hull == null)
                {
                    //fixme: log
                    return;
                }

                float repair_cost = (1.0f - player.Ship.Health)*(1/30f)*ship_hull.BasePrice;
                if (repair_cost <= 0 && player.Money < repair_cost)
                {
                    //fixme: log
                    return;
                }
                player.Money -= (Int32) repair_cost;
                Packets.SendSetMoney(player);
                player.Ship.Health = 1.0f;
            }

            Packets.SendSetHullStatus(player, player.Ship);
        }

        private static void RxLaunchComplete(Player.Player player, byte[] msg)
        {
            // FLPACKET_CLIENT_LAUNCHCOMPLETE
            int pos = 2;
            uint baseid = FLMsgType.GetUInt32(msg, ref pos);
            uint objid = FLMsgType.GetUInt32(msg, ref pos);

            player.Log.AddLog(LogType.FL_MSG, "rx FLPACKET_CLIENT_LAUNCHCOMPLETE baseid={0} objid={1}", baseid, objid);

            if (player.Ship.CurrentAction is LaunchFromBaseAction)
            {
                var action = player.Ship.CurrentAction as LaunchFromBaseAction;
                action.DockingObj.Deactivate(player.Runner);
                player.Ship.CurrentAction = null;
                player.SaveCharFile();
            }
            else if (player.Ship.CurrentAction is LaunchInSpaceAction)
            {
                player.Ship.CurrentAction = null;
                player.SaveCharFile();
            }
        }

        private static void RxClientHail(Player.Player player, byte[] msg)
        {
            // FLPACKET_CLIENT_HAIL
            int pos = 2;
            uint from_objid = FLMsgType.GetUInt32(msg, ref pos);
            uint to_objid = FLMsgType.GetUInt32(msg, ref pos);
            uint systemid = FLMsgType.GetUInt32(msg, ref pos);

            player.Log.AddLog(LogType.FL_MSG, "rx[{0}] FLPACKET_CLIENT_HAIL from_objid={1} to_objid={2} systemid={3}",
                player.FLPlayerID, from_objid, to_objid, systemid);
        }

        private static void RxRequestUseItem(Player.Player player, byte[] msg)
        {
            // FLPACKET_CLIENT_REQUEST_USE_ITEM
            int pos = 2;
            uint objid = FLMsgType.GetUInt32(msg, ref pos);
            uint hpid = FLMsgType.GetUInt16(msg, ref pos);
            uint count = FLMsgType.GetUInt16(msg, ref pos);

            player.Log.AddLog(LogType.FL_MSG, "rx[{0}] FLPACKET_CLIENT_REQUEST_USE_ITEM objid={1} hpid={2} count={3}",
                player.FLPlayerID, objid, hpid, count);
            player.Ship.UseItem(hpid);
        }

        private static void RxRequestInvincibility(Player.Player player, byte[] msg)
        {
            // FLPACKET_CLIENT_REQINVINCIBILITY
            int pos = 2;
            uint objid = FLMsgType.GetUInt32(msg, ref pos);
            uint dunno1 = FLMsgType.GetUInt8(msg, ref pos);
            uint dunno2 = FLMsgType.GetUInt8(msg, ref pos);

            player.Log.AddLog(LogType.FL_MSG, "rx FLPACKET_CLIENT_REQINVINCIBILITY objid={0} dunno1={1} dunno2={2}",
                objid, dunno1, dunno2);

            // FIXME: check for correct player objid

            // FIXME: Make player invincible, avoid cheating from said request
        }

        private static void RxJumpInComplete(Player.Player player, byte[] msg)
        {
            // FLPACKET_CLIENT_JUMPINCOMPLETE
            int pos = 2;
            uint systemid = FLMsgType.GetUInt32(msg, ref pos);
            uint spaceobjid = FLMsgType.GetUInt32(msg, ref pos);

            player.Log.AddLog(LogType.FL_MSG, "rx FLPACKET_CLIENT_JUMPINCOMPLETE systemid={0} spaceobjid={1}",
                systemid, spaceobjid);

            if (!(player.Ship.CurrentAction is JumpAction)) return;

            var action = player.Ship.CurrentAction as JumpAction;
            player.Ship.System = action.DestinationSolar.System;
            player.Ship.CurrentAction = null;
            player.SaveCharFile();

            player.Update();
        }

        private static void RxGoodSell(Player.Player player, byte[] msg)
        {
            // FLPACKET_CLIENT_GFGOODSELL
            int pos = 2;
            uint baseid = FLMsgType.GetUInt32(msg, ref pos);
            uint good = FLMsgType.GetUInt32(msg, ref pos);
            uint count = FLMsgType.GetUInt32(msg, ref pos);

            player.Log.AddLog(LogType.FL_MSG, "rx FLPACKET_CLIENT_GFGOODSELL baseid={0} good={1} count={2}",
                baseid, good, count);
            // ignore this as we do the processing in add/remove equipment
        }

        private void RxGoodBuy(Player.Player player, byte[] msg)
        {
            // FLPACKET_CLIENT_GFGOODBUY
            int pos = 2;
            uint baseid = FLMsgType.GetUInt32(msg, ref pos);
            uint dunno = FLMsgType.GetUInt32(msg, ref pos);
            uint good = FLMsgType.GetUInt32(msg, ref pos);
            uint count = FLMsgType.GetUInt8(msg, ref pos);

            player.Log.AddLog(LogType.FL_MSG, "rx FLPACKET_CLIENT_GFGOODBUY baseid={0} dunno={1} good={2} count={3}",
                baseid, dunno, good, count);
            // ignore this as we do the processing in add/remove equipment
        }

        private static void RxGFSelectObject(Player.Player player, byte[] msg)
        {
            // FLPACKET_CLIENT_GFSELECTOBJECT
            int pos = 2;
            uint charid = FLMsgType.GetUInt32(msg, ref pos);

            player.Log.AddLog(LogType.FL_MSG, "rx FLPACKET_CLIENT_GFSELECTOBJECT charid={0}",
                charid);

            // Send the barman, dealers, etc. These have fixed locations and
            // fidget scripts
            foreach (var ch in player.Ship.Basedata.Chars.Values.Where(ch => ch.Type == "bar"))
            {
                Packets.SendNPCGFUpdateScripts(player, ch, 2);
            }
        }

        private static void RxMissionResponse(Player.Player player, byte[] msg)
        {
            // FLPACKET_CLIENT_MISSIONRESPONSE
            int pos = 2;
            uint charid = FLMsgType.GetUInt32(msg, ref pos);
            uint response = FLMsgType.GetUInt32(msg, ref pos);
            uint dunno = FLMsgType.GetUInt8(msg, ref pos);

            player.Log.AddLog(LogType.FL_MSG, "rx FLPACKET_CLIENT_MISSIONRESPONSE charid={0} response={1} dunno={2}",
                charid, response, dunno);
        }

        private static void RxRequestShipArch(Player.Player player, byte[] msg)
        {
            // FLPACKET_CLIENT_REQSHIPARCH
            int pos = 2;
            uint shipid = FLMsgType.GetUInt32(msg, ref pos);

            player.Log.AddLog(LogType.FL_MSG, "rx FLPACKET_CLIENT_REQSHIPARCH shipid={0}", shipid);

            if (player.Ship.Basedata == null)
            {
                // fixme: invalid state, kick and log
                return;
            }

            Archetype arch = ArchetypeDB.Find(shipid);
            if (arch is ShipArchetype)
            {
                player.Ship.Arch = arch;

                byte[] omsg = {0x23, 0x02};
                FLMsgType.AddUInt32(ref omsg, shipid);
                player.SendMsgToClient(omsg);
            }
        }

        private static void RxRequestEquipment(Player.Player player, byte[] msg)
        {
            // FLPACKET_CLIENT_REQEQUIPMENT
            int pos = 2;
            uint num_items = FLMsgType.GetUInt16(msg, ref pos);
            player.Log.AddLog(LogType.FL_MSG, "rx FLPACKET_CLIENT_REQEQUIPMENT num_items={0}", num_items);

            // If we're in a base and not buying a ship (fixme: current action will be BuyShipAction at some point)
            // then we're either mounting/unmounting equipment or repairing it. Only accept changes for equipment
            // this ship has.
            if (player.Ship.Basedata != null && player.Ship.CurrentAction == null)
            {
                while (num_items-- > 0)
                {
                    uint count = FLMsgType.GetUInt32(msg, ref pos);
                    float health = FLMsgType.GetFloat(msg, ref pos);
                    uint goodid = FLMsgType.GetUInt32(msg, ref pos);
                    uint hpid = FLMsgType.GetUInt16(msg, ref pos);
                    bool mounted = FLMsgType.GetUInt16(msg, ref pos) == 1 ? true : false;
                    string hpname = FLMsgType.GetAsciiStringLen16(msg, ref pos);
                    //player.log.AddLog(LogType.FL_MSG, "    goodid={0} count={1} health={2} hpid={3} mounted={4} hpname={5}",
                    //    item.goodid, item.count, item.health, item.hpid, item.mounted, item.hpname));

                    ShipItem item = player.Ship.FindByHpid(hpid);
                    if (item != null)
                    {
                        // If the mounting state has changed, accept this.
                        item.mounted = mounted;
                        if (!mounted)
                            item.hpname = "";
                        else if (player.Ship.FindByHardpoint(hpname) == null)
                            item.hpname = hpname;

                        // If the item was damaged and the request indicates full health then
                        // start calculating the cost
                        if (item.health < 1.0f && health == 1.0f)
                        {
                            Good good = UniverseDB.FindGoodByArchetype(item.arch);
                            if (good != null)
                            {
                                float repair_cost = (1.0f - item.health)*0.30f*good.BasePrice;
                                if (repair_cost > 0 && player.Money >= repair_cost)
                                {
                                    player.Money -= (Int32) repair_cost;
                                    Packets.SendSetMoney(player);
                                    item.health = 1.0f;
                                }
                            }
                        }
                    }
                }
            }

            Packets.SendSetEquipment(player, player.Ship.Items);
            // fixme: unless the player is buying a ship, we only allow the mounting status of equipment
            // to change.
        }

        private static void RxRequestAddItem(Player.Player player, byte[] msg)
        {
            // FLPACKET_CLIENT_REQADDITEM
            int pos = 2;
            uint goodid = FLMsgType.GetUInt32(msg, ref pos);
            uint count = FLMsgType.GetUInt32(msg, ref pos);
            float health = FLMsgType.GetFloat(msg, ref pos);
            bool mounted = FLMsgType.GetUInt8(msg, ref pos) == 1;
            string hpname = FLMsgType.GetAsciiStringLen32(msg, ref pos);

            player.Log.AddLog(LogType.FL_MSG,
                "rx FLPACKET_CLIENT_REQADDITEM goodid={0} count={1} health={2} mounted={3} hpname={4}",
                goodid, count, health, mounted, hpname);

            if (player.Ship.Basedata == null)
            {
                // fixme: invalid state, kick and log
                return;
            }

            float price = UniverseDB.GetPriceOfGood(player.Ship.Basedata, goodid)*count;
            if (price > player.Money)
            {
                // fixme: kick and log
                return;
            }

            Archetype arch = ArchetypeDB.Find(goodid);

            // If this is cargo then add to an existing item for this good if possible
            if (!mounted)
            {
                ShipItem item = player.Ship.FindItemByGood(goodid);
                if (item == null)
                {
                    item = new ShipItem();
                    item.arch = arch;
                    item.count = 0;
                    item.mounted = false;
                    item.hpid = player.Ship.FindFreeHpid();
                    item.hpname = "";
                    item.health = 1.0f;
                    item.mission = false;
                    player.Ship.Items[item.hpid] = item;
                }

                item.count += count;

                // fixme: check for exceeded volume, capacity or lack of funds and 
                // reverse the transaction.
                Packets.SendAddItem(player, goodid, item.hpid, count, health, mounted, hpname);
            }
                // Otherwise this is equipment and so there should be nothing on the hard point already
            else if (player.Ship.FindByHardpoint(hpname) != null)
            {
                // fixme: log and kick for duplicate mounting
            }
                // Valid equipment so add it to the ship
            else
            {
                var item = new ShipItem();
                item.arch = arch;
                item.count = 1;
                item.mounted = true;
                item.hpid = player.Ship.FindFreeHpid();
                item.hpname = hpname;
                item.health = health;
                item.mission = false;
                player.Ship.Items[item.hpid] = item;
                Packets.SendAddItem(player, goodid, item.hpid, item.count, item.health, item.mounted, item.hpname);
            }


            // Remove the value of the item from the player's account
            player.Money -= (Int32) price;
            Packets.SendSetMoney(player);
        }

        private static void RxRequestRemoveItem(Player.Player player, byte[] msg)
        {
            // FLPACKET_CLIENT_REQREMOVEITEM
            int pos = 2;
            uint hpid = FLMsgType.GetUInt32(msg, ref pos);
            uint count = FLMsgType.GetUInt32(msg, ref pos);

            player.Log.AddLog(LogType.FL_MSG, "rx FLPACKET_CLIENT_REQREMOVEITEM hpid={0} count={1}",
                hpid, count);

            if (player.Ship.Basedata == null)
            {
                // fixme: invalid state, kick and log
                return;
            }

            ShipItem item = player.Ship.FindByHpid(hpid);
            if (item == null || item.count < count)
            {
                // fixme: invalid state, kick and log.
                return;
            }

            item.count -= count;
            if (item.count == 0)
                player.Ship.Items.Remove(hpid);

            float price = UniverseDB.GetPriceOfGood(player.Ship.Basedata, item.arch.ArchetypeID)*count;

            // Add the value of the item from the player's account
            player.Money += (Int32) price;
            Packets.SendSetMoney(player);
        }

        private static void RxRequestSetCash(Player.Player player, byte[] msg)
        {
            // FLPACKET_CLIENT_REQSETCASH
            int pos = 2;
            int money = FLMsgType.GetInt32(msg, ref pos);

            player.Log.AddLog(LogType.FL_MSG, "rx FLPACKET_CLIENT_REQSETCASH money={0} ",
                money);

            if (player.Ship.Basedata == null)
            {
                //TODO: fixme: invalid state, kick and log
            }

            // ignore set cash; do this based on the changes requested
        }

        private static void RxRequestChangeCash(Player.Player player, byte[] msg)
        {
            // FLPACKET_CLIENT_REQCHANGECASH
            int pos = 2;
            int money = FLMsgType.GetInt32(msg, ref pos);

            player.Log.AddLog(LogType.FL_MSG, "rx FLPACKET_CLIENT_REQCHANGECASH money={0} ",
                money);

            if (player.Ship.Basedata == null)
            {
                //TODO: fixme: invalid state, kick and log
            }

            // ignore change cash; do this based on the changes requested
        }

        private static void RxSystemSwitchOutComplete(Player.Player player, byte[] msg)
        {
            // FLPACKET_CLIENT_SYSTEM_SWITCH_OUT_COMPLETE
            int pos = 2;
            uint objid = FLMsgType.GetUInt32(msg, ref pos);

            player.Log.AddLog(LogType.FL_MSG, "rx FLPACKET_CLIENT_SYSTEM_SWITCH_OUT_COMPLETE objid={0} ",
                objid);

            if (!(player.Ship.CurrentAction is JumpAction)) return;
            var action = player.Ship.CurrentAction as JumpAction;

            var positionOffset = new Vector(500, 0, -100); // fixme:
            player.Ship.Position = action.DestinationSolar.Position + positionOffset;
            player.Ship.Orientation = action.DestinationSolar.Orientation;

            Packets.SendSystemSwitchIn(player, player.Ship);

            // Send reputation updates for all solars in the system.
            foreach (var solar in player.Ship.System.Solars.Values)
                Packets.SendSetReputation(player, solar);
        }

        public static void RxObjectCollision(Player.Player player, byte[] msg)
        {
            // FLPACKET_CLIENT_OBJCOLLISION
            int pos = 2;
            uint objid = FLMsgType.GetUInt32(msg, ref pos);
            uint subobjid = FLMsgType.GetUInt16(msg, ref pos);
            uint target_objid = FLMsgType.GetUInt32(msg, ref pos);
            uint target_subobjid = FLMsgType.GetUInt16(msg, ref pos);
            float damage = FLMsgType.GetFloat(msg, ref pos);

            player.Log.AddLog(LogType.FL_MSG,
                "rx FLPACKET_CLIENT_OBJCOLLISION objid={0} subobjid={1} target_objid={2} target_subobjid={3} damage={4} ",
                objid, subobjid, target_objid, target_subobjid, damage);

            Object.Solar.Solar solar = UniverseDB.FindSolar(target_objid);
            if (solar != null)
            {
                if (solar.Arch.Type == Archetype.ObjectType.PLANET
                    || solar.Arch.Type == Archetype.ObjectType.SUN
                    || solar.Arch.Type == Archetype.ObjectType.MOON)
                {
                    player.Ship.Destroy(DeathCause.Environment);
                }
            }
            player.Ship.Damage(DamageListItem.HULL, 0,damage, DeathCause.Environment);

        }

        public static DPCInBaseState Instance()
        {
            if (_instance == null)
                _instance = new DPCInBaseState();
            return _instance;
        }

        #region RxChat

        private void RxChat(Player.Player player, byte[] msg)
        {
            // FLPACKET_COMMON_CHATMSG
            int pos = 2;
            uint len = FLMsgType.GetUInt32(msg, ref pos);

            int posEnd = (int) len + pos;
            uint to = FLMsgType.GetUInt32(msg, ref posEnd);
            uint from = FLMsgType.GetUInt32(msg, ref posEnd);

            string chat = "";
            while (len != 0)
            {
                uint rdl = FLMsgType.GetUInt32(msg, ref pos);
                uint siz = FLMsgType.GetUInt32(msg, ref pos);
                switch (rdl)
                {
                    case 2: // text
                        chat += new UnicodeEncoding().GetString(msg, pos, (int) siz - 2);
                        pos += (int) siz;
                        break;
                    default: // just ignore 'em
                        pos += (int) siz;
                        break;
                }
                len -= 8 + siz;
            }

            switch (to)
            {
                    //left for reference, realisation in chat.chat.process
                case 0x10000: // universe chat
                case 0x10001: // system chat
                case 0x10002: // local chat
                case 0x10003: // group chat
                default: // private chat
                    break;

                case 0x10004: // group commands
                    var command = (Player.Player.ChatCommand) FLMsgType.GetUInt32(msg, ref pos);
                    uint flplayerid = FLMsgType.GetUInt32(msg, ref pos);
                    player.Log.AddLog(LogType.FL_MSG,
                        "rx FLPACKET_COMMON_CHATMSG command={0} flplayerid={1} to={2} from={3}",
                        command, flplayerid, to, from);
                    switch (command)
                    {
                        case Player.Player.ChatCommand.GROUPINVITEREQUEST:
                            HandleGroupInviteRequest(player, player.Runner.GetPlayer(flplayerid));
                            break;
                        case Player.Player.ChatCommand.GROUPINVITATIONACCEPTEDREQUEST:
                            HandleGroupInvitationAcceptedRequest(player, player.Runner.GetPlayer(flplayerid));
                            break;
                        case Player.Player.ChatCommand.GROUPLEAVEREQUEST:
                            HandleGroupLeaveRequest(player);
                            break;
                        default:
                            break;
                    }
                    return;

                    // custom chat messages
                case 0x20000:
                case 0x20001:
                case 0x20010:
                case 0x20100:
                    return;
            }

            //if (to < 0x10000)
            //{
            player.Log.AddLog(LogType.FL_MSG, "rx FLPACKET_COMMON_CHATMSG to={0} from={1} chat={2}",
                to, from, chat);
            Chat.Chat.Process(player, to, chat);
            //}
        }

        private static void HandleGroupLeaveRequest(Player.Player player)
        {
            if (player == null || player.Group == null)
            {
                return;
            }

            player.Group.Leave(player);
            player.Group = null;
            player.Update();
        }

        private static void HandleGroupInvitationAcceptedRequest(Player.Player player, Player.Player playerInviter)
        {
            if (player == null || player.GroupInvited == null || playerInviter == null)
            {
                return;
            }

            player.Group = player.GroupInvited;
            player.Group.InviteAccepted(player, playerInviter);
            player.GroupInvited = null;
            player.Update();
        }

        private static void HandleGroupInviteRequest(Player.Player playerFrom, Player.Player playerTo)
        {
            if (playerFrom == null || playerTo == null)
            {
                return;
            }

            if (playerFrom.Group == null)
            {
                var group = new Group();
                group.AddPlayer(playerFrom);
                playerFrom.Group = group;
                playerFrom.Update();
            }

            Packets.SendChatCommand(playerFrom, Player.Player.ChatCommand.GROUPINVITATIONSENT, playerTo.FLPlayerID);
            Packets.SendChatCommand(playerTo, Player.Player.ChatCommand.GROUPINVITATIONRECEIVED, playerFrom.FLPlayerID);
            playerTo.GroupInvited = playerFrom.Group;
            playerTo.Update();
        }

        #endregion

        private enum ManeuverType
        {
            Null = 0,
            Buzz = 1,
            Goto = 2,
            Trail = 3,
            Flee = 4,
            Evade = 5,
            Idle = 6,
            Dock = 7,
            Launch = 8,
            InstantTradeLane = 9,
            Formation = 10,
            Large_Ship_Move = 11,
            Cruise = 12,
            Strafe = 13,
            Guide = 14,
            Face = 15,
            Loot = 16,
            Follow = 17,
            DrasticEvade = 18,
            FreeFlight = 19,
            Delay = 20,
        };
    }
}