using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FLServer.DataWorkers;
using FLServer.Physics;
using FLServer.Player.PlayerExtensions;
using FLServer.Ship;
using ProtoBuf;

namespace FLServer.Old.CharacterDB
{
    

    public class CharacterData
    {

        #region "Variables"


        /// <summary>
        /// Player's name.
        /// </summary>
        public string Name;

        public Int32 Money;


        /// <summary>
        ///     Player's weapon groups.
        /// </summary>
        public WeaponGroup Wgrp;

        /// <summary>
        ///     The player's ship.
        /// </summary>
        public Old.Object.Ship.Ship Ship;

        /// <summary>
        /// WARNING use this.Settings instead!
        /// </summary>
        private Dictionary<string, bool> _settings;

        public Account PlayerAccount;

        /// <summary>
        ///     The number of kills by type.
        /// TODO: actually put kills in use.
        /// </summary>
        public Dictionary<uint, uint> Kills = new Dictionary<uint, uint>();

        /// <summary>
        /// WARNING use this.ShipState instead!
        /// </summary>
        private AccountStruct.ShipState _shipstate;

        /// <summary>
        /// WARNING use this.Appearance instead!
        /// </summary>
        private AccountStruct.Appearance _appearance;

        /// <summary>
        /// WARNING use this.EqList instead!
        /// </summary>
        private List<AccountStruct.Equipment> _eqList;

        /// <summary>
        /// WARNING use this.CargoList instead!
        /// </summary>
        private List<AccountStruct.Cargo> _cargoList;

        /// <summary>
        /// WARNING use this.RepList instead!
        /// </summary>
        private Dictionary<string,float> _repList;

        /// <summary>
        /// WARNING use this.Visits instead!
        /// </summary>
        private Dictionary<uint,uint> _visits;

        #endregion

        #region "Public props"

        public Dictionary<string, float> RepDictionary
        {
            get
            {
                if (_repList != null)
                    return _repList;
                _repList = new Dictionary<string,float>();
                using (var str = new MemoryStream(Convert.FromBase64String(PlayerAccount.RepList)))
                    _repList = Serializer.Deserialize<Dictionary<string, float>>(str);
                return _repList;
            }
        }

        /// <summary>
        /// Map of objects seen by player.
        /// TODO: add visited state on dock.
        /// </summary>
        public Dictionary<uint,uint> Visits
        {
            get
            {
                if (_visits != null)
                    return _visits;
                _visits = new Dictionary<uint,uint>();
                using (var str = new MemoryStream(Convert.FromBase64String(PlayerAccount.Visits)))
                    _visits = Serializer.Deserialize<Dictionary<uint, uint>>(str);  

                    
                return _visits;
            }
        }


        public Dictionary<string, bool> Settings
        {
            get
            {
                if (_settings != null)
                    return _settings;
                _settings = new Dictionary<string, bool>();
                using (var str = new MemoryStream(Convert.FromBase64String(PlayerAccount.Settings)))
                    _settings = Serializer.Deserialize<Dictionary<string, bool>>(str);
                return _settings;
            }
        }

        public AccountStruct.ShipState ShipState
        {
            get
            {
                if (_shipstate != null)
                    return _shipstate;
                using (var str = new MemoryStream(Convert.FromBase64String(PlayerAccount.ShipState)))
                    _shipstate = Serializer.Deserialize<AccountStruct.ShipState>(str);
                return _shipstate;
            }
            set { _shipstate = value; }
        }

        public AccountStruct.Appearance Appearance
        {
            get
            {
                if (_appearance != null)
                    return _appearance;
                using (var str = new MemoryStream(Convert.FromBase64String(PlayerAccount.Appearance)))
                    _appearance = Serializer.Deserialize<AccountStruct.Appearance>(str);
                return _appearance;
            }
        }

        public List<AccountStruct.Equipment> Equipment
        {
            get
            {
                if (_eqList != null)
                    return _eqList;
                using (var str = new MemoryStream(Convert.FromBase64String(PlayerAccount.Equipment)))
                    _eqList = Serializer.Deserialize<List<AccountStruct.Equipment>>(str);
                return _eqList;
            }
        }

