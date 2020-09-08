using FLServer.Object.Solar;
using FLServer.Physics;

namespace FLServer.Solar
{
    public class Zone
    {
        /// <summary>
        ///     The damage in hit_pts this zone causes to ships in it.
        /// </summary>
        public float damage;

        /// <summary>
        ///     The target number of NPCs per cubic wibble in the zone.
        /// </summary>
        public float density;

        /// <summary>
        /// </summary>
        public double interference;

        public string nickname;
        public Shape shape;
        public uint zoneid;

        public bool IsInZone(float ix, float iy, float iz)
        {
            if (shape == null) return false;

            return shape.IsInside(new Vector(ix, iy, iz));
        }

        public bool IsInZone(Vector position)
        {
            return shape.IsInside(position);
        }
    }
}
