using FLDataFile;

namespace FLServer.GameDB.Arch.Equipment
{

    class PowerArchetype : EquipmentArchetype
    {

        public float Capacity;
        public float ChargeRate;
        public float ThrustCapacity;
        public float ThrustChargeRate;

        public PowerArchetype(Section sec) : base(sec)
        {
            Setting tmpSet;

            if (sec.TryGetFirstOf("capacity", out tmpSet))
                Capacity = float.Parse(tmpSet[0]);

            if (sec.TryGetFirstOf("charge_rate", out tmpSet))
                ChargeRate = float.Parse(tmpSet[0]);

            if (sec.TryGetFirstOf("thrust_capacity", out tmpSet))
                ThrustCapacity = float.Parse(tmpSet[0]);

            if (sec.TryGetFirstOf("thrust_charge_rate", out tmpSet))
                ThrustChargeRate = float.Parse(tmpSet[0]);

        }
    }
}
