using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FLDataFile;
using FLServer.DataWorkers;
using FLServer.GameDB.Base;
using NLog;

namespace FLServer.GameDB
{
	static class BaseDB
	{
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
		public class UniGood
		{

			public enum Cat
			{
				ShipHull,
				ShipPackage,
				Commodity,
				Equipment
			}

			public Cat Category;
			public float BasePrice;
			public bool Combinable;
			public uint ArchID;
			//TODO: make arch getter
		}

		private static readonly Dictionary<uint, Base.Base> Bases = new Dictionary<uint, Base.Base>();

		/// <summary>
		/// Script segments for NPCs
		/// </summary>
		private static readonly Dictionary<string, List<SetSegmentScripts>> Segments =
			new Dictionary<string, List<SetSegmentScripts>>();

		/// <summary>
		/// This one stores info about all the goods in universe with their base prices.
		/// </summary>
		public static readonly Dictionary<uint, UniGood> UniGoods = new Dictionary<uint, UniGood>();

		/// <summary>
		/// Get all the [Base] sections here so we can load the base info.
		/// </summary>
		/// <param name="secs">List of [Base] sections.</param>
		/// <param name="flDataPath">Path to FL's DATA folder.</param>
		public static void LoadBases(IEnumerable<Section> secs,string flDataPath)
		{

			Parallel.ForEach(secs, sec =>
			{
				var path = Path.Combine(flDataPath, sec.GetFirstOf("file")[0]);
				var bdata = new Base.Base(new DataFile(path), flDataPath);
				Bases[FLUtility.CreateID(bdata.Nickname)] = bdata;

			});
		}

		/// <summary>
		/// Load goods and commodities that are sold throughout the universe.
		/// </summary>
		/// <param name="file"></param>
		public static void LoadGoods(DataFile file)
		{
			foreach (var sec in file.GetSections("Good"))
			{
				var good = new UniGood();
				var ghash = FLUtility.CreateID(sec.GetFirstOf("nickname")[0]);

				switch (sec.GetFirstOf("category")[0])
				{
					case "equipment":
						good.Category = UniGood.Cat.Equipment;
						good.BasePrice = float.Parse(sec.GetFirstOf("price")[0]);
						good.ArchID = FLUtility.CreateID(sec.GetFirstOf("equipment")[0]);
						break;
					case "commodity":
						good.Category = UniGood.Cat.Commodity;
						good.BasePrice = float.Parse(sec.GetFirstOf("price")[0]);
						good.ArchID = FLUtility.CreateID(sec.GetFirstOf("equipment")[0]);
						break;
					case "shiphull":
						good.Category = UniGood.Cat.ShipHull;
						good.BasePrice = float.Parse(sec.GetFirstOf("price")[0]);
						good.ArchID = FLUtility.CreateID(sec.GetFirstOf("ship")[0]);
						break;
					case "ship":
						good.Category = UniGood.Cat.ShipPackage;
						good.ArchID = FLUtility.CreateID(sec.GetFirstOf("hull")[0]);
						break;
					default:
						Logger.Warn("Unknown good type for {0}: {1}", sec.GetFirstOf("nickname")[0], sec.GetFirstOf("category")[0]);
						break;
				}
				UniGoods[ghash] = good;
			}
		}

		/// <summary>
		/// Load goods list for every base: buy\sell list, price mods, etc.
		/// </summary>
		/// <param name="file"></param>
		public static void LoadBaseMarketData(DataFile file)
		{
			foreach (var basesect in file.GetSections("BaseGood"))
			{
				var sbase = GetBase(basesect.GetFirstOf("base")[0]);
				if (sbase == null)
				{
					Logger.Warn("Market info for unknown base skipped: {0}", basesect.GetFirstOf("base")[0]);
					continue;
				}

				foreach (var set in basesect.GetSettings("MarketGood"))
				{
					var hash = FLUtility.CreateID(set[0]);
					var good = new Base.Base.MarketGood(hash)
					{
						MinLevelToBuy = float.Parse(set[1]),
						MinRepToBuy = float.Parse(set[2]),
						
					};
					var baseSells = (set[5] == "1");
					if (set.Count >= 7)
					{
						good.PriceMod = float.Parse(set[6]);
					}

					sbase.GoodsToBuy[hash] = good;
					if (baseSells)
						sbase.GoodsForSale[hash] = good;
				}

			}
		}

