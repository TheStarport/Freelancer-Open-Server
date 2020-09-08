using System.Collections.Generic;

namespace FLServer
{
    internal class CmpRevData
    {
        /// <summary>
        ///     The list of parts in the rev data.
        /// </summary>
        public List<Part> Parts = new List<Part>();

        /// <summary>
        ///     Decode a rev node. Throw an exception if this fails.
        /// </summary>
        public CmpRevData(byte[] data)
        {
            var pos = 0;
            for (var count = 0; count < (data.Length/0xD0); count++)
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
                    AxisRotX = Utilities.GetFloat(data, ref pos),
                    AxisRotY = Utilities.GetFloat(data, ref pos),
                    AxisRotZ = Utilities.GetFloat(data, ref pos),
                    Min = Utilities.GetFloat(data, ref pos),
                    Max = Utilities.GetFloat(data, ref pos)
                };

                Parts.Add(part);
            }
        }

        public class Part
        {
            public float AxisRotX;
            public float AxisRotY;
            public float AxisRotZ;
            public string ChildName;
            public float Max;
            public float Min;
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