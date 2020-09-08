using System;
using FLDataFile;
using Jitter.LinearMath;

namespace FLServer.GameDB.Arch
{
    class ShipArchetype : Archetype
    {

        public enum MissionProperty
        {
            // ReSharper disable InconsistentNaming
            // file-specific
            CAN_USE_BERTHS = 1,
            CAN_USE_SMALL_MOORS = 4,
            CAN_USE_MED_MOORS = 8,
            CAN_USE_LARGE_MOORS = 16
            // ReSharper restore InconsistentNaming
        }


        /// <summary>
        ///     Angular drag is the resistance to the steering torque.
        ///     max_rotational_velocity (radian/s) = steering_torque / angular_drag
        /// </summary>
        public JVector AngularDrag;

        public float HoldSize;

        /// <summary>
        ///     The force applied to slow the ship.
        /// </summary>
        public float LinearDrag;

        /// <summary>
        ///     Mission properties define docking parameters.
        /// </summary>
        public MissionProperty mission_property;

        public uint NanobotLimit;

        /// <summary>
        ///     The force applied to move the ship side to side when avoiding rocks.
        /// </summary>
        public float NudgeForce;

        /// <summary>
        ///     Rotational inertia is the amount of initial resistance to moving the centerline on both the start and end of a
        ///     turn.
        ///     Kind of like why a car plows down at the nose when you brake hard. The Inertia controls how snappy or mushy the
        ///     craft handles in flight.
        ///     rotational_acceleration (radian/s*s) = steering_torque / rotation_inertia;
        /// </summary>
        public JVector RotationInertia;

        public uint ShieldBatteryLimit;

        /// <summary>
        ///     Steering torque is the amount of force applied to the centerline of the craft to make it turn;
        /// </summary>
        public JVector SteeringTorque;

        /// <summary>
        ///     The force applied to move the ship side to side when strafing keys are pressed.
        /// </summary>
        public float StrafeForce;

        public ShipArchetype(Section sec) : base(sec)
        {

            Setting tmpSet;

            if (sec.TryGetFirstOf("steering_torque", out tmpSet))
                SteeringTorque = new JVector(float.Parse(tmpSet[0]), float.Parse(tmpSet[1]), float.Parse(tmpSet[2]));

            if (sec.TryGetFirstOf("angular_drag", out tmpSet))
                AngularDrag = new JVector(float.Parse(tmpSet[0]), float.Parse(tmpSet[1]), float.Parse(tmpSet[2]));

            if (sec.TryGetFirstOf("rotation_inertia", out tmpSet))
                RotationInertia = new JVector(float.Parse(tmpSet[0]), float.Parse(tmpSet[1]), float.Parse(tmpSet[2]));

            if (sec.TryGetFirstOf("nudge_force", out tmpSet))
                NudgeForce = float.Parse(tmpSet[0]);

            if (sec.TryGetFirstOf("linear_drag", out tmpSet))
                LinearDrag = float.Parse(tmpSet[0]);

            if (sec.TryGetFirstOf("hold_size", out tmpSet))
                HoldSize = float.Parse(tmpSet[0]);

            if (sec.TryGetFirstOf("strafe_force", out tmpSet))
                StrafeForce = float.Parse(tmpSet[0]);

            if (sec.TryGetFirstOf("nanobot_limit", out tmpSet))
                NanobotLimit = uint.Parse(tmpSet[0]);

            if (sec.TryGetFirstOf("shield_battery_limit", out tmpSet))
                ShieldBatteryLimit = uint.Parse(tmpSet[0]);

            if (sec.TryGetFirstOf("mission_property", out tmpSet))
                mission_property = (MissionProperty)
                        Enum.Parse(typeof(MissionProperty),
                            tmpSet[0].ToUpperInvariant());


        }
    }
}