        public List<AccountStruct.Cargo> Cargo
        {
            get
            {
                if (_cargoList != null)
                    return _cargoList;

                using (var str = new MemoryStream(Convert.FromBase64String(PlayerAccount.Cargo)))
                    _cargoList = Serializer.Deserialize<List<AccountStruct.Cargo>>(str);
                return _cargoList;
            }
        }


        #endregion

        /// <summary>
        ///     Load the specified character file, resetting all character specific
        ///     content for this player and notifying all players of the name.
        /// </summary>
        /// <param name="account">Player account</param>
        /// <param name="log"></param>
        /// <returns>Returns null on successful load otherwise returns error message as a string.</returns>
        public string LoadCharFile(Account account, ILogController log)
        {

            if (account == null)
            {
                log.AddLog(LogType.ERROR, "Broken account found!");
                return "Account is null!";
            }
            //null checks made earlier
            // ReSharper disable once PossibleInvalidOperationException
            PlayerAccount = account;
            Wgrp = new WeaponGroup();

// ReSharper disable once PossibleNullReferenceException
            Name = PlayerAccount.CharName;
            Money = PlayerAccount.Money;

            var arch = ArchetypeDB.Find(PlayerAccount.Ship);
            if (arch is ShipArchetype)
                Ship.Arch = arch;
            else
                return "invalid ship";

            if (ShipState.RepGroup == "")
                Ship.faction = new Faction();
            else
            {
                Ship.faction = UniverseDB.FindFaction(ShipState.RepGroup);
                if (Ship.faction == null)
                    return "invalid faction";
            }

            Ship.System = UniverseDB.FindSystem(PlayerAccount.System);
            if (Ship.System == null)
                return "invalid system";

            if (ShipState.Base == null)
                Ship.Basedata = null;
            else
            {
                Ship.Basedata = UniverseDB.FindBase(ShipState.Base);
                if (Ship.Basedata == null)
                    return "invalid base";
            }

            if (ShipState.LastBase == "")
            {
                Ship.RespawnBasedata = null;
                return "no respawn base";
            }

            Ship.RespawnBasedata = UniverseDB.FindBase(ShipState.LastBase);
            if (Ship.RespawnBasedata == null)
                return "invalid respawn base";




            if (Ship.Basedata == null)
            {

                if (ShipState.Position != null)
                    Ship.Position = ShipState.Position;

                if (ShipState.Rotate != null)
                {
                    Ship.Orientation = Matrix.EulerDegToMatrix(ShipState.Rotate);
                }
            }

            //TODO: why ShipState.Hull is always true
            Ship.Health = ShipState.Hull;
            if (Ship.Health <= 0)
                Ship.Health = 0.05f;

            Ship.voiceid = FLUtility.CreateID(Appearance.Voice);

            //TODO: calculate rank
// ReSharper disable once PossibleNullReferenceException
            Ship.Rank = PlayerAccount.Rank;

            Ship.com_body = Appearance.Body;
            Ship.com_head = Appearance.Head;
            Ship.com_lefthand = Appearance.LeftHand;
            Ship.com_righthand = Appearance.RightHand;

            Ship.Items.Clear();

            uint hpid = 34;
            foreach (var set in Equipment)
            {
                var si = new ShipItem
                {
                    arch = ArchetypeDB.Find(set.Arch),
                    hpname = set.HpName,
                    health = set.Health,
                    count = 1,
                    mission = false,
                    mounted = true,
                    hpid = hpid++
                };
                Ship.Items[si.hpid] = si;
            }

            foreach (var set in Cargo)
            {
                var si = new ShipItem
                {
                    arch = ArchetypeDB.Find(set.Arch),
                    hpname = "",
                    count = set.Count,
                    health = 1.0f,
                    mission = false,
                    mounted = false,
                    hpid = hpid++
                };
                Ship.Items[si.hpid] = si;
            }

            Ship.Reps.Clear();


            foreach (var set in RepDictionary)
            {
                float rep = set.Value;
                var faction = UniverseDB.FindFaction(set.Key);
                if (faction == null)
// ReSharper disable once PossibleNullReferenceException
                    log.AddLog(LogType.ERROR, "error: faction not found char={0} faction={1}", account.CharName,
                        set.Value);
                else
                    Ship.Reps[faction] = rep;
            }

            
            //Visits.Clear();
            //foreach (var set in Visits)
            //{
            //    Visits[set.Key] = set.Value;
            //}

            Ship.CurrentAction = null;

            return null;
        }

