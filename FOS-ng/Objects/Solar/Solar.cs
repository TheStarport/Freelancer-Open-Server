using System;
using System.Collections.Generic;
using System.Linq;
using FLServer.Object.Base;
using FLServer.Object.Solar;
using FLServer.Ship;
using FOS_ng.Data.Arch;
using Jitter.LinearMath;

namespace FOS_ng.Objects.Solar
{
    
    

    public class Solar : SimObject
    {



        private readonly Random _rand = new Random();

        /// <summary>
        ///     If this solar is for a base then this will reference the base data.
        ///     This will be null if this is not a dockable base solar.
        /// </summary>
        public BaseData BaseData;

        /// <summary>
        ///     If this solar is a jumphole/jumpgate then this will reference the destination solar (which implies the system)
        ///     This will be null if this is not a jumphole/jumpgate.
        /// </summary>
        public uint DestinationObjid;

        /// <summary>
        ///     If this solar is a jumphole/jumpgate then this will reference the destination system
        ///     This will be null if this is not a jumphole/jumpgate.
        /// </summary>
        public uint DestinationSystemid;

        public List<DockingObject> DockingObjs = new List<DockingObject>();

        /// <summary>
        ///     The owning faction for the solar. Will never be null.
        /// </summary>
        public Faction Faction = new Faction();

        /// <summary>
        ///     If this solar is a tradelane ring, the prev and/or next ring IDs will be stored here
        ///     This will be null if this is not a jumphole/jumpgate.
        /// </summary>
        public uint NextRing;

        public ShieldGeneratorSim ShieldSim;
        public PowerSim PowerGen;
        public Loadout Loadout;

        public AI.SolarAI AI;

        public string Nickname;

        /// <summary>
        ///     If this solar is a tradelane ring, the prev and/or next ring IDs will be stored here
        ///     This will be null if this is not a jumphole/jumpgate.
        /// </summary>
        public uint PrevRing;

        public StarSystem System;

        public Solar(StarSystem system,string nickname)
            : base(null)
        {
            System = system;
            Nickname = nickname;
            Objid = FLUtility.CreateID(Nickname);
        }


        /// <summary>
        ///     The equip on this solar.
        /// </summary>
        public Dictionary<uint, ShipItem> Items = new Dictionary<uint, ShipItem>();

        public void GetLoadout()
        {
            Loadout = UniverseDB.FindLoadout(Arch.Nickname);
            if (Loadout != null)
            {
                AI = new AI.SolarAI();
                var firstOrDefault = Loadout.Items.FirstOrDefault(item => item.arch.GetType() == typeof(PowerArchetype));
                if (firstOrDefault != null)
                    PowerGen = new PowerSim(this, (PowerArchetype) firstOrDefault.arch, firstOrDefault);
                else return;

                firstOrDefault =
                    Loadout.Items.FirstOrDefault(item => item.arch.GetType() == typeof (ShieldGeneratorArchetype));
                if (firstOrDefault != null)
                {
                    ShieldSim = new ShieldGeneratorSim(this, (ShieldGeneratorArchetype)firstOrDefault.arch, PowerGen)
                    {
                        Power = PowerGen
                    };
                }
                


            }
        }

        public HardpointData FindHp(string hpname)
        {
            return Arch.Hardpoints[hpname.ToLower()];
        }

        public DockingObject GetDockingPoint(FOS_ng.Objects.Ship.Ship ship)
        {
            var validDockingObjs = new List<DockingObject>();

            ShipArchetype.MissionProperty mp = ((ShipArchetype) ship.Arch).mission_property;

            int minType = Int32.MaxValue;
            foreach (DockingObject obj in DockingObjs)
            {
                if (obj.CanDock(mp))
                {
                    if (obj.Type == DockingPoint.DockingSphere.RING)
                    {
                        if (obj.Index == 0)
                            return obj;
                    }
                    else if ((int) obj.Type <= minType)
                    {
                        if ((int) obj.Type < minType)
                        {
                            validDockingObjs.Clear();
                            minType = (int) obj.Type;
                        }

                        validDockingObjs.Add(obj);
                    }
                }
            }

            if (validDockingObjs.Count == 0)
                return null;

            int selectedPoint = _rand.Next(Math.Min(2, validDockingObjs.Count));

            return validDockingObjs.OrderBy(x => x.Position.DistSqr(ship.Position)).ElementAt(selectedPoint);
        }

        public override bool Update(float deltaSeconds)
        {
            if (PowerGen != null) PowerGen.Update(deltaSeconds);
            if (ShieldSim != null) ShieldSim.Update(deltaSeconds);
            return true;
        }

