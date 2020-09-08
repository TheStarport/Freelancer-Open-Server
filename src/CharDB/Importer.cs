using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using FLDataFile;
using FLServer.DataWorkers;
using Jitter.LinearMath;

namespace FLServer.CharDB
{
    /// <summary>
    /// This class is useful for conversion old INI-based saves to Account.
    /// </summary>
    class Importer
    {
        /// <summary>
        ///     The number of kills by type.
        /// </summary>
        public Dictionary<uint, uint> Kills = new Dictionary<uint, uint>();

        /// <summary>
        ///     Map of objects seen by the player
        /// </summary>
        public Dictionary<uint, uint> Visits = new Dictionary<uint, uint>();


        /// <summary>
        ///     Converts old INI-based savefile to a new SQLite row.
        /// </summary>
        /// <returns>Account on success.</returns>
        public Account LoadCharFile(string path, ILogController log)
        {
            var acct = new Account();

            var cfile = new DataFile(path);

            var pSection = cfile.GetFirstOf("Player");
            if (pSection == null) throw new Exception("Section Player in default account not found!");

            acct.CharName = FLUtility.DecodeUnicodeHex(Convert.ToString(pSection.GetFirstOf("name")[0]));
            acct.Money = int.Parse(pSection.GetFirstOf("money")[0]);
            acct.Ship = uint.Parse(pSection.GetFirstOf("ship_archetype")[0]);
            acct.Settings["senddeath"] = bool.Parse(pSection.GetFirstOf("senddeath")[0]);
            acct.Settings["systemdeath"] = bool.Parse(pSection.GetFirstOf("sendsystemdeath")[0]);

            Setting tmpSetting;

            if (pSection.TryGetFirstOf("rep_group", out tmpSetting))
                acct.ShipState.RepGroup = tmpSetting[0];

            acct.System = pSection.GetFirstOf("system")[0];

            //string accBase;
            if (pSection.TryGetFirstOf("base", out tmpSetting))
                acct.ShipState.Base = tmpSetting[0];
            else
            {
                if (pSection.TryGetFirstOf("pos", out tmpSetting))
                {
                    acct.ShipState.Position = new JVector(
                        float.Parse(tmpSetting[0]),
                        float.Parse(tmpSetting[1]),
                        float.Parse(tmpSetting[2]));
                }
                if (pSection.TryGetFirstOf("rotate", out tmpSetting))
                {
                    acct.ShipState.Rotate = new JVector(
                        float.Parse(tmpSetting[0]),
                        float.Parse(tmpSetting[1]),
                        float.Parse(tmpSetting[2]));
                }
            }

            acct.ShipState.LastBase = pSection.GetFirstOf("last_base")[0];

            if (pSection.TryGetFirstOf("base_hull_status", out tmpSetting))
            {
                acct.ShipState.Hull = float.Parse(tmpSetting[0]);
                if (acct.ShipState.Hull <= 0) acct.ShipState.Hull = 0.05f;
            }

            acct.Appearance.Voice = pSection.GetFirstOf("voice")[0];

            acct.Appearance.Body = uint.Parse(pSection.GetFirstOf("com_body")[0]);
            acct.Appearance.Head = uint.Parse(pSection.GetFirstOf("com_head")[0]);
            acct.Appearance.LeftHand = uint.Parse(pSection.GetFirstOf("com_lefthand")[0]);
            acct.Appearance.RightHand = uint.Parse(pSection.GetFirstOf("com_righthand")[0]);

            acct.Rank = byte.Parse(pSection.GetFirstOf("rank")[0]);


            acct.Equipment = pSection.GetSettings("equip")
                .Select(set => new Equipment
                {
                    Arch = uint.Parse(set[0]),
                    HpName = set[1],
                    Health = float.Parse(set[2])
                }).ToList();

            acct.Cargo = pSection.GetSettings("cargo")
                .Select(set => new Cargo
                {
                    Arch = uint.Parse(set[0]),
                    Count = uint.Parse(set[1])
                }).ToList();



            acct.Reputations = pSection.GetSettings("house").
                ToDictionary(set => set[1], set => float.Parse(set[0], CultureInfo.InvariantCulture.NumberFormat));

            acct.Visits = pSection.GetSettings("visit").
                ToDictionary(set => uint.Parse(set[0]), set => uint.Parse(set[1]));

            acct.Serialize();
            return acct;
        }

        }
    }
