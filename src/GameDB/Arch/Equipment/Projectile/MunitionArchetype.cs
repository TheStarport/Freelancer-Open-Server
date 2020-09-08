using FLDataFile;
using FLServer.DataWorkers;

namespace FLServer.GameDB.Arch.Equipment.Projectile
{
    class MunitionArchetype : ProjectileArchetype
    {

        public bool CruiseDisruptor;
        public float DetonationDist;
        public float EnergyDamage;
        public float HullDamage;
        public float MaxAngularVelocity;
        private uint _motorHash;
        public string Seeker;
        public float SeekerFovDeg;
        public float SeekerRange;
        public float TimeToLock;
        private uint _weaponTypeHash;

        public MunitionArchetype(Section sec) : base(sec)
        {

            Setting tmpSet;

            if (sec.TryGetFirstOf("hull_damage", out tmpSet))
                HullDamage = float.Parse(tmpSet[0]);

            if (sec.TryGetFirstOf("energy_damage", out tmpSet))
                EnergyDamage = float.Parse(tmpSet[0]);

            if (sec.TryGetFirstOf("weapon_type", out tmpSet))
                _weaponTypeHash = FLUtility.CreateID(tmpSet[0]);

            if (sec.TryGetFirstOf("seeker", out tmpSet))
                Seeker = tmpSet[0];

            if (sec.TryGetFirstOf("time_to_lock", out tmpSet))
                TimeToLock = float.Parse(tmpSet[0]);

            if (sec.TryGetFirstOf("seeker_range", out tmpSet))
                SeekerRange = float.Parse(tmpSet[0]);

            if (sec.TryGetFirstOf("seeker_fov_deg", out tmpSet))
                SeekerFovDeg = float.Parse(tmpSet[0]);

            if (sec.TryGetFirstOf("detonation_dist", out tmpSet))
                DetonationDist = float.Parse(tmpSet[0]);

            if (sec.TryGetFirstOf("max_angular_velocity", out tmpSet))
                MaxAngularVelocity = float.Parse(tmpSet[0]);

            if (sec.TryGetFirstOf("cruise_disruptor", out tmpSet))
                CruiseDisruptor = bool.Parse(tmpSet[0]);

            if (sec.TryGetFirstOf("motor", out tmpSet))
                _motorHash = FLUtility.CreateID(tmpSet[0]);


        }
    }
}
