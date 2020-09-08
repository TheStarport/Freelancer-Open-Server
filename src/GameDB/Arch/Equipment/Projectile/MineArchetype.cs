using FLDataFile;

namespace FLServer.GameDB.Arch.Equipment.Projectile
{
    class MineArchetype : ProjectileArchetype
    {

        public float Acceleration;
        public float DetonationDist;
        public float LinearDrag;
        public float SeekerDist;
        public float TopSpeed;

        public MineArchetype(Section sec) : base(sec)
        {
            Setting tmpSet;

            if (sec.TryGetFirstOf("linear_drag", out tmpSet))
                LinearDrag = float.Parse(tmpSet[0]);

            if (sec.TryGetFirstOf("detonation_dist", out tmpSet))
                DetonationDist = float.Parse(tmpSet[0]);

            if (sec.TryGetFirstOf("seeker_dist", out tmpSet))
                SeekerDist = float.Parse(tmpSet[0]);

            if (sec.TryGetFirstOf("acceleration", out tmpSet))
                Acceleration = float.Parse(tmpSet[0]);

            if (sec.TryGetFirstOf("top_speed", out tmpSet))
                TopSpeed = float.Parse(tmpSet[0]);
        }
    }
}
