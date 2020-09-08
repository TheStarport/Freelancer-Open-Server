using System;
using FLServer.DataWorkers;
using FLServer.Object;
using FLServer.Old.Object;
using FLServer.Physics;
using FLServer.Server;
using FLServer.Ship;

namespace FLServer.Munition
{
    internal class Missile : SimObject
    {
        public bool broadcast = false;
        public uint hpid;

        public double lifetime = 0;
        public double max_angular_velocity = 0;
        public double max_speed = 0;
        public double motor_accel = 0;
        public double motor_delay = 0;
        public double motor_lifetime = 0;
        public double motor_lifetime_max = 0;
        public MunitionArchetype munition_arch;
        public uint owner_objid;
        public bool seek = false;
        public double seeker_fov;
        public double seeker_range;
        public uint target_objid, target_subobjid;
        public double time_to_lock = 0;

        // The vector describing the object's current velocity in meters/second
        public Vector velocity = new Vector();

        public Missile(DPGameRunner runner, Old.Object.Ship.Ship owner, GunArchetype gun_arch, ShipItem launcher, Vector target_position,
            uint hpid)
            : base(runner)
        {
            munition_arch = gun_arch.ProjectileArch as MunitionArchetype;
            Arch = munition_arch;

            Position = owner.InterpolatedPosition();
            Position += owner.Orientation*owner.Arch.Hardpoints[launcher.hpname.ToLowerInvariant()].Position;
            Orientation = Matrix.CreateLookAt(Position, target_position);
            velocity = Orientation*gun_arch.MuzzleVelocity + owner.EstimatedVelocity;
            owner_objid = owner.Objid;
            target_objid = owner.TargetObjID;
            target_subobjid = owner.target_subobjid;
            this.hpid = hpid;

            lifetime = munition_arch.Lifetime;

            motor_accel = munition_arch.MotorArch.Accel;
            motor_lifetime_max = motor_lifetime = munition_arch.MotorArch.Lifetime;
            motor_delay = munition_arch.MotorArch.Delay;

            seek = (munition_arch.Seeker.ToUpper() == "LOCK") && (target_objid != 0);
            if (seek)
            {
                seeker_fov = munition_arch.SeekerFovDeg*Math.PI/180;
                seeker_range = munition_arch.SeekerRange;

                time_to_lock = munition_arch.TimeToLock;
                max_angular_velocity = munition_arch.MaxAngularVelocity*Math.PI/180;
            }

            max_speed = velocity.Length() + motor_accel*motor_lifetime_max;

            Throttle = 1; // (float)(this.velocity.Length() / this.max_speed);
        }

        public override bool Update(float deltaSeconds)
        {
            // If the missile has outlived its thing, delete it.
            lifetime -= deltaSeconds;
            if (lifetime < 0)
            {
                Runner.DelSimObject(this);
                return false;
            }

            // ideas: have certain missiles acquire a new target from owner if it disappears? have certain missiles explode when lacking a target?
            if (seek && lifetime < munition_arch.Lifetime - 1)
            {
                SimObject target = Runner.FindObject(target_objid);
                if (target == null)
                {
                    target_objid = 0;
                    target_subobjid = 0;
                    seek = false;
                    if (broadcast)
                    {
                        Runner.NotifyOnSetTarget(Objid, 0, 0);
                        broadcast = false;
                    }
                }
                else if (target.Position.DistSqr(Position) > seeker_range*seeker_range)
                {
                    time_to_lock = munition_arch.TimeToLock;
                    if (broadcast)
                    {
                        Runner.NotifyOnSetTarget(Objid, 0, 0);
                        broadcast = false;
                    }
                }
                else
                {
                    Vector current_direction = Orientation*-Vector.Z1();
                    Vector current_target_vector = target.ExtrapolatedPosition() - Position;
                    Vector current_target_direction = current_target_vector.Normalize();
                    double target_cosine = current_target_direction.Dot(current_direction);
                    double solid_angle = 1/Math.Sqrt(Math.Pow(target.Arch.Radius/current_target_vector.Length(), 2) + 1);

                    if (target_cosine < Math.Cos(seeker_fov/2) - solid_angle)
                    {
                        time_to_lock = munition_arch.TimeToLock;
                        if (broadcast)
                        {
                            Runner.NotifyOnSetTarget(Objid, 0, 0);
                            broadcast = false;
                        }
                    }
                    else if (time_to_lock > 0)
                        time_to_lock -= deltaSeconds;
                    else
                    {
                        if (!broadcast)
                        {
                            Runner.NotifyOnSetTarget(Objid, target_objid, target_subobjid);
                            broadcast = true;
                        }

                        double max_cosine = Math.Cos(max_angular_velocity*deltaSeconds);
                        if (max_cosine < target_cosine)
                        {
                            Vector cross = current_direction.Cross(current_target_direction);
                            Matrix rotate = Matrix.CreateRotationAboutAxis(cross, max_cosine);
                            Orientation *= rotate;
                            velocity = rotate*velocity/2 + velocity/2; // need to ponder on this behavior
                        }
                        else
                        {
                            Orientation = Matrix.CreateLookAt(Position, target.ExtrapolatedPosition());
                            velocity = current_target_direction*velocity.Length()/2 + velocity/2;
                        }
                    }
                }
            }

            // Calculate any velocity changes
            if (motor_delay > 0)
            {
                motor_delay -= deltaSeconds;
            }
            else if (motor_lifetime > 0)
            {
                var linear_acceleration = new Vector(0, 0, -(motor_accel*deltaSeconds));
                velocity += Orientation*linear_acceleration;
                motor_lifetime -= deltaSeconds;
            }

            // Move the object
            Position += velocity*deltaSeconds;
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