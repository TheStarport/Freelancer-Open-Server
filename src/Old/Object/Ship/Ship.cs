using System.Collections.Generic;
using System.Linq;
using FLServer.DataWorkers;
using FLServer.Object.Base;
using FLServer.Object.Solar;
using FLServer.Physics;
using FLServer.Player;
using FLServer.Server;
using FLServer.Ship;
using FLServer.Simulators;

namespace FLServer.Old.Object.Ship
{

    ///0 - environment, 1-gun,
    ///         2-missile,3-mine,4-collision,5-admin command.
    /// 

    public enum DeathCause
    {
        Environment,
        Gun,
        Missile,
        Mine,
        Collision,
        Command
    }

    public class CollisionGroup
    {
        public float health;
        public uint id;
        public float max_hit_pts;
    }

    

    

    public class ThrusterSim : IUpdatable
    {
        private ThrusterArchetype arch;
        public ShipItem item;
        private Ship ship;

        public ThrusterSim(Ship ship, ShipItem item)
        {
            this.ship = ship;
            this.item = item;
            arch = item.arch as ThrusterArchetype;
        }


        public bool Update(float deltaSeconds)
        {
            return true;
        }
    }

    public class GunSim : IUpdatable
    {
        private GunArchetype arch;
        public ShipItem item;
        private Ship ship;

        public GunSim(Ship ship, ShipItem item)
        {
            this.ship = ship;
            this.item = item;
            arch = item.arch as GunArchetype;
        }

        public bool Update(float deltaSeconds)
        {
            return true;
        }
    }

    public class Ship : SimObject
    {
        public List<uint> Accessories = new List<uint>();

        /// <summary>
        ///     The ai for the ship if this is a NPC
        /// </summary>
        public AI.ShipAI AI = null;

        /// <summary>
        ///     The factor to reduce damage by when taking hull hits due to the
        ///     armor the ship has.
        /// </summary>
        private float _armorDamageFactor = 1.0f;

        /// <summary>
        /// Ship's scanner archetype.
        /// </summary>
        public ScannerArchetype Scanner;

        /// <summary>
        ///     The current base the ship is docked at. Will be null if the ship
        ///     is in space.
        /// </summary>
        public BaseData Basedata;

        /// <summary>
        ///     Health of collision groups for multiple part ships.
        /// </summary>
        public List<CollisionGroup> cols = new List<CollisionGroup>();

        public uint com_body;
        public uint com_head;
        public uint com_lefthand;
        public uint com_righthand;

        /// <summary>
        ///     The point where this ship is either launching or docking with
        /// </summary>
        public Action CurrentAction;

        /// <summary>
        ///     An estimate of the velocity vector based on position updates
        /// </summary>
        public Vector EstimatedVelocity = new Vector();

        /// <summary>
        ///     The faction that this ship is affiliated with. May never be null.
        /// </summary>
        public Faction faction = new Faction();

        /// <summary>
        ///     The cargo and equipment in this ship.
        /// </summary>
        public Dictionary<uint, ShipItem> Items = new Dictionary<uint, ShipItem>();

        /// <summary>
        ///     The time the estimated_velocity field was updated based
        ///     on a position update from the client.
        /// </summary>
        public double LastVelocityUpdate;

        /// <summary>
        ///     Drag on the ship.
        /// </summary>
        private float _linearDrag;

        /// <summary>
        ///     Force in the direction the ship is facing.
        /// </summary>
        private float _linearForce;

        /// <summary>
        ///     Force in the direction the ship is facing if the thrusters are on.
        /// </summary>
        private float _linearThrusterForce;

        /// <summary>
        ///     The owning player or null if this is an NPC.
        /// </summary>
        public Player.Player player;

        /// <summary>
        ///     This is the reference to the ship's power generator
        /// </summary>
        public PowerSim Powergen;

        /// <summary>
        ///     The rank of the ship
        /// </summary>
        public uint Rank;

        /// <summary>
        ///     The reputation for each faction for this ship.
        /// </summary>
        public Dictionary<Faction, float> Reps = new Dictionary<Faction, float>();

        /// <summary>
        ///     The base the ship should respawn at after death. This is only really
        ///     valid for player ships.
        /// </summary>
        public BaseData RespawnBasedata;

        private Quaternion rotation = new Quaternion();

        /// <summary>
        ///     Although freelancer supports multiple shields, we only support a single
        ///     main shield. This is defined to be the shield with the most capacity.
        ///     If the ship has no shield then this is null.
        /// </summary>
        public ShieldGeneratorSim Shield;

