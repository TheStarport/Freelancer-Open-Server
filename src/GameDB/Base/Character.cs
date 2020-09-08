using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FLServer.GameDB.Base
{

    public class BaseRumor
    {
        public uint Text;
    }

    class SetSegmentScripts
    {
        /// <summary>
        ///     List of scripts that can be used in this scene.
        /// </summary>
        public readonly List<string> Scripts = new List<string>();

        /// <summary>
        ///     True if these scripts are for male character, false if female
        /// </summary>
        public bool Gender;

        /// <summary>
        ///     True if these scripts are for standing character, false if sitting.
        /// </summary>
        public bool Posture;

        /// <summary>
        ///     The name of the scripts
        /// </summary>
        public string SetSegment;
    }

    /// <summary>
    /// NPC character in base.
    /// </summary>
    public class Character
    {
        public uint Body;

        /// <summary>
        ///     Bribes offered by this NPC
        /// </summary>
        public List<CharBribe> Bribes = new List<CharBribe>();

        //TODO: check faction against DB
        public string Faction;

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

		private string _room;
        /// <summary>
        ///     The room this NPC will appear in
        /// </summary>
        public string Room {
			get { return _room; }
			set {
				_room = value;
				_roomID = DataWorkers.FLUtility.CreateID (_room);
			}
		}

        /// <summary>
        ///     The location in the room the NPC will stand or sit in.
        /// </summary>
        public string RoomLocation;

		private uint _roomID;
        /// <summary>
        ///     Hash of room nickname.
        /// </summary>
        public uint RoomID {
			get { return _roomID; }
		}



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