        /// <summary>
        ///     Save the current player state.
        /// </summary>
        public void SaveCharFile()
        {
            if (PlayerAccount == null)
                return;

            //Charfile.AddSetting("Player", "money", new object[] { Money });
            //Charfile.AddSetting("Player", "rank", new object[] { Ship.Rank });
            //Charfile.AddSetting("Player", "ship_archetype", new object[] { Ship.Arch.ArchetypeID });
            //Charfile.AddSetting("Player", "system", new object[] { Ship.System.nickname });

            var ss = new AccountStruct.ShipState
            {
                Hull = Ship.Health,
                LastBase = Ship.RespawnBasedata.Nickname,
                RepGroup = Ship.faction.Nickname
            };

            if (Ship.Basedata != null)
                ss.Base = Ship.Basedata.Nickname;
            else
            {
                ss.Position = Ship.Position;
                ss.Rotate = Matrix.MatrixToEulerDeg(Ship.Orientation);
            }

            

            var ap = new AccountStruct.Appearance
            {
                Body = Ship.com_body,
                Head = Ship.com_head,
                LeftHand = Ship.com_lefthand,
                RightHand = Ship.com_righthand,
                Voice = "trent_voice"
            };
            var eq = new List<AccountStruct.Equipment>();
            var ca = new List<AccountStruct.Cargo>();
            var repd = Ship.Reps.ToDictionary(rep => rep.Key.Nickname, rep => rep.Value);

            PlayerAccount.Ship = Ship.Arch.ArchetypeID;
            //TODO: save weap grp
            PlayerAccount.Money = Money;
            PlayerAccount.System = Ship.System.Nickname;

            foreach (var si in Ship.Items.Values)
            {
                if (si.mounted)
                {
                    eq.Add(new AccountStruct.Equipment { Arch = si.arch.ArchetypeID, Health = si.health, HpName = si.hpname });

                }
                else
                {
                    ca.Add(new AccountStruct.Cargo { Arch = si.arch.ArchetypeID, Count = si.count });

                }
            }

            using (var stream = new MemoryStream())
            {
                Serializer.Serialize(stream, ss);
                PlayerAccount.ShipState = Convert.ToBase64String(stream.ToArray());
            }

            using (var stream = new MemoryStream())
            {
                Serializer.Serialize(stream, Settings);
                PlayerAccount.Settings = Convert.ToBase64String(stream.ToArray());
            }

            using (var stream = new MemoryStream())
            {
                Serializer.Serialize(stream, ap);
                PlayerAccount.Appearance = Convert.ToBase64String(stream.ToArray());
            }

            using (var stream = new MemoryStream())
            {
                Serializer.Serialize(stream, eq);
                PlayerAccount.Equipment = Convert.ToBase64String(stream.ToArray());
            }

            using (var stream = new MemoryStream())
            {
                Serializer.Serialize(stream, ca);
                PlayerAccount.Cargo = Convert.ToBase64String(stream.ToArray());
            }

            using (var stream = new MemoryStream())
            {
                Serializer.Serialize(stream, repd);
                PlayerAccount.RepList = Convert.ToBase64String(stream.ToArray());
            }

            PlayerAccount.TimeOnline += (uint)DateTime.UtcNow.Subtract(PlayerAccount.LastOnline).Minutes;
            PlayerAccount.LastOnline = DateTime.UtcNow;

            

            Database.ModifyAccount(PlayerAccount);

        }
    }

}
