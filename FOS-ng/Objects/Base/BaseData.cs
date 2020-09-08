using System;
using System.Collections.Generic;
using FLServer.DataWorkers;
using FLServer.Object.Solar;
using FOS_ng.Objects.Solar;

namespace FLServer.Object.Base
{
    public class BaseData
    {
        private readonly Random _rand = new Random();

        /// <summary>
        ///     The hash of the nickname
        /// </summary>
        public uint BaseID;

        /// <summary>
        ///     All possible NPC characters in this base
        /// </summary>
        public Dictionary<string, BaseCharacter> Chars = new Dictionary<string, BaseCharacter>();

        /// <summary>
        ///     The goods this base will sell. Any goods not on this list cannot be bought
        ///     by the player; unless they sell to the base in the dealer screen and immediately
        ///     buy the good back.
        /// </summary>
        public Dictionary<uint, float> GoodsForSale = new Dictionary<uint, float>();

        /// <summary>
        ///     The goods this base will buy. Any goods not on this list that the player
        ///     tries to sell to the base will be sold at the base price of the good.
        /// </summary>
        public Dictionary<uint, float> GoodsToBuy = new Dictionary<uint, float>();

        /// <summary>
        ///     The solars and locations where ships can possibly launch from for the
        ///     base.
        /// </summary>
        public List<DockingObject> LaunchObjs = new List<DockingObject>();

        /// <summary>
        ///     List of news items to show in base. This list is sent to the clients.
        /// </summary>
        public List<NewsItem> News = new List<NewsItem>();

        /// <summary>
        ///     The nickname for the base
        /// </summary>
        public string Nickname;

        /// <summary>
        ///     The rooms for the base. Surprise.
        /// </summary>
        public Dictionary<string, Room> Rooms = new Dictionary<string, Room>();

        /// <summary>
        ///     The nickname of the room the player should start in when
        ///     entering the base.
        /// </summary>
        public string StartRoom;

        /// <summary>
        ///     The nickname hash id of the room the player should start in when
        ///     entering the base.
        /// </summary>
        public uint StartRoomID;

        /// <summary>
        ///     The system the base is in.
        /// </summary>
        public uint SystemID;

        /// <summary>
        ///     Determines a valid hardpoint for launching from
        ///     the given base. Validity is determined by the
        ///     DockingPoint.DockingSphere and ShipArchetype.MissionProperty
        ///     enumerations.
        /// </summary>
        /// <param name="ship"></param>
        /// <returns></returns>
        public DockingObject GetLaunchPoint(FOS_ng.Objects.Ship.Ship ship)
        {
            // FIXME: select point based on ship moor type
            var validLaunchObjs = new List<DockingObject>();

            var mp = (ship.Arch as ShipArchetype).mission_property;

            var minType = Int32.MaxValue;
            foreach (DockingObject obj in LaunchObjs)
            {
                if (obj.CanDock(mp))
                {
                    if (obj.Type == DockingPoint.DockingSphere.RING)
                    {
                        if (obj.Index == 1)
                            return obj;
                    }
                    else if ((int)obj.Type <= minType)
                    {
                        if ((int)obj.Type < minType)
                        {
                            validLaunchObjs.Clear();
                            minType = (int)obj.Type;
                        }

                        validLaunchObjs.Add(obj);
                    }
                }
            }

            if (validLaunchObjs.Count == 0)
                return null;

            var selectedPoint = _rand.Next(validLaunchObjs.Count);
            return validLaunchObjs[selectedPoint];
        }
    }
}
