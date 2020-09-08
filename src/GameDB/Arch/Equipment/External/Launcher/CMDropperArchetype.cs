using FLDataFile;

namespace FLServer.GameDB.Arch.Equipment.External.Launcher
{
    class CMDropperArchetype : LauncherArchetype
    {
        public float AiRange;
        public CMDropperArchetype(Section sec) : base(sec)
        {
            Setting tmpSet;

            if (sec.TryGetFirstOf("ai_range", out tmpSet))
                AiRange = float.Parse(tmpSet[0]);

        }
    }
}
