using System;
using System.Collections.Generic;
using FLServer.DataWorkers;
using FLServer.Physics;
using FLServer.Server;

namespace FLServer.AI
{
    public class ShipAI :AI
    {

        protected readonly List<Waypoint> waypoints = new List<Waypoint>();
        private BuzzHeadTowardBlock buzz_head_toward = new BuzzHeadTowardBlock();
        private BuzzPassByBlock buzz_pass_by = new BuzzPassByBlock();

        protected readonly Old.Object.Ship.Ship Ship;

        private int _currWp;
        private AIState _state = AIState.Patrol;
        private byte Status = 0;

        [Flags]
        internal enum BuzzHeadTowardStyle
        {
            STRAIGHT_TO = 0x01,
            SLIDE = 0x02,
            WAGGLE = 0x04
        }

        internal class BuzzHeadTowardBlock
        {
            public float buzz_dodge_cone_angle = 20;
            public float buzz_dodge_cone_angle_variance_percent = 0.500000f;
            public float buzz_dodge_interval_time = 30;
            public float buzz_dodge_interval_time_variance_percent = 0.100000f;
            public float buzz_dodge_roll_angle = 20;
            public float buzz_dodge_turn_throttle = 1;
            public float buzz_dodge_waggle_axis_cone_angle = 0;
            public float buzz_head_toward_engine_throttle = 1;
            public bool buzz_head_toward_roll_flip_direction = false;
            public float buzz_head_toward_roll_throttle = 1;
            public BuzzHeadTowardStyle buzz_head_toward_style = BuzzHeadTowardStyle.STRAIGHT_TO;
            public float buzz_head_toward_turn_throttle = 1;
            public float buzz_max_time_to_head_away = 7;
            public float buzz_min_distance_to_head_toward = 600;
            public float buzz_min_distance_to_head_toward_variance_percent = 0.200000f;
        }

        [Flags]
        internal enum BuzzPassByStyle
        {
// ReSharper disable InconsistentNaming
            STRAIGHT_BY = 1,
            BREAK_AWAY = 2
// ReSharper restore InconsistentNaming
        }

        [Flags]
        internal enum BuzzBreakDirection
        {
// ReSharper disable InconsistentNaming
            LEFT = 1,
            RIGHT = 2,
            UP = 4,
            DOWN = 8
// ReSharper restore InconsistentNaming
        }

        internal class BuzzPassByBlock
        {
            private BuzzBreakDirection buzz_break_direction = BuzzBreakDirection.LEFT | BuzzBreakDirection.RIGHT;
            private float buzz_break_direction_cone_angle = 90f;
            private float buzz_break_turn_throttle = 0.500000f;
            private float buzz_distance_to_pass_by = 200;
            private bool buzz_drop_bomb_on_pass_by = false;
            private float buzz_pass_by_roll_throttle = 1;
            private BuzzPassByStyle buzz_pass_by_style = BuzzPassByStyle.STRAIGHT_BY;
            private float buzz_pass_by_time = 1f;
        }

        internal enum AIState
        {
            Patrol,
            Flee,
            Attack,
            Attack_BuzzHeadToward,
            Attack_BuzzPassBy,
        }

        public Old.Object.Ship.Ship CurrentTarget;

        public ShipAI(Old.Object.Ship.Ship ship)
        {
            Ship = ship;
        }

        private AIState ProcessState(DPGameRunner runner, double seconds)
        {

            switch (_state)
            {
                case AIState.Patrol:
                    Patrol(runner);
                    break;

                default:
                    Status = (byte)((Ship.Shield.OfflineTime * 4) + (Ship.Health * 100) + (Ship.Powergen.CurrPower / Ship.Powergen.Capacity) + (Ship.Rank));
                    Combat();
                    break;

            }

            return _state;
        }

        private void Combat()
        {
            // TODO: Check if enemies are around, return to patrol if skies clear
            if (Status < 128) { Flee();
                return;
            }
            Attack();

        }


        private void Attack()
        {
            
        }

        private void Flee()
        {
            _state = AIState.Flee;
            //TODO: get all enemy object pos from bucket and run the fuck outta them
        }


        private void Patrol(DPGameRunner runner)
        {
            var targetWaypoint = waypoints[_currWp];

            //if (SelectTarget(ship, runner) == null)
            //{
            //    ship.throttle = 0.0f;
            //}
            //else 
            if (Ship.Position.DistanceTo(targetWaypoint.Position) < 100)
            {
                _currWp++;
                if (_currWp >= waypoints.Count)
                    _currWp = 0;
                targetWaypoint = waypoints[_currWp];
                runner.Log.AddLog(LogType.GENERAL, "waypoint={0}", targetWaypoint.Position);
            }
            else
            {
                Ship.Throttle = 1.0f;

                // Calculate turning limits.
                var ship_arch = Ship.Arch as ShipArchetype;
                Vector maximum_angular_velocity = ship_arch.SteeringTorque / ship_arch.AngularDrag;
                Vector angular_acceleration = ship_arch.SteeringTorque / ship_arch.RotationInertia;

                double delta_angle = 0.0;
                double steering_yaw = 0.0;
                double steering_pitch = 0.0;
                double steering_roll = 0.0;

                Vector delta_target = targetWaypoint.Position - Ship.Position;
                delta_target = delta_target.Normalize();

                // If the target is behind the ship, turn at maximum rate towards it.
                double cosine = Ship.Orientation.GetBackward().Inverse().Dot(delta_target);
                if (cosine <= -0.999)
                {
                    steering_yaw = 1.0; // fixme
                    steering_pitch = 0.0;
                }
                // If the target is somewhere other than in directly in front
                else if (cosine < 0.98)
                {
                    delta_angle = Math.Acos(cosine);

                    Vector axis = Ship.Orientation.GetBackward().Cross(delta_target);

                    steering_yaw = -axis.Dot(Ship.Orientation.GetUp()) * delta_angle;
                    steering_pitch = axis.Dot(Ship.Orientation.GetRight()) * delta_angle;
                }
                // Otherwise the target is somewhere in front of the ship
                else
                {
                    steering_yaw = Math.Acos(Ship.Orientation.GetRight().Dot(delta_target)) - 0.5f * Math.PI;
                    steering_pitch = Math.Acos(Ship.Orientation.GetUp().Dot(delta_target)) - 0.5f * Math.PI;
                }

                // Steering yaw/pitch and roll are meant to represent the relative hardness of the
                // turn. i.e. a yaw of 1.0 is a max_rate angular acceleration in that axis.

                Ship.Orientation *= Matrix.EulerToMatrix(new Vector(steering_pitch, steering_yaw, steering_roll));

                //Console.WriteLine("yaw={0} pitch={1}", steering_yaw, steering_pitch);
            }
        }
        public override void Update(Old.Object.Ship.Ship ship, DPGameRunner server, double seconds)
        {
            _state = ProcessState(server, seconds);
        }

    }
}
