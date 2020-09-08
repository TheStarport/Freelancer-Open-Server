namespace FLServer.Object.Base
{
    public class Room
    {
        /// <summary>
        ///     Number of NPC characters to have in room
        /// </summary>
        public uint CharacterDensity;

        /// <summary>
        ///     Room name, one of ShipDealer, Bar, Cityscape, Equipment, Trader
        /// </summary>
        public string Nickname;

        /// <summary>
        ///     Hash of room nickname
        /// </summary>
        public uint RoomID;
    }
}
