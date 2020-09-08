using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FLServer.Old.CharacterDB.AccountStruct;
using FLServer.Player.PlayerExtensions;
using ProtoBuf;
using FLServer.Physics;

namespace FLServer.Old.CharacterDB
{

    /// <summary>
    /// This is a pit of doom, class saved only for reference.
    /// </summary>
    class Importer
    {
        /// <summary>
        ///     The number of kills by type.
        /// </summary>
        public Dictionary<uint, uint> Kills = new Dictionary<uint, uint>();

        /// <summary>
        ///     Player's weapon groups.
        /// </summary>
        public WeaponGroup Wgrp;

        /// <summary>
        ///     Map of objects seen by the player
        /// </summary>
        public Dictionary<uint, uint> Visits = new Dictionary<uint, uint>();


        /// <summary>
        ///     Converts old INI-based savefile to a new SQLite row.
        /// </summary>
        /// <returns>Account on success; otherwise null.</returns>
        public Account LoadCharFile(string path, ILogController log)
        {
            Wgrp = new WeaponGroup();

            var charfile = new FLDataFile(path, true);

            var name = charfile.GetSetting("Player", "name").UniStr(0);
            var money = charfile.GetSetting("Player", "money").UInt(0);

            var accArch = charfile.GetSetting("Player", "ship_archetype").UInt(0);

            var accSendDeathMsg = bool.Parse((string)charfile.GetSetting("Player", "senddeath").Values[0]);
            var accSendSystemDeathMsg = bool.Parse((string)charfile.GetSetting("Player", "sendsystemdeath").Values[0]);


            string accFaction;
            if (charfile.SettingExists("Player", "rep_group"))
            {
                accFaction = charfile.GetSetting("Player", "rep_group").Str(0);
            }
            else
            {
                // empty but legal faction
                accFaction = "";
            }


            var accSystem = charfile.GetSetting("Player", "system").Str(0);

            string accBase;
            if (charfile.SettingExists("Player", "base"))
            {
                accBase = charfile.GetSetting("Player", "base").Str(0);
            }
            else
            {
                accBase = null;
            }

            var accLastBase = charfile.GetSetting("Player", "last_base").Str(0);

            Vector accPosition = null;
            Vector accRotate = null;
            if (accBase == null)
            {
                if (charfile.SettingExists("Player", "pos"))
                {
                    accPosition = charfile.GetSetting("Player", "pos").Vector();
                }

                if (charfile.SettingExists("Player", "rotate"))
                {
                   accRotate = charfile.GetSetting("Player", "rotate").Vector();
                }
            }

            float health;
            if (charfile.SettingExists("Player", "base_hull_status"))
            {
                health = charfile.GetSetting("Player", "base_hull_status").Float(0);
                if (health <= 0)
                    health = 0.05f;
            }
            else
                health = 1;

            string accVoiceID = charfile.GetSetting("Player", "voice").Str(0);

            //Ship.Rank = Charfile.GetSetting("Player", "rank").UInt(0); // fixme, calculate this

            var accComBody = charfile.GetSetting("Player", "com_body").UInt(0);
            var accComHead = charfile.GetSetting("Player", "com_head").UInt(0);
            var accComLefthand = charfile.GetSetting("Player", "com_lefthand").UInt(0);
            var accComRighthand = charfile.GetSetting("Player", "com_righthand").UInt(0);

            var equip = charfile.GetSettings("Player", "equip");
            var accEquip = equip.Select(set => new Equipment {Arch = set.UInt(0), HpName = set.Str(1), Health = set.Float(2)}).ToList();

            var cargo = charfile.GetSettings("Player", "cargo");
            var accCargo = cargo.Select(set => new Cargo {Arch = set.UInt(0), Count = set.UInt(1)}).ToList();

            Dictionary<string, float> accRep = charfile.GetSettings("Player", "house").ToDictionary(set => set.Str(1), set => set.Float(0));

            var accVisits = charfile.GetSettings("Player", "visit").ToDictionary(set => set.UInt(0), set => set.UInt(1));

            var acc = new Account
            {
                CharName = name, 
                Money = (Int32) money,
                Ship = accArch,
                System = accSystem
            };

            using (var stream = new MemoryStream())
            {
                Serializer.Serialize(stream, accRep);
                acc.RepList = Convert.ToBase64String(stream.ToArray());
            }

            using (var stream = new MemoryStream())
            {
                Serializer.Serialize(stream, accVisits);
                acc.Visits = Convert.ToBase64String(stream.ToArray());
            }

            using (var stream = new MemoryStream())
            {
                Serializer.Serialize(stream, accCargo);
                acc.Cargo = Convert.ToBase64String(stream.ToArray());
            }

            using (var stream = new MemoryStream())
            {
                Serializer.Serialize(stream, accEquip);
                acc.Equipment = Convert.ToBase64String(stream.ToArray());
            }

            var ap = new Appearance
            {
                Body = accComBody,
                Head = accComHead,
                LeftHand = accComLefthand,
                RightHand = accComRighthand,
                Voice = accVoiceID
            };

            using (var stream = new MemoryStream())
            {
                Serializer.Serialize(stream, ap);
                acc.Appearance = Convert.ToBase64String(stream.ToArray());
            }

            var ss = new ShipState
            {
                Base = accBase,
                Hull = health,
                LastBase = accLastBase,
                Position = accPosition,
                RepGroup = accFaction,
                Rotate = accRotate
            };

            using (var stream = new MemoryStream())
            {
                Serializer.Serialize(stream, ss);
                acc.ShipState = Convert.ToBase64String(stream.ToArray());
            }

            var sets = new Dictionary<string, bool>
            {
                {@"senddeath", accSendDeathMsg},
                {@"sendsystemdeath", accSendSystemDeathMsg}
            };

            using (var stream = new MemoryStream())
            {
                Serializer.Serialize(stream, sets);
                acc.Settings = Convert.ToBase64String(stream.ToArray());
            }


            //TODO: figure out where's ID is stored
            //acc.ID =

            return acc;
        }

        }
    }
