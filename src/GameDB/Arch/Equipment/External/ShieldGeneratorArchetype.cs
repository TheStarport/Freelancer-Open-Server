using FLDataFile;

namespace FLServer.GameDB.Arch.Equipment.External
{
    class ShieldGeneratorArchetype : ExternalEquipmentArchetype
    {

        public float ConstantPowerDraw;
        public float MaxCapacity;
        public float OfflineRebuildTime;
        public float OfflineThreshold;
        public float RebuildPowerDraw;
        public float RegenerationRate;

        public ShieldGeneratorArchetype(Section sec) : base(sec)
        {
            Setting tmpSet;

            if (sec.TryGetFirstOf("regeneration_rate", out tmpSet))
                RegenerationRate = float.Parse(tmpSet[0]);

            if (sec.TryGetFirstOf("max_capacity", out tmpSet))
                MaxCapacity = float.Parse(tmpSet[0]);

            if (sec.TryGetFirstOf("offline_rebuild_time", out tmpSet))
                OfflineRebuildTime = float.Parse(tmpSet[0]);

            if (sec.TryGetFirstOf("offline_threshold", out tmpSet))
                OfflineThreshold = float.Parse(tmpSet[0]);

            if (sec.TryGetFirstOf("constant_power_draw", out tmpSet))
                ConstantPowerDraw = float.Parse(tmpSet[0]);

            if (sec.TryGetFirstOf("rebuild_power_draw", out tmpSet))
                RebuildPowerDraw = float.Parse(tmpSet[0]);
        }
    }
}
