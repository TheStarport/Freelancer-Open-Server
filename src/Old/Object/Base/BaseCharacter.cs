using System.Collections.Generic;

namespace FLServer.Object.Base
{

    public class BaseCharacter
    {
        public uint Body;

        /// <summary>
        ///     Bribes offered by this NPC
        /// </summary>
        public List<BaseBribe> Bribes = new List<BaseBribe>();

        public Faction Faction;

        /// <summary>
        ///     The fidget script for the NPC
        /// </summary>
        public string FidgetScript;

        /// <summary>
        ///     True if male otherwise false if female
        /// </summary>
        public bool Gender = true;

        public uint Head;
        public uint IndividualName;
        public uint Lefthand;
        public string Nickname;

        /// <summary>
        ///     True if standing otherwise false if sitting
        /// </summary>
        public bool Posture = true;

        public uint Righthand;

        /// <summary>
        ///     The room this NPC will appear in
        /// </summary>
        public string Room;

        /// <summary>
        ///     The location in the room the NPC will stand or sit in.
        /// </summary>
        public string RoomLocation;

        /// <summary>
        ///     Hash of room nickname.
        /// </summary>
        public uint RoomID;

        /// <summary>
        ///     Rumors offerred by this NPC.
        /// </summary>
        public List<BaseRumor> Rumors = new List<BaseRumor>();

        /// <summary>
        ///     The NPC type, one of shipdealer, trader, equipment, bartender or nothing
        /// </summary>
        public string Type;

        public uint Voice;
    }

}
