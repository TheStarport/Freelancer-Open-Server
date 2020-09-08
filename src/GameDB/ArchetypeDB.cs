using System.Collections.Generic;
using FLDataFile;
using FLServer.DataWorkers;
using Archetype = FLServer.GameDB.Arch.Archetype;
using MotorArchetype = FLServer.GameDB.Arch.MotorArchetype;
using WeaponType = FLServer.GameDB.Arch.Weapon.WeaponType;

namespace FLServer.GameDB
{
    static class ArchetypeDB
    {
        public static Dictionary<uint,WeaponType> WeaponDamageModifier =  new Dictionary<uint, WeaponType>();
		private static Dictionary<uint,Archetype> Archetypes = new Dictionary<uint, Archetype>();


		public static Archetype GetArchetype(uint hash)
		{
			if (!Archetypes.ContainsKey (hash))
				return null;
			return Archetypes [hash];
		}

		public static Archetype GetArchetype(string nickname)
		{
			return GetArchetype (FLUtility.CreateID (nickname));
		}

        public static void Load(string datapath)
        {
            
        }

        public static void LoadWeaponType(Section sec)
        {
            var type = new WeaponType() {Nickname = sec.GetFirstOf("nickname")[0]};
            foreach (var entry in sec.GetSettings("shield_mod"))
            {
                type.ShieldMods.Add(FLUtility.CreateID(entry[0]),float.Parse(entry[1]));
            }
            WeaponDamageModifier[FLUtility.CreateID(type.Nickname)] = type;
        }

        public static void LoadArch(Section sec)
        {
            Archetype arch;
            switch (sec.Name.ToLowerInvariant())
            {
                case "motor":
                    arch = new MotorArchetype(sec);
                    break;
                case "munition":
                    break;

            }
        }

    }
}
