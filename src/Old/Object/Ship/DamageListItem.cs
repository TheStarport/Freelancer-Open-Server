namespace FLServer.Ship
{
    public class DamageListItem
    {
        public const uint HULL = 1;
        public const uint SHIELD = 0xFFF1;

        /// <summary>
        ///     Blow it up
        /// </summary>
        public bool destroyed;

        /// <summary>
        ///     Absolute hit points
        /// </summary>
        public float hit_pts;

        /// <summary>
        ///     1 = root, 0xFFF1 = shield, 2-33 collisiongroup, 34+ equip/cargo
        /// </summary>
        public uint hpid;

        public DamageListItem(uint hpid, float hit_pts, bool destroyed)
        {
            this.hpid = hpid;
            this.hit_pts = hit_pts;
            this.destroyed = destroyed;
        }
    }
}
