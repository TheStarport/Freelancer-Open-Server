using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FOS_ng.Data.Arch
{
    public class DockingPoint
    {
        public enum DockingSphere
        {
            // ReSharper disable once InconsistentNaming
            AIRLOCK = 0, // Nothing can dock with this
            // ReSharper disable once InconsistentNaming
            BERTH = 1,
            // ReSharper disable once InconsistentNaming
            RING = 2,
            // ReSharper disable once InconsistentNaming
            MOOR_SMALL = 4,
            // ReSharper disable once InconsistentNaming
            MOOR_MEDIUM = 8,
            // ReSharper disable once InconsistentNaming
            MOOR_LARGE = 16,
            // ReSharper disable once InconsistentNaming
            TRADELANE_RING = 32,
            // ReSharper disable once InconsistentNaming
            JUMP = 64 // Everything can dock with this
        }

        public float DockingRadius;

        public string HpName;
        public Vector Position;
        public Matrix Rotation;
        public DockingSphere Type;
    }
}
