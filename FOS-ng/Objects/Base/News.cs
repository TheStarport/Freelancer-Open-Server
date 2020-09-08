using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FLServer.Object.Base
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


    public class BaseBribe
    {
        public uint Cost;
        public Faction Faction;
        public uint Text;
    }

    public class BaseRumor
    {
        public uint Text;
    }

    public class BaseFaction
    {
        public List<BaseCharacter> Characters = new List<BaseCharacter>();
        public Faction Faction;
        public bool OffersMissions;
        public float Weight;
    }

    internal class News
    {
        private static readonly Dictionary<string, List<SetSegmentScripts>> Segments =
            new Dictionary<string, List<SetSegmentScripts>>();


        private static int _index;

        public static void Load(string flPath, ILogController log)
        {
            string flIniPath = flPath + Path.DirectorySeparatorChar + "EXE" + Path.DirectorySeparatorChar +
                                 "Freelancer.ini";
            try
            {
                var flIni = new FLDataFile(flIniPath, true);
                var dataPath =
                    Path.GetFullPath(Path.Combine(flPath + Path.DirectorySeparatorChar + "EXE",
                        flIni.GetSetting("Freelancer", "data path").Str(0)));
                LoadMBaseFile(new FLDataFile(dataPath + "\\missions\\mbases.ini", true), log);
                LoadNewsFile(new FLDataFile(dataPath + "\\missions\\news.ini", true), log);
                LoadGenericScripts(new FLDataFile(dataPath + "\\scripts\\gcs\\genericscripts.ini", true), log);
            }
            catch (Exception e)
            {
                log.AddLog(LogType.ERROR, "error: '" + e.Message + "' when parsing '" + flIniPath);
            }
        }


        //W01cG -- Gen: welcome
        //W02aF -- Fac: welcome to base, faction
        //W03bB -- Bar: a drink?
        //W04bP -- Player: ask for info?
        //W04cP -- Player: ask for info?
        //A01aG -- ask for marker
        //A02aG -- nothing happening, come back
        //A02cG -- bad rep, go away
        //A02dG -- good rep
        //A02eB -- nothing happening
        //A03aG -- the <faction> don't
        //A04aG -- follow up from A03aG. Hate
        //D01aN -- first time here?
        //D01bG -- you suck after the last time
        //D01cG -- you rock after the last time
        //D01dG -- mission aborted
        //D01fG -- you're back, why?
        //D01fGm -- you're back, why?
        //D01hG -- we've spoken before
        //D01kG -- welcome back, mission succeeded
        //D01kGm -- welcome back
        //D02aP -- yeah
        //D02aPm
        //D02bP fail mission, ask for another one
        //D02dP finish mission, ask for another one
        //D02eP yeah, i'm trent
        //D03aN i remember you, good to see you
        //D04aG i could have a match for you, mr ...
        //D04bB well you've come to the right man
        //D05aP 
        //D05bP thanks, any action going on?
        //D06aG i see, i work the <faction>
        //D07aG we run this base
        //D07bG we may not own this place but we do have a sizable stack in its operation
        //F01aB well there's a rumor going around, like to hear it?
        //F01bB well i'll tell you, some new information did surface today
        //F01dG just between us mate, i have heard a rumor.
        //F01eG well it is possible i may have some information you can use
        //F01fG your rep will get you wacked, i can fix it for a fee.
        //F01jG i might have a proposition for you
        //F02aP well yes, i'm interested
        //F03bR it's not something you'd really find in the public record
        //F03cR it's a secret my friend
        //F03dR it's very secret
        //F04aG mission rejected - but you don't even have jump gate access to that system yet
        //F04dG mission rejected - you'll need more cargo space for this mission.
        //F04fG mission rejected - you're already on a mission
        //F04gP mission - is it dangerous?
        //F05aG mission - don't worry about it, you'll be just fine
        //F05bG mission - it could be challenging but don't worry about it
        //F05cG mission - well, yeah, it's a bugger
        //F06aP mission - okay, give me the details
        //F07aP mission - i'll take it
        //F07bP rumor reject - that's way too expensive
        //F07cP rumor reject - sorry no deal
        //F08bR rumor accept - okay, this is what i heard
        //F08dR rumor reject - player doesn't have enough money
        //F09bP ??
        //F10aR bye
        //F10bR bye
        //F10bRm bye
        //C01aR ??
        /// <summary>
        ///     W01bB -- Bar: welcome
        /// </summary>
        /// <param name="player"></param>
        /// <param name="npc"></param>
        /// <returns></returns>
        public static List<string> GetScriptsForNPCInteraction(Player.Player player, BaseCharacter npc)
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

            var newSet = new SetSegmentScripts {SetSegment = segment, Gender = gender, Posture = posture};
            Segments[segment].Add(newSet);
            return newSet;
        }

        private static void LoadGenericScripts(FLDataFile ini, ILogController log)
        {
            foreach (FLDataFile.Section sec in ini.Sections)
            {
                string currentSetSegment = "";
                bool currentSetGender = false;
                bool currentSetPosture = false;
                foreach (var set in sec.Settings)
                {
                    if (set.SettingName == "set_segment")
                    {
                        currentSetSegment = set.Str(0).ToLowerInvariant();
                    }
                    else if (set.SettingName == "set_gender")
                    {
                        string gender = set.Str(0).ToLowerInvariant();
                        if (gender == "male")
                            currentSetGender = true;
                        else if (gender == "female")
                            currentSetGender = false;
                        else throw new Exception("invalid set_gender");
                    }
                    else if (set.SettingName == "set_posture")
                    {
                        string posture = set.Str(0).ToLowerInvariant();
                        if (posture == "stand")
                            currentSetPosture = true;
                        else if (posture == "sitlow")
                            currentSetPosture = false;
                        else throw new Exception("invalid set_posture");
                    }
                    else if (set.SettingName == "script")
                    {
                        var setSegment = GetSetSegment(currentSetSegment, currentSetGender,
                            currentSetPosture);
                        string script = set.Str(0).ToLowerInvariant();
                        setSegment.Scripts.Add(script);
                    }
                }
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="ini"></param>
        /// <param name="log"></param>
        /// First value is "minimum" mission difficulty. Second value is "maximum" mission difficulty. 
        /// The server uses a funky formula for that, but preferably (and I've requested a hack applied
        /// to FLServer) it should just use those bare values. Third value is only used in the faction 
        /// headers, and is the percentage chance for a mission for that faction to appear - the total 
        /// of all mission lines of all factions for any given base should be 100.
        /// The difficulty then determines the payout and the various ships and solars that may appear.
        /// - Get as close to the max value of the difficulty by adding ships and solars in the waves 
        /// in that mission, using the NPCRankToDiff.ini and MissionSolars.ini file for guidance
        /// - Then calculate the actual difficulty of the mission, and use the Diff2Money.ini file 
        /// for calculating the mission payout. Then post the mission.
        private static void LoadMBaseFile(FLDataFile ini, ILogController log)
        {
            BaseData bd = null;

            foreach (var sec in ini.Sections)
            {
                string sectionName = sec.SectionName.ToLowerInvariant();
                if (sectionName == "mbase")
                {
                    string nickname = sec.GetSetting("nickname").Str(0);
                    bd = UniverseDB.FindBase(nickname);
                }
                else if (sectionName == "mvendor")
                {
                }
                else if (sectionName == "basefaction")
                {
                    var bf = new BaseFaction
                    {
                        Faction = UniverseDB.FindFaction(sec.GetSetting("faction").Str(0)),
                        Weight = sec.GetSetting("weight").Float(0),
                        OffersMissions = sec.SettingExists("offers_missions")
                    };

                    foreach (FLDataFile.Setting set in sec.Settings)
                    {
                        if (set.SettingName == "mission_type")
                        {
                            string mission_type = set.Str(0);
                        }
                        else if (set.SettingName == "npc")
                        {
                            string npcname = set.Str(0);
                        }
                    }
                }
                else if (sectionName == "gf_npc")
                {
                    var bc = new BaseCharacter {Nickname = sec.GetSetting("nickname").Str(0).ToLowerInvariant()};

                    if (sec.SettingExists("base_appr"))
                    {
                        // fixme
                        // var body;
                        // var head;
                        // var lefthead;
                        // var righthand;
                    }
                    else
                    {
                        bc.Body = Utilities.CreateID(sec.GetSetting("body").Str(0));
                        bc.Head = Utilities.CreateID(sec.GetSetting("head").Str(0));
                        bc.Lefthand = Utilities.CreateID(sec.GetSetting("lefthand").Str(0));
                        bc.Righthand = Utilities.CreateID(sec.GetSetting("righthand").Str(0));
                    }

                    bc.IndividualName = sec.GetSetting("individual_name").UInt(0);
                    bc.Faction = UniverseDB.FindFaction(sec.GetSetting("affiliation").Str(0));
                    bc.Voice = Utilities.CreateID(sec.GetSetting("voice").Str(0));
                    if (sec.SettingExists("room"))
                    {
                        bc.Room = String.Format("{0:x}_{1}", bd.BaseID, sec.GetSetting("room").Str(0));
                        bc.RoomID = Utilities.CreateID(bc.Room);
                    }

                    foreach (var set in sec.Settings)
                    {
                        if (set.SettingName == "bribe")
                        {
                            var bb = new BaseBribe
                            {
                                Faction = UniverseDB.FindFaction(set.Str(0)),
                                Cost = set.UInt(1),
                                Text = set.UInt(2)
                            };
                            bc.Bribes.Add(bb);
                        }
                        else if (set.SettingName == "rumor")
                        {
                            var br = new BaseRumor {Text = set.UInt(3)};
                            bc.Rumors.Add(br);
                        }
                    }

                    if (bd != null) bd.Chars[bc.Nickname] = bc; else log.AddLog(LogType.ERROR,"Character {0} can't find base",bc.Nickname);
                }
                else if (sectionName == "mroom")
                {
                    string nickname = sec.GetSetting("nickname").Str(0).ToLowerInvariant();
                    if (sec.SettingExists("character_density"))
                    {
                        bd.Rooms[nickname].CharacterDensity = sec.GetSetting("character_density").UInt(0);
                    }

                    foreach (FLDataFile.Setting set in sec.Settings)
                    {
                        if (set.SettingName == "fixture")
                        {
                            string name = set.Str(0).ToLowerInvariant();
                            string roomLocation = set.Str(1);
                            string fidget_script = set.Str(2);
                            string type = set.Str(3).ToLowerInvariant();

                            if (!bd.Chars.ContainsKey(name))
                            {
                                log.AddLog(LogType.ERROR, "character not found at {0}", set.Desc);
                                continue;
                            }

                            bd.Chars[name].Room = String.Format("{0:x}_{1}", bd.BaseID, nickname);
                            bd.Chars[name].RoomID = Utilities.CreateID(bd.Chars[name].Room);
                            bd.Chars[name].RoomLocation = roomLocation;
                            bd.Chars[name].FidgetScript = fidget_script;
                            bd.Chars[name].Type = type;
                        }
                    }
                }
            }
        }

        private static void LoadNewsFile(FLDataFile ini, ILogController log)
        {
            foreach (FLDataFile.Section sec in ini.Sections)
            {
                string section_name = sec.SectionName.ToLowerInvariant();
                if (section_name == "newsitem")
                {
                    var item = new NewsItem
                    {
                        Category = sec.GetSetting("category").UInt(0),
                        Headline = sec.GetSetting("headline").UInt(0)
                    };

                    var icon = sec.GetSetting("icon").Str(0);
                    switch (icon)
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

                    item.Text = sec.GetSetting("text").UInt(0);
                    item.Audio = sec.SettingExists("audio");
                    item.Logo = sec.GetSetting("logo").Str(0);
                    foreach (var set in sec.Settings)
                    {
                        if (set.SettingName != "base") continue;
                        var basename = set.Str(0);

                        var bd = UniverseDB.FindBase(basename);
                        if (bd == null)
                            log.AddLog(LogType.ERROR, "basename in news item not found, category={0} base={1}",
                                item.Category, basename);
                        else
                            bd.News.Add(item);
                    }
                }
            }
        }

        private class SetSegmentScripts
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
    }
}