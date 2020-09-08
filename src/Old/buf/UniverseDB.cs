using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FLServer.DataWorkers;
using FLServer.Object.Base;
using FLServer.Object.Solar;
using FLServer.Ship;
using FLServer.Solar;
using FLServer.Physics;

namespace FLServer
{
    /// <summary>
    ///     This class stores static data for the universe. It might turn out that this is
    ///     a very bad idea.
    /// </summary>
    internal class UniverseDB
    {
        // FIXME: Find a better way of storing those and making them editable
        public const uint TradelaneSpeed = 2500;
        public const uint CruiseSpeed = 300;

        /// <summary>
        ///     Loadouts for NPCs
        /// </summary>
        private static readonly Dictionary<uint, Loadout> Loadouts = new Dictionary<uint, Loadout>();

        /// <summary>
        ///     The system list
        /// </summary>
        public static Dictionary<uint, StarSystem> Systems = new Dictionary<uint, StarSystem>();

        /// <summary>
        ///     The base list
        /// </summary>
        public static Dictionary<uint, BaseData> Bases = new Dictionary<uint, BaseData>();

        /// <summary>
        ///     The solar list
        /// </summary>
        public static Dictionary<uint, Object.Solar.Solar> Solars = new Dictionary<uint, Object.Solar.Solar>();

        /// <summary>
        ///     The faction list
        /// </summary>
        public static Dictionary<string, Faction> Factions = new Dictionary<string, Faction>();

        // Floyd-Warshall shortest path information
        protected static Dictionary<uint, Dictionary<uint, PathData>> MinimumDistances =
            new Dictionary<uint, Dictionary<uint, PathData>>();

        protected static Dictionary<uint, Dictionary<uint, PathData>> NextIndex =
            new Dictionary<uint, Dictionary<uint, PathData>>();

        /// <summary>
        ///     The list of goods that can be bought and sold
        /// </summary>
        public static Dictionary<uint, Good> Goods = new Dictionary<uint, Good>();

        public static void Load(string flPath, ILogController log)
        {
            // Load the universe and systems and all other static data
            string flIniPath = flPath + Path.DirectorySeparatorChar + "EXE" + Path.DirectorySeparatorChar +
                                 "Freelancer.ini";
            try
            {
                var flIni = new FLDataFile(flIniPath, true);
                string dataPath =
                    Path.GetFullPath(Path.Combine(flPath + Path.DirectorySeparatorChar + "EXE",
                        flIni.GetSetting("Freelancer", "data path").Str(0)));

                log.AddLog(LogType.GENERAL, "Loading loadouts");
                foreach (var entry in flIni.GetSettings("Data", "loadouts"))
                    LoadLoadout(dataPath + Path.DirectorySeparatorChar + entry.Str(0), log);
                log.AddLog(LogType.GENERAL, "Loading factions");
                foreach (var entry in flIni.GetSettings("Data", "groups"))
                    LoadFactions(dataPath + Path.DirectorySeparatorChar + entry.Str(0), log);
                log.AddLog(LogType.GENERAL, "Loading universe");
                foreach (var entry in flIni.GetSettings("Data", "universe"))
                    LoadUniverse(dataPath + Path.DirectorySeparatorChar + entry.Str(0), log);
                log.AddLog(LogType.GENERAL, "Loading goods");
                foreach (var entry in flIni.GetSettings("Data", "goods"))
                    LoadGoodData(dataPath + Path.DirectorySeparatorChar + entry.Str(0), log);
                log.AddLog(LogType.GENERAL, "Loading markets");
                foreach (var entry in flIni.GetSettings("Data", "markets"))
                    LoadBaseMarketData(dataPath + Path.DirectorySeparatorChar + entry.Str(0), log);
            }
            catch (Exception e)
            {
                log.AddLog(LogType.ERROR, "error: '" + e.Message + "' when parsing '" + flIniPath);
            }
        }