		public static void LoadNews(DataFile file)
		{
			foreach (var sec in file.GetSections("NewsItem"))
			{
				var item = new Base.Base.NewsItem();
				if (!sec.ContainsAnyOf("headLine","headline"))
					continue;

				item.Category = uint.Parse(sec.GetFirstOf("category")[0]);

				var hl = sec.GetFirstOf("headLine") ?? sec.GetFirstOf("headline");

				item.Headline = uint.Parse(hl[0]);
				item.Text = uint.Parse(sec.GetFirstOf("text")[0]);

				item.Audio = sec.ContainsAnyOf("audio") && bool.Parse(sec.GetFirstOf("audio")[0]);

				item.Logo = sec.GetFirstOf("logo")[0];

				switch (sec.GetFirstOf("icon")[0])
				{
					case "critical":
						item.Icon = 1;
						break;
					case "world":
						item.Icon = 2;
						break;
					case "mission":
						item.Icon = 3;
						break;
					case "system":
						item.Icon = 4;
						break;
					case "faction":
						item.Icon = 5;
						break;
					case "universe":
						item.Icon = 6;
						break;
					default:
						item.Icon = 0;
						break;
				}

				foreach (var bname in sec.GetSettings("base"))
				{
					var bclass = GetBase(bname[0]);
					if (bclass == null)
					{
						Logger.Warn("LOAD News entry for unknown base: {0} text {1}",bname[0],item.Text);
						continue;
					}
					bclass.News.Add(item);
				}

			}
		}

		public static void LoadMBase(DataFile file)
		{
			Base.Base bd = null;
			//That's lame but the mbases retarded file format gives us no chance
			foreach (var sec in file.Sections)
			{
				
				switch (sec.Name.ToLowerInvariant())
				{
					case "mbase":
						bd = GetBase(sec.GetFirstOf("nickname")[0]);
						if (bd == null)
							Logger.Warn("LOAD Unknown base in MBases: {0}", sec.GetFirstOf("nickname")[0]);
						break;
					case "mvendor":
						break;
					case "basefaction":
						break;
					case "gf_npc":
						if (bd == null) continue;

						var bc = new Character
						{
							Nickname = sec.GetFirstOf("nickname")[0]
						};

						

						Setting set;
						if (sec.TryGetFirstOf("base_appr", out set))
						{
					//TODO: get from base_appr
						}
						else
						{
							bc.Body = FLUtility.CreateID(sec.GetFirstOf("body")[0]);
							bc.Head = FLUtility.CreateID(sec.GetFirstOf("head")[0]);
							bc.Lefthand = FLUtility.CreateID(sec.GetFirstOf("lefthand")[0]);
							bc.Righthand = FLUtility.CreateID(sec.GetFirstOf("righthand")[0]);
						}
						bc.Voice = FLUtility.CreateID(sec.GetFirstOf("voice")[0]);
						bc.IndividualName = FLUtility.CreateID(sec.GetFirstOf("individual_name")[0]);

						bc.Faction = sec.GetFirstOf("affiliation")[0];

						if (sec.TryGetFirstOf("room", out set))
						{
							bc.Room = String.Format("{0:x}_{1}", bd.BaseID, set[0].ToLowerInvariant());
						//bc.RoomID = Utilities.CreateID(bc.Room);
						}

						foreach (var bb in sec.GetSettings("bribe").Select(bset => new CharBribe
						{
							Faction = bset[0],
							Cost = uint.Parse(bset[1]),
							Text = uint.Parse(bset[2])
						}))
						{
							bc.Bribes.Add(bb);
						}

						//TODO: unused fields in rumor
						foreach (var br in sec.GetSettings("rumor").
							Select(rset => new BaseRumor { Text = uint.Parse(rset[3]) }))
						{
							bc.Rumors.Add(br);
						}

						bd.Chars[bc.Nickname] = bc;
						break;
					case "mroom":
						if (bd == null) continue;
						var rnick = sec.GetFirstOf("nickname")[0].ToLowerInvariant();

						Setting tSetting;

						if (sec.TryGetFirstOf("character_density",out tSetting))
						{
							if (!bd.Rooms.ContainsKey(rnick))
							{
								Logger.Warn("LOAD MRoom definition for nonexistent room {0} in {1}",rnick,bd.Nickname);
								continue;
							}

							bd.Rooms[rnick].CharacterDensity = uint.Parse(tSetting[0]);
						}

						if (sec.TryGetFirstOf("fixture", out tSetting))
						{
							//TODO: all tolower?
							string name = tSetting[0];
							string roomLocation = tSetting[1];
							string fidget_script = tSetting[2];
							string type = tSetting[3];

							if (!bd.Chars.ContainsKey(name))
							{
								//log.AddLog(LogType.ERROR, "character not found at {0}", set.Desc);
								Logger.Warn("LOAD Fixture for unknown character in MBases: {0} {1}",name,bd.Nickname);
								continue;
							}

							bd.Chars[name].Room = String.Format("{0:x}_{1}", bd.BaseID, rnick);
						//bd.Chars[name].RoomID = Utilities.CreateID(bd.Chars[name].Room);
							bd.Chars[name].RoomLocation = roomLocation;
							bd.Chars[name].FidgetScript = fidget_script;
							bd.Chars[name].Type = type;
						}

						break;

				}
			}
		}

