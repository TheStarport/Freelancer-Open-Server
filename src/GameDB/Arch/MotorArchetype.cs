using FLDataFile;

namespace FLServer.GameDB.Arch
{
    class MotorArchetype : Archetype
    {

        public float Acceleration;
        public float AiRange;
        public float Delay;
        public float Lifetime;

        public MotorArchetype(Section sec) : base(sec)
        {
            Setting tmpSet;

            if (sec.TryGetFirstOf("ai_range", out tmpSet))
                AiRange = float.Parse(tmpSet[0]);

            if (sec.TryGetFirstOf("lifetime", out tmpSet))
                Lifetime = float.Parse(tmpSet[0]);

            if (sec.TryGetFirstOf("accel", out tmpSet))
                Acceleration = float.Parse(tmpSet[0]);

            if (sec.TryGetFirstOf("delay", out tmpSet))
                Delay = float.Parse(tmpSet[0]);
        }
    }
}
