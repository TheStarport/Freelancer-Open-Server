using System.Collections.Generic;

namespace FLServer
{
    internal class CmpFixData
    {
        /// <summary>
        ///     The list of parts in the fix data.
        /// </summary>
        public List<Part> Parts = new List<Part>();

        /// <summary>
        ///     Decode a fix node. Throw an exception if this fails.
        /// </summary>
        public CmpFixData(byte[] data)
        {
            int pos = 0;
            int numParts = data.Length/0xb0;
            for (var count = 0; count < numParts; count++)
            {
                var part = new Part
                {
                    ParentName = Utilities.GetString(data, ref pos, 0x40),
                    ChildName = Utilities.GetString(data, ref pos, 0x40),
                    OriginX = Utilities.GetFloat(data, ref pos),
                    OriginY = Utilities.GetFloat(data, ref pos),
                    OriginZ = Utilities.GetFloat(data, ref pos),
                    RotMatXX = Utilities.GetFloat(data, ref pos),
                    RotMatXY = Utilities.GetFloat(data, ref pos),
                    RotMatXZ = Utilities.GetFloat(data, ref pos),
                    RotMatYX = Utilities.GetFloat(data, ref pos),
                    RotMatYY = Utilities.GetFloat(data, ref pos),
                    RotMatYZ = Utilities.GetFloat(data, ref pos),
                    RotMatZX = Utilities.GetFloat(data, ref pos),
                    RotMatZY = Utilities.GetFloat(data, ref pos),
                    RotMatZZ = Utilities.GetFloat(data, ref pos)
                };

                Parts.Add(part);
            }
        }

        public class Part
        {
            public string ChildName;
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