using FLServer.Physics;

namespace FLServer
{
    public class Waypoint
    {
        protected bool Equals(Waypoint other)
        {
            return ObjID == other.ObjID && Position.DistanceTo(other.Position) < 1 && SystemID == other.SystemID;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((Waypoint) obj);
        }

        public uint ObjID;
        public Vector Position;
        public uint SystemID;

        public Waypoint(Vector position, uint objid, uint systemid)
        {
            Position = position;
            ObjID = objid;
            SystemID = systemid;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = (int) ObjID;
                hashCode = (hashCode*397) ^ (Position != null ? Position.GetHashCode() : 0);
                hashCode = (hashCode*397) ^ (int) SystemID;
                return hashCode;
            }
        }

        public static bool operator ==(Waypoint a, Waypoint b)
        {
            return (((object) a) == null && ((object) b) == null) ||
                   (((object) a) != null && ((object) b) != null && a.Equals(b));
        }

        public static bool operator !=(Waypoint a, Waypoint b)
        {
            return (((object) a) == null && ((object) b) != null) || (((object) a) != null && ((object) b) == null) ||
                   !a.Equals(b);
        }
    }
}