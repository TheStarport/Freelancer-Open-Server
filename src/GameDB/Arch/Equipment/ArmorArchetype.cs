using FLDataFile;

namespace FLServer.GameDB.Arch.Equipment
{
    class ArmorArchetype : EquipmentArchetype
    {
        public float HitPtsScale;

        public ArmorArchetype(Section sec) : base(sec)
        {
            Setting tmpSet;

            if (sec.TryGetFirstOf("hit_pts_scale", out tmpSet))
                HitPtsScale = float.Parse(tmpSet[0]);
        }
    }
}