		public static void LoadGenericScripts(DataFile ini)
		{
			foreach (var sec in ini.Sections)
			{
				string currentSetSegment = "";
				bool currentSetGender = false;
				bool currentSetPosture = false;
				foreach (var set in sec.Settings)
				{
					if (set.Name == "set_segment")
					{
						currentSetSegment = set[0];
					}
					else if (set.Name == "set_gender")
					{
						string gender = set[0];
						if (gender == "male")
							currentSetGender = true;
						else if (gender == "female")
							currentSetGender = false;
						else throw new Exception("invalid set_gender");
					}
					else if (set.Name == "set_posture")
					{
						string posture = set[0];
						if (posture == "stand")
							currentSetPosture = true;
						else if (posture == "sitlow")
							currentSetPosture = false;
						else throw new Exception("invalid set_posture");
					}
					else if (set.Name == "script")
					{
						var setSegment = GetSetSegment(currentSetSegment, currentSetGender,
							currentSetPosture);
						string script = set[0];
						setSegment.Scripts.Add(script);
					}
				}
			}
		}


		private static int _index;
		public static List<string> GetScriptsForNPCInteraction(Character npc)
		{
			if (npc.Type == "trader" || npc.Type == "equipment" || npc.Type == "shipdealer")
				return GetSetSegment("c01ar", npc.Gender, npc.Posture).Scripts;

			if (_index >= Segments.Count)
				_index = 0;

			List<SetSegmentScripts> setl = Segments.ElementAt(_index++).Value;
			foreach (SetSegmentScripts set in setl)
			{
				if (set.Gender && set.Posture)
				{
					Console.WriteLine(set.SetSegment);
					var scripts = new List<string>();
					scripts.AddRange(set.Scripts);
					return scripts;
				}
			}
			return null;
		}

		private static SetSegmentScripts GetSetSegment(string segment, bool gender, bool posture)
		{
			segment = segment.ToLowerInvariant();

			if (!Segments.ContainsKey(segment))
				Segments.Add(segment, new List<SetSegmentScripts>());

			foreach (var set in Segments[segment].Where(set => set.Gender == gender && set.Posture == posture))
				return set;

			var newSet = new SetSegmentScripts { SetSegment = segment, Gender = gender, Posture = posture };
			Segments[segment].Add(newSet);
			return newSet;
		}


		public static Base.Base GetBase(uint baseid)
		{
			if (!Bases.ContainsKey(baseid)) return null;

			return Bases[baseid];
		}

		public static Base.Base GetBase(string nickname)
		{
			return GetBase(FLUtility.CreateID(nickname));
		}

		public static int BaseCount
		{ get { return Bases.Count; } }

	}
}
