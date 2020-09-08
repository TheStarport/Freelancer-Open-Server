using FLServer.DataWorkers;
using FLServer.Object;
using FLServer.Old.Object;
using FLServer.Ship;

namespace FLServer.Simulators
{
    public class PowerSim : IUpdatable //ShipItemSim
    {
        private readonly PowerArchetype _arch;

        /// <summary>
        ///     The current number of units of power this ship has.
        /// </summary>
        public float CurrPower;

        public readonly float Capacity;
        public ShipItem Item;

        /// <summary>
        ///     The current number of units of thruster power this ship has.
        /// </summary>
        public float CurrPowerThruster;

        private SimObject Object;

        public PowerSim(SimObject obj, PowerArchetype arch, ShipItem item)
        {
            Object = obj;
            Item = item;
            _arch = arch;
            Capacity = _arch.Capacity;
        }

        public bool Update(float deltaSeconds)
        {
            CurrPower += _arch.ChargeRate * deltaSeconds;
            if (CurrPower > _arch.Capacity)
                CurrPower = _arch.Capacity;

            CurrPowerThruster += _arch.ThrustChargeRate * deltaSeconds;
            if (CurrPowerThruster > _arch.ThrustCapacity)
                CurrPowerThruster = _arch.ThrustCapacity;

            return true;
        }
    }
}
