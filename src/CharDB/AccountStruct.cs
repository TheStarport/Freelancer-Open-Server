using System.Collections.Generic;
using FLServer.Physics;
using Jitter.LinearMath;
using ProtoBuf;

// ReSharper disable once CheckNamespace
namespace FLServer.CharDB
{
    [ProtoContract]
    public class ShipState
    {
        [ProtoMember(1)]
        public string RepGroup;
        [ProtoMember(2)]
        public string Base;
        [ProtoMember(3)]
        public string LastBase;
        [ProtoMember(4)]
        public JVector Position;
        [ProtoMember(5)]
        public JVector Rotate;
        [ProtoMember(6)]
        public float Hull = 1;
    }

    /// <summary>
    /// TODO: Use statistics
    /// </summary>
    [ProtoContract]
    public class Stats
    {
        [ProtoMember(1)]
        public Dictionary<uint, uint> Kills;
        [ProtoMember(2)]
        public uint MsnSuccesses;
        [ProtoMember(3)]
        public uint MsnFailures;
    }

    [ProtoContract]
    public class Appearance
    {
        [ProtoMember(1)]
        public string Voice;
        [ProtoMember(2)]
        public uint Body;
        [ProtoMember(3)]
        public uint Head;
        [ProtoMember(4)]
        public uint LeftHand;
        [ProtoMember(5)]
        public uint RightHand;
    }

    [ProtoContract]
    public class Equipment
    {
        [ProtoMember(1)]
        public uint Arch;
        [ProtoMember(2)]
        public string HpName;
        [ProtoMember(3)]
        public float Health;
    }

    [ProtoContract]
    public class Cargo
    {
        [ProtoMember(1)]
        public uint Arch;
        [ProtoMember(2)]
        public uint Count;
    }
}
