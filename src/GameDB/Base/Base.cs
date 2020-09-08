using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FLDataFile;
using FLServer.DataWorkers;
using NLog;

namespace FLServer.GameDB.Base
{
    /// <summary>
    /// This class provides data about insides of the base, heh.
    /// Dealers, NPCs, that sort of thing.
    /// </summary>
    class Base
    {



        public class NewsItem
        {
            public bool Audio;
            public uint Category;
            public uint Headline;
            public uint Icon;
            public string Logo;
            public uint Text;
        }

        public class MarketGood
        {


            private readonly uint _baseGoodID;
            private BaseDB.UniGood _baseGood;

            public float PriceMod = 1f;
            public float MinLevelToBuy;
            public float MinRepToBuy;

            public MarketGood(uint id)
            {
                _baseGoodID = id;
            }

            public BaseDB.UniGood UniGood
            {
                get
                {
                    if (_baseGood != null) return _baseGood;
                    if (!BaseDB.UniGoods.ContainsKey(_baseGoodID)) return null;
                    _baseGood = BaseDB.UniGoods[_baseGoodID];
                    return _baseGood;
                }
            }

        }

        public string Nickname;

        public uint BaseID;

        /// <summary>
        ///     All possible NPC characters in this base
        /// </summary>
        public Dictionary<string, Character> Chars = new Dictionary<string, Character>();

        /// <summary>
        ///     The goods this base will sell. Any goods not on this list cannot be bought
        ///     by the player; unless they sell to the base in the dealer screen and immediately
        ///     buy the good back.
        /// </summary>
        public Dictionary<uint, MarketGood> GoodsForSale = new Dictionary<uint, MarketGood>();

        /// <summary>
        ///     The goods this base will buy. Any goods not on this list that the player
        ///     tries to sell to the base will be sold at the base price of the good.
        /// </summary>
        public Dictionary<uint, MarketGood> GoodsToBuy = new Dictionary<uint, MarketGood>();


        /// <summary>
        ///     List of news items to show in base. This list is sent to the clients.
        /// </summary>
        public List<NewsItem> News = new List<NewsItem>();


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

        public Base(DataFile datafile, string flDataPath)
        {
            Nickname = datafile.GetSetting("BaseInfo", "nickname")[0];
            BaseID = FLUtility.CreateID(Nickname);
            StartRoom = String.Format("{0:x}_{1}", BaseID,
                datafile.GetSetting("BaseInfo", "start_room")[0].ToLowerInvariant());
            StartRoomID = FLUtility.CreateID(StartRoom);

            foreach (var sec in datafile.GetSections("Room"))
            {
                var nick = sec.GetFirstOf("nickname")[0].ToLowerInvariant();
                var file = new DataFile(Path.Combine(flDataPath, sec.GetFirstOf("file")[0]));
                var room = new Room(nick, file,flDataPath);
                Rooms.Add(nick,room);
            }
        }

    }


    class Room
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

        public Room(string nickname, DataFile file,string flDataPath)
        {
            Nickname = nickname;
            RoomID = FLUtility.CreateID(Nickname);
            //TODO: base room loading!
            //THN parsing is ass slow, disabled for now.
            return;

#pragma warning disable 162
// ReSharper disable HeuristicUnreachableCode
            Setting tset;

            var sec = file.Sections.FirstOrDefault(sect => sect.Name == "Room_Info");
            if (sec == null)
            {
                var log = LogManager.GetCurrentClassLogger();
                log.Warn("Base room without Room_Info: {1} in {0}",file.Path,nickname);
                return;
            }
            if (!sec.TryGetFirstOf("set_script", out tset)) return;

            var thnPath = tset[0];
            var thnText = File.ReadAllText(Path.Combine(flDataPath, thnPath));

            var thn = new ThnParse();
            thn.Parse(thnText);
            foreach (var e in thn.entities.Where(e => e.type.ToLowerInvariant() == "marker"))
            {
                //TODO: do something with base thns
            }
// ReSharper restore HeuristicUnreachableCode
#pragma warning restore 162
        }
    }
}