        /// <summary>
        ///     The amount that we're changing the rotation of the ship by.
        /// </summary>
        public Quaternion steering_angular = new Quaternion();

        /// <summary>
        ///     The system this ship is in.
        /// </summary>
        public StarSystem System;

        /// <summary>
        ///     The current target of this ship. 0 if there's no target.
        /// </summary>
        public uint TargetObjID;

        public bool IsDestroyed = false;

        /// <summary>
        ///     If target_objid is non-zero then this is the subobj target.
        /// </summary>
        public uint target_subobjid;

        public Vector velocity = new Vector();
        public uint voiceid;

        /// <summary>
        ///     Initialise the ship.
        /// </summary>
        /// <param name="runner"></param>
        public Ship(DPGameRunner runner) : base(runner)
        {
            //max_turning_speed = new Vector(steering_torque);
            //max_turning_speed /= arch.angular_drag;
        }

        public Vector PositionHpMount
        {
            get { return Position + Orientation*Arch.HpMount.Position; }
        }

        /// <summary>
        ///     This method checks the ship's equipment/cargo list and rebuilds
        ///     the simulations for the power generator, thrusters, etc.
        /// </summary>
        public void InitialiseEquipmentSimulation()
        {
            _armorDamageFactor = 1.0f;

            var shipArch = Arch as ShipArchetype;
// ReSharper disable once PossibleNullReferenceException
            _linearDrag = shipArch.LinearDrag;
            _linearThrusterForce = 0.0f;
            _linearForce = 0.0f;

            foreach (var item in Items.Values)
            {
                if (item.arch is ShieldGeneratorArchetype && item.mounted)
                {
                    var newShield = new ShieldGeneratorSim(this, (ShieldGeneratorArchetype)item.arch,Powergen);
                    if (Shield == null)
                        Shield = newShield;
                    else if (Shield.Health < newShield.Health)
                        Shield = newShield;
                    item.sim = newShield;
                }
                else if (item.arch is PowerArchetype && item.mounted)
                {
                    item.sim = new PowerSim(this, (PowerArchetype)item.arch, item);
                    if (Powergen == null)
                        Powergen = item.sim as PowerSim;
                }
                else if (item.arch is ThrusterArchetype && item.mounted)
                {
                    item.sim = new ThrusterSim(this, item);
                    var thruster = item.arch as ThrusterArchetype;
                    _linearThrusterForce += thruster.MaxForce;
                }
                else if (item.arch is GunArchetype && item.mounted)
                {
                    item.sim = new GunSim(this, item);
                }
                else if (item.arch is ArmorArchetype)
                {
                    // hit points scale has the effect of increasing the base hit points
                    // the damage factor is the inverse of this
                    var armorArch = item.arch as ArmorArchetype;
                    _armorDamageFactor += (1/armorArch.HitPtsScale);
                }
                else if (item.arch is EngineArchetype && item.mounted)
                {
                    var engine = item.arch as EngineArchetype;
                    _linearForce += engine.MaxForce;
                    _linearDrag += engine.LinearDrag;
                }
                else if (item.arch is ScannerArchetype && item.mounted)
                {
                    Scanner = item.arch as ScannerArchetype;
                    BucketRange = Scanner.ScanRange;
                }
            }
        }

        public ShipItem FindItemByGood(uint goodid)
        {
            return Items.Values.FirstOrDefault(item => item.arch.ArchetypeID == goodid);
        }

        public ShipItem FindByHpid(uint hpid)
        {
            if (Items.ContainsKey(hpid))
                return Items[hpid];
            return null;
        }

        public ShipItem FindByHardpoint(string hpname)
        {
            return Items.Values.FirstOrDefault(item => item.hpname == hpname);
        }

        public uint FindFreeHpid()
        {
            uint hpid = 2;
            while (FindByHpid(hpid) != null)
                hpid++;
            return hpid;
        }

        public float GetAttitudeTowardsFaction(Faction other_faction)
        {
            // If the ship doesn't know about this faction then add it
            // with the default faction affiliaton from the initialworld
            // settings
            if (!Reps.ContainsKey(other_faction))
            {
                Reps[other_faction] = 0.0f; //fixme solar.faction.default_rep;
            }

            return Reps[other_faction];
        }

        public void SetReputation(Faction other_faction, float attitude)
        {
            Reps[other_faction] = attitude;
            // fixme: notify
        }