        private static void LoadLoadout(string path, ILogController log)
        {
            var ini = new FLDataFile(path, true);
            foreach (FLDataFile.Section sec in ini.Sections)
            {
                string sectionName = sec.SectionName.ToLowerInvariant();
                if (sectionName == "loadout")
                {
                    var loadout = new Loadout();
                    uint hpid = 34;
                    foreach (FLDataFile.Setting set in sec.Settings)
                    {
                        if (set.SettingName == "nickname")
                        {
                            loadout.Nickname = set.Str(0);
                            loadout.LoadoutID = FLUtility.CreateID(loadout.Nickname);
                        }
                        else if (set.SettingName == "equip")
                        {
                            var item = new ShipItem {arch = ArchetypeDB.Find(FLUtility.CreateID(set.Str(0)))};
                            if (item.arch == null)
                                continue; // TODO: log
                            item.hpname = "";
                            if (set.NumValues() > 1)
                                item.hpname = set.Str(1);
                            item.health = 1.0f;
                            item.mission = false;
                            item.mounted = true;
                            item.count = 1;
                            item.hpid = hpid++;
                            loadout.Items.Add(item);
                        }
                        else if (set.SettingName == "cargo")
                        {
                            var item = new ShipItem {arch = ArchetypeDB.Find(FLUtility.CreateID(set.Str(0)))};
                            if (item.arch == null)
                                continue; // TODO: log
                            item.hpname = "";
                            item.health = 1.0f;
                            item.mission = false;
                            item.mounted = false;
                            item.count = set.UInt(1);
                            item.hpid = hpid++;
                            loadout.Items.Add(item);
                        }

                    }
                    Loadouts[loadout.LoadoutID] = loadout;
                }
            }
        }

        public static Loadout FindLoadout(string nickname)
        {
            return FindLoadout(FLUtility.CreateID(nickname));
        }

        public static Loadout FindLoadout(uint loadoutid)
        {
            if (Loadouts.ContainsKey(loadoutid))
                return Loadouts[loadoutid];
            return null;
        }

        /// <summary>
        ///     Return the faction by its nickname or return null if not found.
        /// </summary>
        /// <param name="nickname"></param>
        /// <returns></returns>
        public static Faction FindFaction(string nickname)
        {
            if (Factions.ContainsKey(nickname))
                return Factions[nickname];
            return null;
        }

        // <summary>
        /// Load the factions from initial world.
        /// </summary>
        /// <param name="path"></param>
        private static void LoadFactions(string path, ILogController log)
        {
            var ini = new FLDataFile(path, true);
            foreach (FLDataFile.Section sec in ini.Sections)
            {
                string sectionName = sec.SectionName.ToLowerInvariant();
                if (sectionName == "group")
                {
                    var faction = new Faction {Nickname = sec.GetSetting("nickname").Str(0)};
                    faction.FactionID = FLUtility.CreateFactionID(faction.Nickname);
                    Factions[faction.Nickname] = faction;
                }
            }
        }

        public static Good FindGood(string nickname)
        {
            uint goodid = FLUtility.CreateID(nickname);
            if (!Goods.ContainsKey(goodid))
                return null;
            return Goods[goodid];
        }

        public static Good FindGood(uint goodid)
        {
            if (!Goods.ContainsKey(goodid))
                return null;
            return Goods[goodid];
        }

        public static Good FindGoodByArchetype(Archetype ship_arch)
        {
            foreach (Good good in Goods.Values)
            {
                if (good.EquipmentOrShipArch == ship_arch)
                {
                    return good;
                }
            }
            return null;
        }

        public static float GetPriceOfGood(BaseData basedata, uint goodid)
        {
            if (basedata.GoodsForSale.ContainsKey(goodid))
                return basedata.GoodsForSale[goodid];
            if (basedata.GoodsToBuy.ContainsKey(goodid))
                return basedata.GoodsToBuy[goodid];
            Good good = FindGood(goodid);
            if (good != null)
                return good.BasePrice;
            return 0.0f;
        }