        /// <summary>
        ///     Damage the solar.
        /// </summary>
        /// <param name="energyDamage">The energy damage in hitpts</param>
        /// <param name="hullDamage">The hull damage in hitpts</param>
        /// <returns>Returns true if the ship or an equipment item was destroyed</returns>
        public bool Damage(float energyDamage, float hullDamage)
        {

            if (energyDamage < 0 || hullDamage < 0)
                return false;

            if (energyDamage == 0 && hullDamage == 0)
                return false;

            // If we have an active shield ignore hits on the hull or external equipment
            // and make them on the shield.
            if (ShieldSim != null && (ShieldSim.Health > 0))
            {
                // Step 1: calculate shield damage from energy damage
                float relativeShieldDamage = (energyDamage * 1.0f) / ShieldSim.Arch.MaxCapacity;
                ShieldSim.Health -= relativeShieldDamage;
                if (ShieldSim.Health <= 0)
                {
                    energyDamage = -ShieldSim.Health * 1.0f * ShieldSim.Arch.MaxCapacity;
                    ShieldSim.Health = 0;
                }

                else
                {
                    
                  energyDamage = 0;

                    // Step 2 : calculate shield damage from hull damage if the shield is still up
                    relativeShieldDamage = (hullDamage * 0.5f) / ShieldSim.Arch.MaxCapacity;
                    ShieldSim.Health -= relativeShieldDamage;
                    if (ShieldSim.Health < 0)
                    {
                        hullDamage = -ShieldSim.Health * 1.0f * ShieldSim.Arch.MaxCapacity;
                        ShieldSim.Health = 0;
                    }
                    else
                        hullDamage = 0;
                }

                if (ShieldSim.Health < ShieldSim.Arch.OfflineThreshold)
                    ShieldSim.OfflineTime = ShieldSim.Arch.OfflineRebuildTime;

                //TODO: notify doesn't work
                Runner.NotifyOnSetHitPoints(Objid, 1 , ShieldSim.Health * ShieldSim.Arch.MaxCapacity, false);
            }

            // Any remaining energy damage hits the powerplant
            if ((PowerGen != null) & (energyDamage > 0))
            {
                PowerGen.CurrPower -= energyDamage;
                //same
                Runner.NotifyOnSetHitPoints(Objid, PowerGen.Item.hpid, PowerGen.CurrPower, false);
            }

            // Avoid running the rest if the shield absorbed all damage
            if (hullDamage <= 0)
                return false;

            // TODO: Solar damage/ TL damage
            // Otherwise this is a hull hit.
            // TODO: why the Health is negative?
            //Health -= (hullDamage / Arch.HitPts) * 0.01f; //TODO: armor_damage_factor
            //if (Health < 0)
            //    Health = 0;

            //if (Health == 0)
            //{
            //    //TODO: destroy solar
            //    //Destroy(type);
            //    return true;
            //}

            //Runner.NotifyOnSetHitPoints(Objid, DamageListItem.HULL, Health * Arch.HitPts, true);
            return false;
        }

        public override Vector ExtrapolatedPosition()
        {
            return Position;
        }
    };

    public class DockingObject
    {
        /// <summary>
        /// </summary>
        public float DockingRadius;

        /// <summary>
        ///     The index of this object with reference to the docking_sphere entries in the archetype.
        ///     This is used to indicate which hardpoint the ship should dock at.
        /// </summary>
        public uint Index;

        /// <summary>
        ///     The position in system space
        /// </summary>
        public JVector Position;

        /// <summary>
        ///     The rotation of the object in system space
        /// </summary>
        public JMatrix Rotation;

        /// <summary>
        ///     The ship that is docking or launching from this docking point.
        /// </summary>
        public FOS_ng.Objects.Ship.Ship Ship;

        /// <summary>
        ///     The solar is object is part of
        /// </summary>
        public Solar Solar;

        /// <summary>
        ///     The type of object, one of ring, jump, berth, moor
        /// </summary>
        public DockingPoint.DockingSphere Type;

        /// <summary>
        ///     Activate the docking point (i.e. open the door)
        /// </summary>
        public void Activate(Ship.Ship ship)
        {
            Ship = ship;
            runner.SendActivateObject(this, true, Type == DockingPoint.DockingSphere.TRADELANE_RING ? ship.Objid : Index);
        }

        /// <summary>
        ///     Activate the docking point (i.e. close the door)
        /// </summary>
        public void Deactivate(DPGameRunner runner)
        {
            runner.SendActivateObject(this, false,
                Type == DockingPoint.DockingSphere.TRADELANE_RING ? Ship.Objid : Index);
            Ship = null;
        }

        public bool CanDock(ShipArchetype.MissionProperty mp)
        {
            return (int) mp <= (int) Type;
        }
    };
}