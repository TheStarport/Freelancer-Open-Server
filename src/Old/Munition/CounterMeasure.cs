using System;
using FLServer.DataWorkers;
using FLServer.Object;
using FLServer.Old.Object;
using FLServer.Physics;
using FLServer.Server;
using FLServer.Ship;

namespace FLServer.Munition
{
    internal class CounterMeasure : SimObject
    {
        public static Random rand = new Random();
        public Vector angular_velocity = new Vector();
        public uint hpid;

        public double lifetime = 0;
        public CounterMeasureArchetype munition_arch;
        public uint owner_objid;
        public double owner_safe_time = 0;

        // The vector describing the object's current velocity in meters/second
        public Vector velocity = new Vector();

        public CounterMeasure(DPGameRunner runner, Old.Object.Ship.Ship owner, CounterMeasureDropperArchetype gun_arch,
            ShipItem launcher, uint hpid)
            : base(runner)
        {
            munition_arch = gun_arch.ProjectileArch as CounterMeasureArchetype;
            Arch = munition_arch;

            Position = owner.InterpolatedPosition();
            Position += owner.Orientation*owner.Arch.Hardpoints[launcher.hpname.ToLowerInvariant()].Position;
            Orientation = Matrix.TurnAround(owner.Orientation);
            velocity = Orientation*gun_arch.MuzzleVelocity + owner.EstimatedVelocity;
            owner_objid = owner.Objid;
            this.hpid = hpid;

            lifetime = munition_arch.Lifetime;
            owner_safe_time = munition_arch.OwnerSafeTime;

            Throttle = 1; // (float)(this.velocity.Length() / this.max_speed);

            angular_velocity = new Vector(rand.NextDouble()*10 - 5, rand.NextDouble()*10 - 5, rand.NextDouble()*10 - 5);
        }

        public override bool Update(float deltaSeconds)
        {
            // If the countermeasure has outlived its thing, delete it.
            lifetime -= deltaSeconds;
            if (lifetime < 0)
            {
                Runner.DelSimObject(this);
                return false;
            }

            if (owner_safe_time > 0)
            {
                owner_safe_time -= deltaSeconds;

                // activation!
                if (owner_safe_time <= 0)
                {
                    foreach (SimObject obj in Runner.Objects.Values)
                    {
                        if (obj is Missile && (obj as Missile).target_objid == owner_objid &&
                            rand.NextDouble() <= munition_arch.DiversionPctg &&
                            obj.Position.DistSqr(Position) <= munition_arch.Range*munition_arch.Range)
                        {
                            var m = obj as Missile;
                            m.target_subobjid = 0;
                            m.target_objid = Objid;
                        }
                    }
                }
            }

            velocity *= (velocity.Length() - munition_arch.LinearDrag)/velocity.Length();

            // Move the object
            Position += velocity*deltaSeconds;
            Orientation *= Matrix.EulerToMatrix(angular_velocity*deltaSeconds*180/Math.PI);

            //throttle = (float)(this.velocity.Length() / this.max_speed);
            UpdateTime += (float) deltaSeconds;
            Runner.NotifyOnObjUpdate(this);
            return true;
        }

        public override Vector ExtrapolatedPosition()
        {
            return Position;
        }
    }
}