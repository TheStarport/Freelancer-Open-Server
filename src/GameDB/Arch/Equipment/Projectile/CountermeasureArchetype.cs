using FLDataFile;

namespace FLServer.GameDB.Arch.Equipment.Projectile
{
    class CountermeasureArchetype :ProjectileArchetype
    {

        public float DiversionPctg;
        public float LinearDrag;
        public float Range;

        public CountermeasureArchetype(Section sec) : base(sec)
        {
            Setting tmpSet;

            if (sec.TryGetFirstOf("linear_drag", out tmpSet))
                LinearDrag = float.Parse(tmpSet[0]);

            if (sec.TryGetFirstOf("diversion_pctg", out tmpSet))
                DiversionPctg = float.Parse(tmpSet[0]);

            if (sec.TryGetFirstOf("range", out tmpSet))
                Range = float.Parse(tmpSet[0]);
        }
    }
}
