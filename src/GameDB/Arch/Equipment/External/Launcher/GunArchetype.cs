using FLDataFile;

namespace FLServer.GameDB.Arch.Equipment.External.Launcher
{

    class GunArchetype: LauncherArchetype
    {
        public float DispersionAngle;
        public float TurnRate;

        public GunArchetype(Section sec) : base(sec)
        {
            Setting tmpSet;

            if (sec.TryGetFirstOf("dispersion_angle", out tmpSet))
                DispersionAngle = float.Parse(tmpSet[0]);

            if (sec.TryGetFirstOf("turn_rate", out tmpSet))
                TurnRate = float.Parse(tmpSet[0]);
        }
    }
}