        /// <summary>
        ///     Load shop information for equipment and commodities.
        /// </summary>
        /// <param name="path"></param>
        private static void LoadGoodData(string path, ILogController log)
        {
            var ini = new FLDataFile(path, true);
            foreach (FLDataFile.Section sec in ini.Sections)
            {
                var sectionName = sec.SectionName.ToLowerInvariant();
                if (sectionName != "good") continue;
                var good = new Good {Nickname = sec.GetSetting("nickname").Str(0)};
                good.GoodID = FLUtility.CreateID(good.Nickname);
                string category = sec.GetSetting("category").Str(0);
                if (category == "equipment")
                {
                    good.category = Good.Category.Equipment;
                    good.BasePrice = sec.GetSetting("price").Float(0);
                    uint archid = FLUtility.CreateID(sec.GetSetting("equipment").Str(0));
                    good.EquipmentOrShipArch = ArchetypeDB.Find(archid);
                }
                else if (category == "commodity")
                {
                    good.category = Good.Category.Commodity;
                    good.BasePrice = sec.GetSetting("price").Float(0);
                    uint archid = FLUtility.CreateID(sec.GetSetting("equipment").Str(0));
                    good.EquipmentOrShipArch = ArchetypeDB.Find(archid);
                }
                else if (category == "shiphull")
                {
                    good.category = Good.Category.ShipHull;
                    good.BasePrice = sec.GetSetting("price").Float(0);
                    uint archid = FLUtility.CreateID(sec.GetSetting("ship").Str(0));
                    good.EquipmentOrShipArch = ArchetypeDB.Find(archid);
                }
                else if (category == "ship")
                {
                    good.category = Good.Category.ShipPackage;
                    uint goodid = FLUtility.CreateID(sec.GetSetting("hull").Str(0));
                    good.Shiphull = Goods[goodid];
                }
                else
                    log.AddLog(LogType.ERROR, "error: unknown category " + sec.Desc);

                Goods[good.GoodID] = good;
            }
        }

        /// <summary>
        ///     Load base market data and setup the prices for goods at each base.
        /// </summary>
        /// <param name="path"></param>
        private static void LoadBaseMarketData(string path, ILogController log)
        {
            var ini = new FLDataFile(path, true);
            foreach (var sec in ini.Sections)
            {
                string sectionName = sec.SectionName.ToLowerInvariant();

                if (sectionName != "basegood") continue;

                var basedata = FindBase(sec.GetSetting("base").Str(0));
                if (basedata == null)
                {
                    log.AddLog(LogType.ERROR, "error: " + sec.Desc);
                    continue;
                }

                foreach (FLDataFile.Setting set in sec.Settings)
                {
                    var settingName = set.SettingName.ToLowerInvariant();
                    if (settingName != "marketgood") continue;
                    var nickname = set.Str(0);
                    var level_needed_to_buy = set.Float(1);
                    var reputation_needed_to_buy = set.Float(2);
                    var baseSells = (set.UInt(5) == 1);
                    var basePriceMultiplier = 1.0f;
                    if (set.NumValues() > 6)
                        basePriceMultiplier = set.Float(6);

                    var goodid = FLUtility.CreateID(nickname);
                    var good = FindGood(goodid);
                    if (good == null)
                    {
                        log.AddLog(LogType.ERROR, "error: " + set.Desc);
                        continue;
                    }

                    if (baseSells)
                        basedata.GoodsForSale[goodid] = good.BasePrice*basePriceMultiplier;

                    basedata.GoodsToBuy[goodid] = good.BasePrice*basePriceMultiplier;
                }
            }
        }

