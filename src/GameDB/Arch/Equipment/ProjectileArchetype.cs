using FLDataFile;

namespace FLServer.GameDB.Arch.Equipment
{
    class ProjectileArchetype : EquipmentArchetype
    {
        public bool ForceGunOri;
        public float Lifetime;
        public float OwnerSafeTime;
        public bool RequiresAmmo;

        public ProjectileArchetype(Section sec) : base(sec)
        {
            Setting tmpSet;

            if (sec.TryGetFirstOf("lifetime", out tmpSet))
                Lifetime = float.Parse(tmpSet[0]);

            if (sec.TryGetFirstOf("owner_safe_time", out tmpSet))
                OwnerSafeTime = float.Parse(tmpSet[0]);

            if (sec.TryGetFirstOf("requires_ammo", out tmpSet))
                RequiresAmmo = bool.Parse(tmpSet[0]);

            if (sec.TryGetFirstOf("force_gun_ori", out tmpSet))
                ForceGunOri = bool.Parse(tmpSet[0]);
        }
    }
}