        public void UseItem(uint hpid)
        {
            //TODO: move healing of bats/bots to cfg?
            const float HIT_PTS_PER_BAT = 300;
            const float HIT_PTS_PER_BOT = 600;

            ShipItem item = player.Ship.FindByHpid(hpid);
            if (item == null)
                return;

            // Calculate the number of shield bats needed to fully recharge the shield and if possible
            // repair the shield and use the items.
            if (item.arch is ShieldBatteryArchetype && Shield != null && Shield.Health < 1.0f)
            {
                uint bats_needed = 1 + (uint) (((1.0f - Shield.Health)*Shield.Arch.MaxCapacity)/HIT_PTS_PER_BAT);
                if (bats_needed > item.count)
                    bats_needed = item.count;

                if (bats_needed > 0)
                {
                    Items[item.hpid].count -= bats_needed;
                    if (player != null)
                    {
                        Packets.SendUseItem(player, Objid, item.hpid, bats_needed);
                    }
                    RepairShield(bats_needed*HIT_PTS_PER_BAT);
                }
            }
                // Otherwise try to repair the hull
            else if (item.arch is RepairKitArchetype && Health < 1.0f)
            {
                uint bots_needed = 1 + (uint) (((1.0f - Health)*Arch.HitPts)/HIT_PTS_PER_BOT);
                if (bots_needed > item.count)
                    bots_needed = item.count;

                if (bots_needed > 0)
                {
                    Items[item.hpid].count -= bots_needed;
                    if (player != null)
                    {
                        Packets.SendUseItem(player, Objid, item.hpid, bots_needed);
                    }
                    RepairHull(bots_needed*HIT_PTS_PER_BOT);
                }
            }
        }

        /// <summary>
        ///     Damage the ship, either the shield, external equipment or hull.
        /// Use shield damage to damage shield or drain power.
        /// Hull damage is absorbed by shield if it's on, anything left goes on hull.
        /// </summary>
        /// <param name="hpid">Subtarget taking damage</param>
        /// <param name="energy_damage">The energy damage in hitpts</param>
        /// <param name="hull_damage">The hull damage in hitpts</param>
        /// <param name="type">Weapon type.</param>
        /// <returns>Returns true if the ship or an equipment item was destroyed</returns>
        public bool Damage(uint hpid, float energy_damage, float hull_damage, DeathCause cause)
        {
            if (energy_damage < 0 || hull_damage < 0)
                return false;

            if (energy_damage == 0 && hull_damage == 0)
                return false;

            // If we have an active shield ignore hits on the hull or external equipment
            // and make them on the shield.
            if (Shield != null && (Shield.Health > 0 || hpid == 0xFFF1))
            {
                // Step 1: calculate shield damage from energy damage
                float relative_shield_damage = (energy_damage*1.0f)/Shield.Arch.MaxCapacity;
                Shield.Health -= relative_shield_damage;
                if (Shield.Health <= 0)
                {
                    energy_damage = -Shield.Health*1.0f*Shield.Arch.MaxCapacity;
                    Shield.Health = 0;
                }
                else
                {
                    energy_damage = 0;

                    // Step 2 : calculate shield damage from hull damage if the shield is still up
                    relative_shield_damage = (hull_damage*0.5f)/Shield.Arch.MaxCapacity;
                    Shield.Health -= relative_shield_damage;
                    if (Shield.Health < 0)
                    {
                        hull_damage = -Shield.Health*1.0f*Shield.Arch.MaxCapacity;
                        Shield.Health = 0;
                    }
                    else
                        hull_damage = 0;
                }

                if (Shield.Health < Shield.Arch.OfflineThreshold)
                    Shield.OfflineTime = Shield.Arch.OfflineRebuildTime;
                Runner.NotifyOnSetHitPoints(Objid, DamageListItem.SHIELD, Shield.Health*Shield.Arch.MaxCapacity, false);
            }

            // Any remaining energy damage hits the powerplant
            if (energy_damage > 0)
            {
                Powergen.CurrPower -= energy_damage;
                Runner.NotifyOnSetHitPoints(Objid, Powergen.Item.hpid, Powergen.CurrPower, false);
            }

            // Avoid running the rest if the shield absorbed all damage
            if (hull_damage <= 0)
                return false;

            bool hasDestroyedSomething = false;

            // Otherwise if this is a hit on an external equipment item do the hit on that
            ShipItem item = FindByHpid(hpid);
            if (item != null && item.arch is ExternalEquipmentArechetype && item.health > 0)
            {
                item.health -= (hull_damage/item.arch.HitPts)*_armorDamageFactor;
                if (item.health < 0)
                {
                    hull_damage = -item.health*item.arch.HitPts/_armorDamageFactor;
                    item.health = 0;
                }

                if (item.health == 0)
                {
                    Runner.NotifyOnSetHitPoints(Objid, item.hpid, 0, true);
                    Items.Remove(item.hpid);
                    hasDestroyedSomething = true;
                }

                Runner.NotifyOnSetHitPoints(Objid, item.hpid, item.health*item.arch.HitPts, false);
            }

            // Avoid running the rest if the equipment absorbed all damage
            if (hull_damage <= 0)
                return hasDestroyedSomething;

            // Otherwise this is a hull hit.
            Health -= (hull_damage/Arch.HitPts)*_armorDamageFactor;
            if (Health < 0)
                Health = 0;

            if (Health == 0)
            {
                Destroy(cause);
                return true;
            }

            Runner.NotifyOnSetHitPoints(Objid, DamageListItem.HULL, Health*Arch.HitPts, false);
            return hasDestroyedSomething;
        }


