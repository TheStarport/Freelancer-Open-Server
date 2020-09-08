using FOS_ng.Data.Arch;
using Jitter.LinearMath;

namespace FOS_ng.Objects
{
    public class Loot : SimObject
    {
        /// <summary>
        ///     The content of the loot container in space.
        /// </summary>
        public Archetype LootContent;

        /// <summary>
        ///     The health of the content, from 0 to 1.
        ///     TODO: It should be better to store actual hitpoints because auf createloot package (SimObject doesn't do this, too)
        /// </summary>
        public float LootContentHealth;

        /// <summary>
        ///     The amount of the content.
        /// </summary>
        public ushort LootContentQuantity;

        /// <summary>
        ///     The first mission flag (TODO: Reverse meaning).
        /// </summary>
        public bool MissionFlag1;

        /// <summary>
        ///     The second mission flag (TODO: Reverse meaning).
        /// </summary>
        public bool MissionFlag2;

        public Loot(Archetype lootCrate, float lootCrateHealth, Archetype lootContent, float lootContentHealth,
            ushort lootContentQuantity, bool missionFlag1, bool missionFlag2, JVector position)
            : base(null)
        {
            Arch = lootCrate;
            Health = lootCrateHealth;
            LootContent = lootContent;
            LootContentHealth = lootContentHealth;
            LootContentQuantity = lootContentQuantity;
            MissionFlag1 = missionFlag1;
            MissionFlag2 = missionFlag2;
            Position = position;
        }

        public override JVector ExtrapolatedPosition()
        {
            return Position;
        }
    }
}