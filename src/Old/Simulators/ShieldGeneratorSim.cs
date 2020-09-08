using FLServer.DataWorkers;
using FLServer.Object;
using FLServer.Old.Object;
using FLServer.Ship;

namespace FLServer.Simulators
{
    public class ShieldGeneratorSim : IUpdatable
    {
        /// <summary>
        ///     The owning object
        /// </summary>
        private readonly SimObject _object;

        public ShieldGeneratorArchetype Arch;
        private float _capacityAtLastNotification;
        public float Health;
        public PowerSim Power;

        /// <summary>
        /// Time until the shield is regenerated.
        /// </summary>
        public float OfflineTime;

        public ShieldGeneratorSim(SimObject obj, ShieldGeneratorArchetype arch, PowerSim sim)
        {
            Arch = arch;
            _object = obj;
            Power = sim;
            Health = 1.0f;
            _capacityAtLastNotification = Health;
        }

        public bool Update(float deltaSeconds)
        {
            if (Power == null)
                return true;

            Power.CurrPower -= Arch.ConstantPowerDraw;
            if (Power.CurrPower < 0)
                Power.CurrPower = 0;

            if (OfflineTime > 0)
            {
                if (Power.CurrPower >= Arch.RebuildPowerDraw)
                {
                    OfflineTime -= deltaSeconds;
                    if (OfflineTime <= 0)
                    {
                        OfflineTime = 0;
                        Power.CurrPower -= Arch.RebuildPowerDraw;
                    }
                }
            }
            else
            {
                if (Power.CurrPower > 0)
                {
                    Health += (Arch.RegenerationRate / Arch.MaxCapacity) * deltaSeconds;
                    if (Health > 1.0f)
                        Health = 1.0f;
                }
            }

            if ((_capacityAtLastNotification - Health) > 0.05f)
            {
                _capacityAtLastNotification = Health;
                _object.Runner.NotifyOnSetHitPoints(_object.Objid, DamageListItem.SHIELD, Health * Arch.MaxCapacity, false);
            }
            return true;
        }
    }
}
