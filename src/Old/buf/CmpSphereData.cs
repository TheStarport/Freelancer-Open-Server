using System.Collections.Generic;

namespace FLServer
{
    internal class CmpSphereData
    {
        /// <summary>
        ///     The list of parts in the sphere data.
        /// </summary>
        public List<Part> Parts = new List<Part>();

        /// <summary>
        ///     Decode a sphere node. Throw an exception if this fails.
        /// </summary>
        public CmpSphereData(byte[] data)
        {
            var pos = 0;
            for (var count = 0; count < (data.Length/0xD4); count++)
            {
                var part = new Part
                {
                    ParentName = Utilities.GetString(data, ref pos, 0x40),
                    ChildName = Utilities.GetString(data, ref pos, 0x40),
                    OriginX = Utilities.GetFloat(data, ref pos),
                    OriginY = Utilities.GetFloat(data, ref pos),
                    OriginZ = Utilities.GetFloat(data, ref pos),
                    OffsetX = Utilities.GetFloat(data, ref pos),
                    OffsetY = Utilities.GetFloat(data, ref pos),
                    OffsetZ = Utilities.GetFloat(data, ref pos),
                    RotMatXX = Utilities.GetFloat(data, ref pos),
                    RotMatXY = Utilities.GetFloat(data, ref pos),
                    RotMatXZ = Utilities.GetFloat(data, ref pos),
                    RotMatYX = Utilities.GetFloat(data, ref pos),
                    RotMatYY = Utilities.GetFloat(data, ref pos),
                    RotMatYZ = Utilities.GetFloat(data, ref pos),
                    RotMatZX = Utilities.GetFloat(data, ref pos),
                    RotMatZY = Utilities.GetFloat(data, ref pos),
                    RotMatZZ = Utilities.GetFloat(data, ref pos),
                    Min1 = Utilities.GetFloat(data, ref pos),
                    Max1 = Utilities.GetFloat(data, ref pos),
                    Min2 = Utilities.GetFloat(data, ref pos),
                    Max2 = Utilities.GetFloat(data, ref pos),
                    Min3 = Utilities.GetFloat(data, ref pos),
                    Max3 = Utilities.GetFloat(data, ref pos)
                };

                Parts.Add(part);
            }
        }

        public class Part
        {
            public string ChildName;
            public float Max1;
            public float Max2;
            public float Max3;
            public float Min1;
            public float Min2;
            public float Min3;
            public float OffsetX;
            public float OffsetY;
            public float OffsetZ;
            public float OriginX;
            public float OriginY;
            public float OriginZ;
            public string ParentName;
            public float RotMatXX;
            public float RotMatXY;
            public float RotMatXZ;
            public float RotMatYX;
            public float RotMatYY;
            public float RotMatYZ;
            public float RotMatZX;
            public float RotMatZY;
            public float RotMatZZ;
        };
    }
}