        /// <summary>
        ///     Repair equipment by hpid if it exists
        /// </summary>
        /// <param name="hpid"></param>
        /// <param name="hit_pts"></param>
        public void RepairEquipment(uint hpid, float hit_pts)
        {
            if (hit_pts <= 0)
                return;

            // Repair equipment if we can find it.
            ShipItem item = FindByHpid(hpid);
            if (item != null && item.arch is EquipmentArchetype && item.health > 0)
                // fixme: change to external equipment
            {
                item.health += hit_pts/item.arch.HitPts;
                if (item.health > 1.0f)
                    item.health = 1.0f;
                Runner.NotifyOnSetHitPoints(Objid, item.hpid, item.health*item.arch.HitPts, false);
            }
        }

        /// <summary>
        ///     Repair the ship hull
        /// </summary>
        /// <param name="hit_pts"></param>
        public void RepairHull(float hit_pts)
        {
            if (hit_pts <= 0)
                return;

            // Otherwise repair the hull
            Health += hit_pts/Arch.HitPts;
            if (Health > 1.0f)
                Health = 1.0f;
            Runner.NotifyOnSetHitPoints(Objid, DamageListItem.HULL, Health*Arch.HitPts, false);
        }

        /// <summary>
        ///     Repair the shield but don't allow the shield to go over it's maximum capacity.
        /// </summary>
        /// <param name="hit_pts"></param>
        public void RepairShield(float hit_pts)
        {
            if (hit_pts <= 0)
                return;

            if (Shield == null)
                return;

            Shield.Health += hit_pts/Shield.Arch.MaxCapacity;
            if (Shield.Health > 1.0f)
                Shield.Health = 1.0f;

            Runner.NotifyOnSetHitPoints(Objid, DamageListItem.SHIELD, Shield.Health*Shield.Arch.MaxCapacity, false);
        }

        /// <summary>
        ///     Kill the ship immediately, playing the death fuse.
        ///     If this is a player then the charfile will be saved and the player
        ///     set to respawn.
        ///     <param name="type">
        ///         Byte describing the damage type. 0 - environment, 1-gun,
        ///         2-missile,3-mine,4-collision,5-admin command.
        ///     </param>
        /// </summary>
        public void Destroy(DeathCause cause)
        {
            Health = 0;
            IsDestroyed = true;
            Basedata = RespawnBasedata;
            Runner.NotifyOnObjDestroy(this);
                    if (player != null)
            {
                // TODO: generate death message
                Chat.Chat.SendDeathMessage(player, cause);
                player.SaveCharFile();
            }
            else
            {
                Runner.DelSimObject(this);
            }
        }

