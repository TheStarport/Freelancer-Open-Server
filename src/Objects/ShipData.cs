using System.Collections.Generic;
using FLServer.GameDB.Arch;
using System.Linq;

namespace FLServer.Objects
{
    /// <summary>
    /// This class is used to store all the generic info about ship itself.
    /// </summary>
    public class ShipData
    {

        public class ShipItem
        {

            uint _archID;
            Archetype _arch;

            public uint Count;
            public float Health = 1.0f;
            public uint HardpointID;
            public string HardpointName;
            public bool IsMission;
            public bool IsMounted;

            public ShipItem(uint sarch)
            {
                _archID = sarch;
            }


            public Archetype Archetype
            {
                get
                {
                    if (_archID == 0)
                        return null;
                    if (_arch == null)
                        _arch = GameDB.ArchetypeDB.GetArchetype(_archID);
                    return _arch;
                }
                set
                {
                    _arch = value;
                    if (_arch != null)
                        _archID = DataWorkers.FLUtility.CreateID(_arch.Nickname);
                }

            }
        }

        /// <summary>
        ///     The cargo and equipment on this ship.
        /// </summary>
        public Dictionary<uint, ShipItem> Items = new Dictionary<uint, ShipItem>();

        public ShipItem GetItemByGood(uint goodid)
        {
            return Items.Values.FirstOrDefault(item => item.Archetype.ArchetypeID == goodid);
        }

        public ShipItem GetItemByHpid(uint hpid)
        {
            return Items.ContainsKey(hpid) ? Items[hpid] : null;
        }

        public uint FindFreeHpid()
        {
            uint hpid = 2;
            while (Items.ContainsKey(hpid))
                hpid++;
            return hpid;
        }

        public float Health = 1.0f;

        uint _archID;
        Archetype _arch;

        /// <summary>
        /// Gets or sets the ship's archetype.
        /// </summary>
        /// <value>The archetype.</value>
        public Archetype Archetype
        {
            get
            {
                if (_archID == 0)
                    return null;
                if (_arch == null)
                    _arch = GameDB.ArchetypeDB.GetArchetype(_archID);
                return _arch;
            }
            set
            {
                _arch = value;
                if (_arch != null)
                    _archID = DataWorkers.FLUtility.CreateID(_arch.Nickname);
            }
        }



    }
}

