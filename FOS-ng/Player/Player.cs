using FOS_ng.Universe;

namespace FOS_ng.Player
{
    public class Player
    {

        /// <summary>
        ///     The accountid of the player.
        /// </summary>
        public string AccountID;

        public string Name;

        /// <summary>
        ///     The FL player ID.
        /// </summary>
        public uint FLPlayerID;

        /// <summary>
        /// Directplay ID.
        /// </summary>
        public uint DPlayID;

        public Objects.Ship.Ship Ship;

        public string SysNickname;

        public static MessagePump MPump;

        public Session DPSess;

        public Player(Session dpSession,uint dPlayID, uint playerID)
        {
            FLPlayerID = playerID;
            DPlayID = dPlayID;
            DPSess = dpSession;
        }

        public MessagePump MessagePump;

    }
}
