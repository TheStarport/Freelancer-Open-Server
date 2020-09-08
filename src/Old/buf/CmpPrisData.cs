using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FLServer
{
    internal class CmpPrisData
    {
        /// <summary>
        ///     The list of parts in the fix data.
        /// </summary>
        public List<Part> Parts = new List<Part>();

        /// <summary>
        ///     Decode a pris node. Throw an exception if this fails.
        /// </summary>
        public CmpPrisData(byte[] data)
        {
            var pos = 0;
            for (var count = 0; count < (data.Length/0xD0); count++)
            {
                var part = new Part();

                var index = data.ToList().FindIndex(pos, 0x40, value => value == 0) - pos;
                if (index < 0 || index > 0x3F)
                    index = 0x3F;
                part.ParentName = Encoding.ASCII.GetString(data, pos, index).Trim();
                pos += 0x40;

                index = data.ToList().FindIndex(pos, 0x40, value => value == 0) - pos;
                if (index < 0 || index > 0x3F)
                    index = 0x3F;
                part.ChildName = Encoding.ASCII.GetString(data, pos, index).Trim();
                pos += 0x40;

                part.OriginX = BitConverter.ToSingle(data, pos);
                pos += 4;
                part.OriginY = BitConverter.ToSingle(data, pos);
                pos += 4;
                part.OriginZ = BitConverter.ToSingle(data, pos);
                pos += 4;

                part.OffsetX = BitConverter.ToSingle(data, pos);
                pos += 4;
                part.OffsetY = BitConverter.ToSingle(data, pos);
                pos += 4;
                part.OffsetZ = BitConverter.ToSingle(data, pos);
                pos += 4;

                part.RotMatXX = BitConverter.ToSingle(data, pos);
                pos += 4;
                part.RotMatXY = BitConverter.ToSingle(data, pos);
                pos += 4;
                part.RotMatXZ = BitConverter.ToSingle(data, pos);
                pos += 4;

                part.RotMatYX = BitConverter.ToSingle(data, pos);
                pos += 4;
                part.RotMatYY = BitConverter.ToSingle(data, pos);
                pos += 4;
                part.RotMatYZ = BitConverter.ToSingle(data, pos);
                pos += 4;

                part.RotMatZX = BitConverter.ToSingle(data, pos);
                pos += 4;
                part.RotMatZY = BitConverter.ToSingle(data, pos);
                pos += 4;
                part.RotMatZZ = BitConverter.ToSingle(data, pos);
                pos += 4;

                part.AxisRotX = BitConverter.ToSingle(data, pos);
                pos += 4;
                part.AxisRotY = BitConverter.ToSingle(data, pos);
                pos += 4;
                part.AxisRotZ = BitConverter.ToSingle(data, pos);
                pos += 4;

                part.Unknown = BitConverter.ToSingle(data, pos);
                pos += 4;
                part.Angle = BitConverter.ToSingle(data, pos);
                pos += 4;

                Parts.Add(part);
            }
        }

        public class Part
        {
            public float Angle;
            public float AxisRotX;
            public float AxisRotY;
            public float AxisRotZ;
            public string ChildName;
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
            public float Unknown;
        };
    }
}