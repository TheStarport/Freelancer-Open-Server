using FOS_ng.Data.Arch;
using FOS_ng.Objects;

namespace FLServer.Ship
{
    public class ShipItem
    {
        public bool activated = false;
        public Archetype arch;
        public uint count = 0;
        public float health = 1.0f;
        public uint hpid;
        public string hpname;
        public bool mission = false;
        public bool mounted = false;
        public IUpdatable sim;
    }
}
