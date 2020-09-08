using FLServer.DataWorkers;

namespace FLServer.Object.Base
{
    public class Good
    {
        public enum Category
        {
            ShipHull,
            ShipPackage,
            Commodity,
            Equipment
        }

        public float BasePrice;
        public Category category;
        public bool Combinable;

        /// <summary>
        ///     If the category is not ShipHull then this contains the archetype of the ship or
        ///     equipment for this good and "shiphull" will be null.
        /// </summary>
        public Archetype EquipmentOrShipArch;

        public uint GoodID;
        public string Nickname;

        /// <summary>
        ///     If the category is ShipHull then this contains the reference the good that contains
        ///     the ship's hull (and it's price). The "EquipmentOrShipArch" will be null.
        /// </summary>
        public Good Shiphull;
    }
}