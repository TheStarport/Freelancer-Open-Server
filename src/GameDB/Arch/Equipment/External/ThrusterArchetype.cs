using FLDataFile;

namespace FLServer.GameDB.Arch.Equipment.External
{
    class ThrusterArchetype :ExternalEquipmentArchetype
    {
        public float MaxForce;
        public float PowerUsage;

        public ThrusterArchetype(Section sec) : base(sec)
        {
            Setting tmpSet;

            if (sec.TryGetFirstOf("max_force", out tmpSet))
                MaxForce = float.Parse(tmpSet[0]);

            if (sec.TryGetFirstOf("power_usage", out tmpSet))
                PowerUsage = float.Parse(tmpSet[0]);
        }
    }
}
