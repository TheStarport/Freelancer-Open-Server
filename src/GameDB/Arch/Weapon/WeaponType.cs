using System.Collections.Generic;

namespace FLServer.GameDB.Arch.Weapon
{
    class WeaponType
    {
        /// <summary>
        ///     The nickname of the weapon type.
        /// </summary>
        public string Nickname;

        /// <summary>
        ///     Map of shield_typeid to shield factor used to determine amount of damage
        ///     an energy hit makes to ship.
        /// </summary>
        public Dictionary<uint, float> ShieldMods = new Dictionary<uint, float>();
    }
}
