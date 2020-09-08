using FLDataFile;
using FLServer.DataWorkers;
using Jitter.LinearMath;

namespace FLServer.GameDB.Arch.Equipment.External
{
    class LauncherArchetype : ExternalEquipmentArchetype
    {

        public float DamagePerFire;
        public JVector MuzzleVelocity = new JVector();
        public float PowerUsage;

        //TODO: getter
        private uint _projectileArchHash;

        public float RefireDelay;

        public LauncherArchetype(Section sec) : base(sec)
        {
            Setting tmpSet;

            if (sec.TryGetFirstOf("damage_per_fire", out tmpSet))
                DamagePerFire = float.Parse(tmpSet[0]);

            if (sec.TryGetFirstOf("power_usage", out tmpSet))
                PowerUsage = float.Parse(tmpSet[0]);

            if (sec.TryGetFirstOf("refire_delay", out tmpSet))
                RefireDelay = float.Parse(tmpSet[0]);

            if (sec.TryGetFirstOf("muzzle_velocity", out tmpSet))
                MuzzleVelocity = new JVector(0,0,-float.Parse(tmpSet[0]));

            if (sec.TryGetFirstOf("projectile_archetype", out tmpSet))
                _projectileArchHash = FLUtility.CreateID(tmpSet[0]);
        }
    }
}