        /// <summary>
        ///     Load the universe
        /// </summary>
        /// <param name="path"></param>
        /// <param name="log"></param>
        private static void LoadUniverse(string path, ILogController log)
        {
            var universePath = Path.GetDirectoryName(path);
            var fldatapath = Directory.GetParent(universePath).FullName;

            var ini = new FLDataFile(path, true);
            foreach (FLDataFile.Section sec in ini.Sections)
            {
                string sectionName = sec.SectionName.ToLowerInvariant();
                if (sectionName == "system")
                {
                    var system = new StarSystem {Nickname = sec.GetSetting("nickname").Str(0)};
                    system.SystemID = FLUtility.CreateID(system.Nickname);
                    LoadSystem(universePath + Path.DirectorySeparatorChar + sec.GetSetting("file").Str(0), system, log);
                    Systems[system.SystemID] = system;
                }
                else if (sectionName == "base")
                {
                    var basedata = new BaseData {Nickname = sec.GetSetting("nickname").Str(0)};
                    basedata.BaseID = FLUtility.CreateID(basedata.Nickname);
                    basedata.SystemID = FLUtility.CreateID(sec.GetSetting("system").Str(0));
                    LoadBase(fldatapath, fldatapath + Path.DirectorySeparatorChar + sec.GetSetting("file").Str(0),
                        basedata, log);
                    Bases[basedata.BaseID] = basedata;
                }
            }

            // FIXME: Might want to dump this information to disk

            foreach (var syspair1 in Systems)
            {
                var systemMinDist = new Dictionary<uint, PathData>();
                var systemNext = new Dictionary<uint, PathData>();
                foreach (var syspair2 in Systems)
                {
                    systemMinDist.Add(syspair2.Key, syspair1.Key == syspair2.Key ? 0 : ((uint) Systems.Count + 1));
                    systemNext.Add(syspair2.Key, 0);
                }
                MinimumDistances.Add(syspair1.Key, systemMinDist);
                NextIndex.Add(syspair1.Key, systemNext);

                syspair1.Value.CalculatePathfinding();
            }

            foreach (var syspair in Systems)
            {
                foreach (Object.Solar.Solar s in syspair.Value.Gates)
                {
                    PathData pd = MinimumDistances[syspair.Key][s.DestinationSystemid];
                    if (s.Arch.Type == Archetype.ObjectType.JUMP_GATE)
                        pd.Shortest = pd.Legal = 1;
                    else
                        pd.Shortest = pd.Illegal = 1;
                    MinimumDistances[syspair.Key][s.DestinationSystemid] = pd;
                }
            }

            foreach (var k in Systems)
            {
                foreach (var i in Systems)
                {
                    foreach (var j in Systems)
                    {
                        for (int x = -1; x <= 1; x++)
                        {
                            uint testDistance = MinimumDistances[i.Key][k.Key][x] + MinimumDistances[k.Key][j.Key][x];
                            if (testDistance < MinimumDistances[i.Key][j.Key][x])
                            {
                                PathData min = MinimumDistances[i.Key][j.Key];
                                min[x] = testDistance;
                                MinimumDistances[i.Key][j.Key] = min;

                                PathData index = NextIndex[i.Key][j.Key];
                                index[x] = k.Key;
                                NextIndex[i.Key][j.Key] = index;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        ///     Load a single system
        /// </summary>
        /// <param name="path"></param>
        /// <param name="system"></param>
        /// <param name="log"></param>
        private static void LoadSystem(string path, StarSystem system, ILogController log)
        {
            try
            {
                var ini = new FLDataFile(path, true);
                foreach (FLDataFile.Section sec in ini.Sections)
                {
                    string sectionName = sec.SectionName.ToLowerInvariant();
                    if (sectionName == "zone")
                    {
                        var zone = new Zone {shape = null, nickname = sec.GetSetting("nickname").Str(0)};
                        zone.zoneid = FLUtility.CreateID(zone.nickname);

                        Vector position = sec.GetSetting("pos").Vector();
                        var orientation = new Matrix();

                        double[] size = null;

                        string shape = sec.GetSetting("shape").Str(0).ToLowerInvariant();

                        foreach (FLDataFile.Setting set in sec.Settings)
                        {
                            string settingName = set.SettingName.ToLowerInvariant();
                            switch (settingName)
                            {
                                case "rotation":
                                    orientation = Matrix.EulerDegToMatrix(set.Vector());
                                    break;
                                case "size":
                                    size = new double[set.NumValues()];
                                    for (int a = 0; a < size.Length; a++)
                                        size[a] = set.Float(a);
                                    break;
                                case "damage":
                                    zone.damage = set.Float(0);
                                    break;
                                case "interference":
                                    zone.interference = set.Float(0);
                                    break;
                                case "encounter":
                                    break;
                                case "faction":
                                    break;
                                case "density":
                                    zone.density = set.Float(0);
                                    break;
                            }
                        }

                        if (size != null)
                        {
                            if (shape == "sphere" && size.Length == 1)
                            {
                                zone.shape = new Sphere(position, orientation, size[0]);
                            }
                            else if (shape == "cylinder" && size.Length == 2)
                            {
                                zone.shape = new Cylinder(position, orientation, size[0], size[1]);
                            }
                            else if (shape == "ellipsoid" && size.Length == 3)
                            {
                                zone.shape = new Ellipsoid(position, orientation, new Vector(size[0], size[1], size[2]));
                            }
                            else if (shape == "box" && size.Length == 3)
                            {
                                zone.shape = new Box(position, orientation, new Vector(size[0], size[1], size[2]));
                            }
                            else if (shape == "ring" && size.Length == 3)
                            {
                                zone.shape = new Ring(position, orientation, size[0], size[1], size[2]);
                            }
                        }

                        system.Zones.Add(zone);
                    }
                    else if (sectionName == "object")
                    {
                        var solar = new Object.Solar.Solar(system, sec.GetSetting("nickname").Str(0));

                        if (sec.SettingExists("pos"))
                        {
                            solar.Position = sec.GetSetting("pos").Vector();
                        }

                        if (sec.SettingExists("rotate"))
                        {
                            Vector euler = sec.GetSetting("rotate").Vector();
                            solar.Orientation = Matrix.EulerDegToMatrix(euler);
                        }

                        if (sec.SettingExists("base"))
                        {
                            // When a ship undocks, it undocks from the solar specified by baseid.
                            // uint baseid = FLUtility.CreateID(sec.GetSetting("base").Str(0));
                            // FIXME: check base exists
                            // solar.base_data = bases[baseid];
                            // bases[baseid].solar = solar;
                        }

                        if (sec.SettingExists("archetype"))
                        {
                            uint archetypeid = FLUtility.CreateID(sec.GetSetting("archetype").Str(0));
                            solar.Arch = ArchetypeDB.Find(archetypeid);
                            solar.GetLoadout();
                            // FIXME: check archetype exists
                        }

                        if (sec.SettingExists("dock_with"))
                        {
                            uint baseid = FLUtility.CreateID(sec.GetSetting("dock_with").Str(0));
                            solar.BaseData = Bases[baseid];
                        }

                        if (sec.SettingExists("goto"))
                        {
                            solar.DestinationObjid = FLUtility.CreateID(sec.GetSetting("goto").Str(1));
                            solar.DestinationSystemid = FLUtility.CreateID(sec.GetSetting("goto").Str(0));
                        }

                        if (sec.SettingExists("prev_ring"))
                        {
                            solar.PrevRing = FLUtility.CreateID(sec.GetSetting("prev_ring").Str(0));
                        }

                        if (sec.SettingExists("next_ring"))
                        {
                            solar.NextRing = FLUtility.CreateID(sec.GetSetting("next_ring").Str(0));
                        }

                        if (sec.SettingExists("reputation"))
                        {
                            Faction faction = FindFaction(sec.GetSetting("reputation").Str(0));
                            if (faction == null)
                                log.AddLog(LogType.ERROR, "error: not valid faction={0}",
                                    sec.GetSetting("reputation").Str(0));
                            else
                                solar.Faction = faction;
                        }

                        // Rebuild the docking points from the archetype
                        // to the solar position and rotation.
                        foreach (DockingPoint dockingPoint in solar.Arch.DockingPoints)
                        {
                            var dockingObj = new DockingObject
                            {
                                Type = dockingPoint.Type,
                                Solar = solar,
                                Index = (uint) solar.Arch.DockingPoints.IndexOf(dockingPoint),
                                DockingRadius = dockingPoint.DockingRadius,
                                Position = solar.Orientation*dockingPoint.Position
                            };

                            // rotate the hardpoint by the base orientation and then
                            dockingObj.Position += solar.Position;

                            // the ship launch rotation is the base rotation rotated by the hardpoint rotation
                            dockingObj.Rotation = dockingPoint.Rotation*solar.Orientation;

                            if (solar.BaseData != null)
                                solar.BaseData.LaunchObjs.Add(dockingObj);

                            solar.DockingObjs.Add(dockingObj);
                        }


                        // Store the solar.
                        system.Solars[solar.Objid] = solar;
                        Solars[solar.Objid] = solar;

                        if (solar.Arch.Type == Archetype.ObjectType.JUMP_GATE ||
                            solar.Arch.Type == Archetype.ObjectType.JUMP_HOLE)
                            system.Gates.Add(solar);
                    }
                }
            }
            catch (Exception e)
            {
                log.AddLog(LogType.ERROR, "error: '" + e.Message + "' when parsing '" + path);
                if (e.InnerException != null)
                    log.AddLog(LogType.ERROR, "error: '" + e.InnerException.Message + "' when parsing '" + path);
            }
        }

        public static StarSystem FindSystem(string nickname)
        {
            uint systemid = FLUtility.CreateID(nickname);
            if (!Systems.ContainsKey(systemid))
                return null;
            return Systems[systemid];
        }

        public static StarSystem FindSystem(uint systemid)
        {
            if (!Systems.ContainsKey(systemid))
                return null;
            return Systems[systemid];
        }

        public static BaseData FindBase(string nickname)
        {
            uint baseid = FLUtility.CreateID(nickname);
            if (!Bases.ContainsKey(baseid))
                return null;
            return Bases[baseid];
        }

        public static BaseData FindBase(uint baseid)
        {
            if (!Bases.ContainsKey(baseid))
                return null;
            return Bases[baseid];
        }

        public static uint[] FindBestLegalPath(uint system1, uint system2)
        {
            return FindBestPath(system1, system2, 1);
        }

        public static uint[] FindBestIllegalPath(uint system1, uint system2)
        {
            return FindBestPath(system1, system2, -1);
        }

        public static uint[] FindBestAnyPath(uint system1, uint system2)
        {
            return FindBestPath(system1, system2, 0);
        }

        public static uint[] FindBestPath(uint system1, uint system2, int type)
        {
            if (MinimumDistances[system1][system2][type] == (uint) Systems.Count + 1)
                return null;

            var path = new List<uint>();

            ConstructBestPath(system1, system2, type, ref path);

            return path.Distinct().ToArray();
        }

        protected static void ConstructBestPath(uint system1, uint system2, int type, ref List<uint> path)
        {
            uint intermediate = NextIndex[system1][system2][type];

            if (intermediate == 0)
            {
                path.Add(system1);
                path.Add(system2);
                return;
            }

            ConstructBestPath(system1, intermediate, type, ref path);
            ConstructBestPath(intermediate, system2, type, ref path);
        }

        private static void LoadBase(string fldatapath, string path, BaseData basedata, ILogController log)
        {
            try
            {
                var ini = new FLDataFile(path, true);
                foreach (FLDataFile.Section sec in ini.Sections)
                {
                    string sectionName = sec.SectionName.ToLowerInvariant();
                    if (sectionName == "baseinfo")
                    {
                        basedata.StartRoom = String.Format("{0:x}_{1}", basedata.BaseID,
                            sec.GetSetting("start_room").Str(0));
                        basedata.StartRoomID = FLUtility.CreateID(basedata.StartRoom);
                    }
                    else if (sectionName == "room")
                    {
                        var room = new Room {Nickname = sec.GetSetting("nickname").Str(0).ToLowerInvariant()};
                        room.RoomID = FLUtility.CreateID(room.Nickname);
                        path = fldatapath + Path.DirectorySeparatorChar + sec.GetSetting("file").Str(0);
                        //I don't need room data at the moment. Yay.
                        //oldtodo: make LoadRoom?
                        //LoadRoom(fldatapath, path, basedata, room, log);
                        basedata.Rooms[room.Nickname] = room;
                    }
                }
            }
            catch (Exception e)
            {
                log.AddLog(LogType.ERROR, "error: '" + e.Message + "' when parsing '" + path);
            }
        }

        //oldtodo: <s>use LoadRoom</s>
        private static void LoadRoom(string fldatapath, string path, BaseData basedata, Room room, ILogController log)
        {
            try
            {
                var ini = new FLDataFile(path, true);
                foreach (var sec in ini.Sections)
                {
                    var sectionName = sec.SectionName.ToLowerInvariant();
                    if (sectionName != "room_info") continue;
                    if (!sec.SettingExists("set_script")) continue;
                    var setScript = sec.GetSetting("set_script").Str(0);
                    var thnText = File.ReadAllText(fldatapath + Path.DirectorySeparatorChar + setScript);

                    var thn = new ThnParse();
                    thn.Parse(thnText);
                    foreach (var e in thn.entities.Where(e => e.type.ToLowerInvariant() == "marker"))
                    {
                        //oldtodo: so what
                    }
                }
            }
            catch (Exception e)
            {
                log.AddLog(LogType.ERROR, "error: '" + e.Message + "' when parsing '" + path);
            }
        }


        /// <summary>
        ///     This may need to be moved into the system thread as it has some non-static
        ///     fields
        /// </summary>
        /// <param name="objid"></param>
        /// <returns></returns>
        public static Object.Solar.Solar FindSolar(uint objid)
        {
            if (!Solars.ContainsKey(objid))
                return null;
            if (Solars[objid] is Object.Solar.Solar)
                return Solars[objid];
            return null;
        }

        protected struct PathData
        {
            public uint Illegal;
            public uint Legal;
            public uint Shortest;

            public PathData(uint s, uint l, uint i)
            {
                Shortest = s;
                Legal = l;
                Illegal = i;
            }

            public uint this[int index]
            {
                get
                {
                    if (index < 0) return Illegal;
                    if (index > 0) return Legal;
                    return Shortest;
                }
                set
                {
                    if (index < 0) Illegal = value;
                    else if (index > 0) Legal = value;
                    else Shortest = value;
                }
            }

            public static implicit operator PathData(uint value)
            {
                return new PathData(value, value, value);
            }
        }
    }

    public class Loadout
    {
        public List<ShipItem> Items = new List<ShipItem>();
        public uint LoadoutID;
        public string Nickname;
    }
}