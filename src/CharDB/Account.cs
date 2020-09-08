using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using ProtoBuf;

namespace FLServer.CharDB
{
    public class Account
    {
        /// <summary>
        /// Unique identifier for an account. For tracking purposes
        /// </summary>
        [XmlIgnore]
        public string UID;

        [XmlIgnore]
        public string ID;

        [XmlIgnore]
        public string CharName;

        [XmlIgnore]
        public string CharFileName;

        /// <summary>
        /// Minutes online count.
        /// </summary>
        [XmlIgnore]
        public UInt32 TimeOnline;

        public Int32 Money;


        /// <summary>
        /// ShipArch hashcode.
        /// </summary>
        public uint Ship;

        /// <summary>
        /// System nickname.
        /// </summary>
        public string System;

        /// <summary>
        /// 
        /// </summary>
        public string Location;

        /// <summary>
        /// Last time account was updated, UTC time.
        /// </summary>
        public DateTime LastOnline = DateTime.UtcNow;
        public byte Rank;
        public bool IsBanned;

        /// <summary>
        /// Base64-encoded serialized string.
        /// </summary>
        public string eSettings;

        [XmlIgnore]
        private Dictionary<string, bool> _sets;
        [XmlIgnore]
        public Dictionary<string, bool> Settings
        {
            get
            {
                if (_sets != null) return _sets;
                if (eSettings != null)
                using (var str = new MemoryStream(Convert.FromBase64String(eSettings)))
                    _sets = Serializer.Deserialize<Dictionary<string, bool>>(str);
                else _sets = new Dictionary<string, bool>();
                return _sets;
            }
            set { _sets = value; }
        }


        /// <summary>
        /// Base64-encoded serialized string.
        /// </summary>
        public string eShipState;
        [XmlIgnore]
        private ShipState _shipState;

        /// <summary>
        /// Base64-encoded serialized string.
        /// </summary>
        public string eAppearance;
        [XmlIgnore]
        private Appearance _appearance;

        /// <summary>
        /// Base64-encoded serialized string.
        /// </summary>
        public string eEquipment;

        [XmlIgnore]
        private List<Equipment> _equipment;
        /// <summary>
        /// Base64-encoded serialized string.
        /// </summary>
        public string eCargo;
        [XmlIgnore]
        private List<Cargo> _cargo;
        [XmlIgnore]
        public List<Cargo> Cargo
        {
            get
            {
                if (_cargo != null) return _cargo;
                if (eCargo != null)
                    using (var str = new MemoryStream(Convert.FromBase64String(eCargo)))
                        _cargo = Serializer.Deserialize<List<Cargo>>(str);
                else _cargo = new List<Cargo>();
                return _cargo;
            }
            set { _cargo = value; }
        }


        /// <summary>
        /// Base64-encoded serialized string.
        /// </summary>
        public string eRepList;

        [XmlIgnore]
        private Dictionary<string, float> _reps;
        [XmlIgnore]
        public Dictionary<string, float> Reputations
        {
            get
            {
                if (_reps != null) return _reps;
                if (eRepList != null)
                using (var str = new MemoryStream(Convert.FromBase64String(eRepList)))
                    _reps = Serializer.Deserialize<Dictionary<string, float>>(str);
                else _reps = new Dictionary<string, float>();
                return _reps;
            }
            set { _reps = value; }
        }

        /// <summary>
        /// Base64-encoded serialized string.
        /// </summary>
        public string eKills;

        [XmlIgnore]
        private Dictionary<uint,uint> _kills;
        [XmlIgnore]
        public Dictionary<uint, uint> Kills
        {
            get
            {
                if (_kills != null) return _kills;
                if (eKills != null)
                using (var str = new MemoryStream(Convert.FromBase64String(eKills)))
                    _kills = Serializer.Deserialize<Dictionary<uint, uint>>(str);
                else _kills = new Dictionary<uint, uint>();
                return _kills;
            }
            set { _kills = value; }
        }


        /// <summary>
        /// Base64-encoded serialized string.
        /// </summary>
        public string eVisits;


        [XmlIgnore]
        private Dictionary<uint, uint> _visits;
        [XmlIgnore]
        public Dictionary<uint, uint> Visits
        {
            get
            {
                if (_visits != null) return _visits;
                if (eVisits != null)
                using (var str = new MemoryStream(Convert.FromBase64String(eVisits)))
                    _visits = Serializer.Deserialize<Dictionary<uint,uint>>(str);
                else _visits = new Dictionary<uint, uint>();
                return _visits;
            }
            set { _visits = value; }
        }

        [XmlIgnore]
        public ShipState ShipState
        {
            get
            {
                if (_shipState != null) return _shipState;
                if (eShipState != null)
                using (var str = new MemoryStream(Convert.FromBase64String(eShipState)))
                    _shipState = Serializer.Deserialize<ShipState>(str);
                else _shipState = new ShipState();
                return _shipState;
            }
            set { _shipState = value; }
        }

        [XmlIgnore]
        public Appearance Appearance
        {
            get
            {
                if (_appearance != null) return _appearance;
                if (eAppearance != null)
                using (var str = new MemoryStream(Convert.FromBase64String(eAppearance)))
                    _appearance = Serializer.Deserialize<Appearance>(str);
                else _appearance = new Appearance();
                return _appearance;
            }
        }

        [XmlIgnore]
        public List<Equipment> Equipment
        {
            get
            {
                if (_equipment != null) return _equipment;
                if (eEquipment != null)
                using (var str = new MemoryStream(Convert.FromBase64String(eEquipment)))
                    _equipment = Serializer.Deserialize<List<Equipment>>(str);
                else _equipment = new List<Equipment>();
                return _equipment;
            }
            set { _equipment = value; }
        }

        public void Serialize()
        {
            using (var stream = new MemoryStream())
            {
                Serializer.Serialize(stream, Settings);
                eSettings = Convert.ToBase64String(stream.ToArray());
            }

            using (var stream = new MemoryStream())
            {
                Serializer.Serialize(stream, ShipState);
                eShipState = Convert.ToBase64String(stream.ToArray());
            }

            using (var stream = new MemoryStream())
            {
                Serializer.Serialize(stream, Appearance);
                eAppearance = Convert.ToBase64String(stream.ToArray());
            }

            using (var stream = new MemoryStream())
            {
                Serializer.Serialize(stream, Equipment);
                eEquipment = Convert.ToBase64String(stream.ToArray());
            }

            using (var stream = new MemoryStream())
            {
                Serializer.Serialize(stream, Cargo);
                eCargo = Convert.ToBase64String(stream.ToArray());
            }

            using (var stream = new MemoryStream())
            {
                Serializer.Serialize(stream, Reputations);
                eRepList = Convert.ToBase64String(stream.ToArray());
            }

            using (var stream = new MemoryStream())
            {
                Serializer.Serialize(stream, Kills);
                eKills = Convert.ToBase64String(stream.ToArray());
            }

            using (var stream = new MemoryStream())
            {
                Serializer.Serialize(stream, Visits);
                eVisits = Convert.ToBase64String(stream.ToArray());
            }
        }

    }
}