        public override bool Update(float deltaSeconds)
        {
            // If this ship is dead, do nothing.
            if (Objid == 0 || Basedata != null || Health == 0)
                return true;

            var shipArch = Arch as ShipArchetype;


            //TODO: check only zones close to it
            // ^ how do we determine which zone is close? no wai.
            // 80K radius zone may be far but still affect the ship

            // Apply zone damage to all parts of the ship if the ship is inside zones
            // that cause damage.
            var dmg_items = new List<DamageListItem>();
            foreach (var z in System.Zones)
            {
                if (z.damage > 0 && z.shape != null && z.IsInZone(Position))
                {
                    var damageHitPts = z.damage*deltaSeconds;

                    //TODO: break?
                    foreach (var item in Items.Values)
                    {
                        if (Damage(item.hpid, 0, damageHitPts, DeathCause.Environment))
                            break;
                    }
                    Damage(DamageListItem.HULL, 0, damageHitPts, DeathCause.Environment);
                }
            }

            // Run the equipment simulations.
            foreach (var item in Items.Values)
            {
                if (item.sim != null)
                {
                    item.sim.Update(deltaSeconds);
                }
            }

            // If this ship has an AI (and thus is not a player)
            // run the AI and move the ship. The ai is responsible for 
            // changing the steering and throttle.
            if (AI == null || !(deltaSeconds > 0.0)) return true;
            // and steering_linear. 
            AI.Update(this, Runner, deltaSeconds);

            double speed = Throttle*(_linearForce/_linearDrag);


            velocity = Orientation*new Vector(0, 0, -speed);

            

            // Update position and orientation.
            Position += velocity*deltaSeconds;


            //runner.log.AddLog(LogType.GENERAL, "velocity={0} speed={1} position={2} rot={3} delta_secs={4}",
            //    velocity, velocity.Length(), position, orientation, delta_seconds);

            //Quaternion orientationq = Quaternion.MatrixToQuaternion(orientation);
            //orientationq *= rotation; // *delta_seconds;
            //orientation = Quaternion.QuaternionToMatrix(orientationq);

            //switch (movement_state)
            //    case CRUISE:
            //    case ENGINE:
            //    case THRUSTER:
            //    case TRADELANE:
            //    case REVERSE:

            // The intent of the commented out code was to support "mass" impacting acceleration
            // but the FL physics engine doesn't work this way it seems.
            // Calculate and apply drag to each of the components of the velocity vector
            //float linear_drag_force = (float)velocity.Length() * linear_drag;
            //float linear_drag_acceleration = linear_drag_force / ship_arch.mass;
            //velocity -= linear_drag_acceleration * delta_seconds;
            // Calculate and apply acceleration in the direction the ship is facing. The acceleration
            // will approach zero as we reach the maximum ship speed. max_speed = linear_force / linear_drag;
            //float linear_acceleration = (throttle * linear_force) / ship_arch.mass;
            // Vector directional_acceleration = orientation * new Vector(0, 0, -linear_acceleration);

            // Clip the speed if necessary.

            // Adjust the ship rotation by the steering
            //rotation = steering_angular;

            UpdateTime += deltaSeconds;

            SetUpdateObject(Position, Orientation, Throttle, (float) UpdateTime);

            return true;
        }


        /// <summary>
        ///     Return an estimated position based on the last known position
        ///     and velocity of the ship.
        /// </summary>
        public Vector InterpolatedPosition()
        {
            if (player == null) return Position;

            var deltaTime = Runner.GameTime() - LastVelocityUpdate;
            return Position + EstimatedVelocity*deltaTime;
        }

        /// <summary>
        ///     Return an estimated position at the next tick based on the last
        ///     known position and velocity of the ship .
        /// </summary>
        public override Vector ExtrapolatedPosition()
        {
            if (player == null) return Position;
            var deltaTime = Runner.GameTime() - LastVelocityUpdate;
            return Position + EstimatedVelocity*deltaTime*2;
        }

        public void SetUpdateObject(Vector position, Matrix orientation, float throttle, float updateTime)
        {
            EstimatedVelocity = position - Position;
            EstimatedVelocity /= (updateTime - UpdateTime);
            LastVelocityUpdate = Runner.GameTime();

            Position = position;
            Orientation = orientation;
            Throttle = throttle;
            UpdateTime = updateTime;

            Runner.NotifyOnObjUpdate(this);

            if (CurrentAction is DockAction)
            {
                var action = CurrentAction as DockAction;
                if (action.DockingObj.Position.DistSqr(PositionHpMount) <=
                    action.DockingObj.DockingRadius*action.DockingObj.DockingRadius)
                {
                    uint baseid = action.DockingObj.Solar.BaseData.BaseID;
                    uint solarid = action.DockingObj.Solar.Objid;
                    Packets.SendServerLand(player, this, solarid, baseid);

                    action.DockingObj.Deactivate(player.Runner);
                    player.Ship.Basedata = action.DockingObj.Solar.BaseData;
                    player.Ship.RespawnBasedata = action.DockingObj.Solar.BaseData;
                    player.Ship.CurrentAction = null;

                    player.SaveCharFile();
                }
            }
            else if (CurrentAction is JumpAction)
            {
                var action = CurrentAction as JumpAction;
                if (!action.Activated &&
                    action.DockingObj.Position.DistSqr(PositionHpMount) <=
                    action.DockingObj.DockingRadius*action.DockingObj.DockingRadius)
                {
                    Packets.SendSystemSwitchOut(player, this, action.DockingObj.Solar);

                    action.DockingObj.Deactivate(player.Runner);
                    action.Activated = true;
                }
            }
        }
    }
}