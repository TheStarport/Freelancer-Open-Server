using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using FOS_ng.Structures;

namespace FOS_ng
{

    //TODO: refactor the shit out of it. Module with random stuff = bad
    internal class Utilities
    {
        /// <summary>
        ///     Look up table for id creation.
        /// </summary>
        private static uint[] _createIdTable;

        private static readonly UInt32[] Flcrc32Tbl =
        {
            0x00000000, 0x09073096, 0x120E612C, 0x1B0951BA,
            0xFF6DC419, 0xF66AF48F, 0xED63A535, 0xE46495A3,
            0xFEDB8832, 0xF7DCB8A4, 0xECD5E91E, 0xE5D2D988,
            0x01B64C2B, 0x08B17CBD, 0x13B82D07, 0x1ABF1D91,
            0xFDB71064, 0xF4B020F2, 0xEFB97148, 0xE6BE41DE,
            0x02DAD47D, 0x0BDDE4EB, 0x10D4B551, 0x19D385C7,
            0x036C9856, 0x0A6BA8C0, 0x1162F97A, 0x1865C9EC,
            0xFC015C4F, 0xF5066CD9, 0xEE0F3D63, 0xE7080DF5,
            0xFB6E20C8, 0xF269105E, 0xE96041E4, 0xE0677172,
            0x0403E4D1, 0x0D04D447, 0x160D85FD, 0x1F0AB56B,
            0x05B5A8FA, 0x0CB2986C, 0x17BBC9D6, 0x1EBCF940,
            0xFAD86CE3, 0xF3DF5C75, 0xE8D60DCF, 0xE1D13D59,
            0x06D930AC, 0x0FDE003A, 0x14D75180, 0x1DD06116,
            0xF9B4F4B5, 0xF0B3C423, 0xEBBA9599, 0xE2BDA50F,
            0xF802B89E, 0xF1058808, 0xEA0CD9B2, 0xE30BE924,
            0x076F7C87, 0x0E684C11, 0x15611DAB, 0x1C662D3D,
            0xF6DC4190, 0xFFDB7106, 0xE4D220BC, 0xEDD5102A,
            0x09B18589, 0x00B6B51F, 0x1BBFE4A5, 0x12B8D433,
            0x0807C9A2, 0x0100F934, 0x1A09A88E, 0x130E9818,
            0xF76A0DBB, 0xFE6D3D2D, 0xE5646C97, 0xEC635C01,
            0x0B6B51F4, 0x026C6162, 0x196530D8, 0x1062004E,
            0xF40695ED, 0xFD01A57B, 0xE608F4C1, 0xEF0FC457,
            0xF5B0D9C6, 0xFCB7E950, 0xE7BEB8EA, 0xEEB9887C,
            0x0ADD1DDF, 0x03DA2D49, 0x18D37CF3, 0x11D44C65,
            0x0DB26158, 0x04B551CE, 0x1FBC0074, 0x16BB30E2,
            0xF2DFA541, 0xFBD895D7, 0xE0D1C46D, 0xE9D6F4FB,
            0xF369E96A, 0xFA6ED9FC, 0xE1678846, 0xE860B8D0,
            0x0C042D73, 0x05031DE5, 0x1E0A4C5F, 0x170D7CC9,
            0xF005713C, 0xF90241AA, 0xE20B1010, 0xEB0C2086,
            0x0F68B525, 0x066F85B3, 0x1D66D409, 0x1461E49F,
            0x0EDEF90E, 0x07D9C998, 0x1CD09822, 0x15D7A8B4,
            0xF1B33D17, 0xF8B40D81, 0xE3BD5C3B, 0xEABA6CAD,
            0xEDB88320, 0xE4BFB3B6, 0xFFB6E20C, 0xF6B1D29A,
            0x12D54739, 0x1BD277AF, 0x00DB2615, 0x09DC1683,
            0x13630B12, 0x1A643B84, 0x016D6A3E, 0x086A5AA8,
            0xEC0ECF0B, 0xE509FF9D, 0xFE00AE27, 0xF7079EB1,
            0x100F9344, 0x1908A3D2, 0x0201F268, 0x0B06C2FE,
            0xEF62575D, 0xE66567CB, 0xFD6C3671, 0xF46B06E7,
            0xEED41B76, 0xE7D32BE0, 0xFCDA7A5A, 0xF5DD4ACC,
            0x11B9DF6F, 0x18BEEFF9, 0x03B7BE43, 0x0AB08ED5,
            0x16D6A3E8, 0x1FD1937E, 0x04D8C2C4, 0x0DDFF252,
            0xE9BB67F1, 0xE0BC5767, 0xFBB506DD, 0xF2B2364B,
            0xE80D2BDA, 0xE10A1B4C, 0xFA034AF6, 0xF3047A60,
            0x1760EFC3, 0x1E67DF55, 0x056E8EEF, 0x0C69BE79,
            0xEB61B38C, 0xE266831A, 0xF96FD2A0, 0xF068E236,
            0x140C7795, 0x1D0B4703, 0x060216B9, 0x0F05262F,
            0x15BA3BBE, 0x1CBD0B28, 0x07B45A92, 0x0EB36A04,
            0xEAD7FFA7, 0xE3D0CF31, 0xF8D99E8B, 0xF1DEAE1D,
            0x1B64C2B0, 0x1263F226, 0x096AA39C, 0x006D930A,
            0xE40906A9, 0xED0E363F, 0xF6076785, 0xFF005713,
            0xE5BF4A82, 0xECB87A14, 0xF7B12BAE, 0xFEB61B38,
            0x1AD28E9B, 0x13D5BE0D, 0x08DCEFB7, 0x01DBDF21,
            0xE6D3D2D4, 0xEFD4E242, 0xF4DDB3F8, 0xFDDA836E,
            0x19BE16CD, 0x10B9265B, 0x0BB077E1, 0x02B74777,
            0x18085AE6, 0x110F6A70, 0x0A063BCA, 0x03010B5C,
            0xE7659EFF, 0xEE62AE69, 0xF56BFFD3, 0xFC6CCF45,
            0xE00AE278, 0xE90DD2EE, 0xF2048354, 0xFB03B3C2,
            0x1F672661, 0x166016F7, 0x0D69474D, 0x046E77DB,
            0x1ED16A4A, 0x17D65ADC, 0x0CDF0B66, 0x05D83BF0,
            0xE1BCAE53, 0xE8BB9EC5, 0xF3B2CF7F, 0xFAB5FFE9,
            0x1DBDF21C, 0x14BAC28A, 0x0FB39330, 0x06B4A3A6,
            0xE2D03605, 0xEBD70693, 0xF0DE5729, 0xF9D967BF,
            0xE3667A2E, 0xEA614AB8, 0xF1681B02, 0xF86F2B94,
            0x1C0BBE37, 0x150C8EA1, 0x0E05DF1B, 0x0702EF8D
        };

        public static string GetString(TreeNode node)
        {
            var data = node.Data as byte[];
            if (data == null)
                return "";
            int pos = 0;
            return GetString(data, ref pos, data.Length);
        }

        public static string GetString(byte[] data, ref int startIndex, int maxLength)
        {
            int len = Array.IndexOf(data, (byte)0, startIndex, maxLength) - startIndex;
            if (len < 0)
                len = maxLength;
            startIndex += maxLength;
            return Encoding.ASCII.GetString(data, startIndex - maxLength, len);
        }

        public static float GetFloat(byte[] data, ref int pos)
        {
            pos += 4;
            return BitConverter.ToSingle(data, pos - 4);
        }

        public static void WriteFloat(byte[] data, Single input, ref int pos)
        {
            Array.Copy(BitConverter.GetBytes(input), 0, data, pos, 4);
            pos += 4;
        }

        public static Int16 GetShort(byte[] data, ref int pos)
        {
            pos += 2;
            return BitConverter.ToInt16(data, pos - 2);
        }

        public static void WriteShort(byte[] data, Int16 input, ref int pos)
        {
            Array.Copy(BitConverter.GetBytes(input), 0, data, pos, 2);
            pos += 2;
        }

        public static UInt16 GetWord(byte[] data, ref int pos)
        {
            pos += 2;
            return BitConverter.ToUInt16(data, pos - 2);
        }

        public static void WriteWord(byte[] data, UInt16 input, ref int pos)
        {
            Array.Copy(BitConverter.GetBytes(input), 0, data, pos, 2);
            pos += 2;
        }

        public static Int32 GetInt(byte[] data, ref int pos)
        {
            pos += 4;
            return BitConverter.ToInt32(data, pos - 4);
        }

        public static void WriteInt(byte[] data, Int32 input, ref int pos)
        {
            Array.Copy(BitConverter.GetBytes(input), 0, data, pos, 4);
            pos += 4;
        }

        public static UInt32 GetDWord(byte[] data, ref int pos)
        {
            pos += 4;
            return BitConverter.ToUInt32(data, pos - 4);
        }

        public static void WriteDWord(byte[] data, UInt32 input, ref int pos)
        {
            Array.Copy(BitConverter.GetBytes(input), 0, data, pos, 4);
            pos += 4;
        }

        /// <summary>
        ///     Function for calculating the Freelancer data nickname hash.
        ///     Algorithm from flhash.exe by sherlog@t-online.de (2003-06-11)
        /// </summary>
        /// <param name="nickName">String to hash</param>
        /// <returns>Return the hashcode</returns>
        public static uint CreateID(string nickName)
        {
            // ReSharper disable InconsistentNaming
            const uint FLHASH_POLYNOMIAL = 0xA001;
            const int LOGICAL_BITS = 30;
            const int PHYSICAL_BITS = 32;
            // ReSharper restore InconsistentNaming

            // Build the crc lookup table if it hasn't been created
            if (_createIdTable == null)
            {
                _createIdTable = new uint[256];
                for (uint i = 0; i < 256; i++)
                {
                    uint x = i;
                    for (uint bit = 0; bit < 8; bit++)
                        x = ((x & 1) == 1) ? (x >> 1) ^ (FLHASH_POLYNOMIAL << (LOGICAL_BITS - 16)) : x >> 1;
                    _createIdTable[i] = x;
                }
            }

            byte[] tNickName = Encoding.ASCII.GetBytes(nickName.ToLowerInvariant());

            // Calculate the hash.
            uint hash = tNickName.Aggregate<byte, uint>(0, (current, t) => (current >> 8) ^ _createIdTable[(byte)current ^ t]);
            // b0rken because byte swapping is not the same as bit reversing, but 
            // that's just the way it is; two hash bits are shifted out and lost
            hash = (hash >> 24) | ((hash >> 8) & 0x0000FF00) | ((hash << 8) & 0x00FF0000) | (hash << 24);
            hash = (hash >> (PHYSICAL_BITS - LOGICAL_BITS)) | 0x80000000;

            return hash;
        }

        // CRC32 table for Freelancer

        public static uint FLModelCRC(byte[] data)
        {
            uint crc = 0xFFFFFFFF;
            for (uint i = 0; i < data.Length; i++)
                crc = (crc >> 8) ^ Flcrc32Tbl[(byte)crc ^ data[i]];
            return ~crc;
        }


        /// Get a CRC code for a model name. This type of CRC is used in model files.
        /// Many thanks to Anton's CRCTool source for the algorithm and the CRC table!
        public static uint FLModelCRC(string str)
        {
            return FLModelCRC(Encoding.ASCII.GetBytes(str.ToLowerInvariant()));
        }

        /// <summary>
        ///     Convert degrees to radians
        /// </summary>
        /// <param name="angle"></param>
        /// <returns></returns>
        public static double DegreeToRadian(double angle)
        {
            return angle * Math.PI / 180;
        }

        /// <summary>
        ///     Convert radians to degrees
        /// </summary>
        /// <param name="angle"></param>
        /// <returns></returns>
        public static double RadianToDegree(double angle)
        {
            return angle * 180 / Math.PI;
        }

        /// <summary>
        ///     Convert orientation (matrix) to rotation (pitch/yaw/roll).
        ///     Same as what Freelancer does when saving a game.
        /// </summary>
        public static void OrientationToRotation(
            float XX, float XY, float XZ,
            float YX, float YY, float YZ,
            float ZX, float ZY, float ZZ,
            out float pitch, out float yaw, out float roll)
        {
            double p, y, r;
            double h = Math.Sqrt(XX * XX + YX * YX);
            if (h > 1 / 524288.0)
            {
                p = Math.Atan2(ZY, ZZ);
                y = Math.Atan2(-ZX, h);
                r = Math.Atan2(YX, XX);
            }
            else
            {
                p = Math.Atan2(-YZ, YY);
                y = Math.Atan2(-ZX, h);
                r = 0;
            }
            pitch = (float)RadianToDegree(p);
            yaw = (float)RadianToDegree(y);
            roll = (float)RadianToDegree(r);
        }

        // cos(±90°) and sin(±180°) are supposed to be zero, but they end up
        // just being really small, as do some other calculations.  Let's
        // explicitly treat exceedingly small numbers as zero.
        public static float check(double value)
        {
            if (Math.Abs(value) < 1e-12)
                return 0;
            return (float)value;
        }

        /// <summary>
        ///     Translate rotation (pitch/yaw/roll) to orientation (matrix).
        ///     Same as EulerMatrix in common.dll.
        /// </summary>
        /// <param name="pitch"></param>
        /// <param name="yaw"></param>
        /// <param name="roll"></param>
        public static void RotationToOrientation(float pitch, float yaw, float roll,
            out float XX, out float XY, out float XZ,
            out float YX, out float YY, out float YZ,
            out float ZX, out float ZY, out float ZZ)
        {
            // Assuming the angles are in degrees.
            double p = DegreeToRadian(pitch);
            double y = DegreeToRadian(yaw);
            double r = DegreeToRadian(roll);

            double cp = Math.Cos(p);
            double sp = Math.Sin(p);
            double cy = Math.Cos(y);
            double sy = Math.Sin(y);
            double cr = Math.Cos(r);
            double sr = Math.Sin(r);

            XX = check(cy * cr);
            XY = check(sp * sy * cr + cp * -sr);
            XZ = check(cp * sy * cr + -sp * -sr);
            YX = check(cy * sr);
            YY = check(sp * sy * sr + cp * cr);
            YZ = check(cp * sy * sr + -sp * cr);
            ZX = check(-sy);
            ZY = check(sp * cy);
            ZZ = check(cp * cy);
        }

        /// <summary>
        ///     Case-insensitive, multiple string comparison.
        /// </summary>
        public static bool StrIEq(string str, params string[] test)
        {
            return (StrIeqP(str, test) != 0);
        }

        /// <summary>
        ///     Case-insensitive, multiple string comparison.
        /// </summary>
        public static int StrIeqP(string str, params string[] test)
        {
            for (int i = 0; i < test.Length; ++i)
                if (str.Equals(test[i], StringComparison.OrdinalIgnoreCase))
                    return i + 1;
            return 0;
        }

        /// <summary>
        ///     Determine if a character would generate an invalid float value.
        /// </summary>
        /// <param name="val">Existing valid number</param>
        /// <param name="ch">Character to add</param>
        /// <param name="pos">Position of new character</param>
        /// <returns></returns>
        public static bool ValidFloatChar(string val, char ch, int pos)
        {
            int exponent = val.IndexOfAny(new[] { 'e', 'E' });

            if (ch >= '0' && ch <= '9')
            {
                // Restrict exponent to two digits.
                if (exponent != -1 && pos > exponent && exponent != val.Length - 1)
                {
                    ++exponent;
                    if (val[exponent] == '+' || val[exponent] == '-')
                        ++exponent;
                    if (val.Length - exponent > 1)
                        return false;
                }
                // Digits can go anywhere, except before a sign.
                return (pos == val.Length || (val[pos] != '+' && val[pos] != '-'));
            }

            if (ch == '+' || ch == '-')
            {
                // Sign can only occur once, at the start of the number,
                // or after the exponent.
                if (pos == 0 || pos == exponent + 1)
                {
                    return (pos == val.Length || (val[pos] != '+' && val[pos] != '-'));
                }
                return false;
            }

            int point = val.IndexOf(NumberFormatInfo.CurrentInfo.NumberDecimalSeparator, StringComparison.Ordinal);

            if (ch == 'e' || ch == 'E')
            {
                // Exponent can only occur once, after the point, after a digit.
                return (exponent == -1 && pos > point &&
                        pos != 0 && Char.IsDigit(val[pos - 1]));
            }

            if (ch == NumberFormatInfo.CurrentInfo.NumberDecimalSeparator[0])
            {
                // Point can only occur once, and not in the exponent.
                return (point == -1 && (exponent == -1 || pos <= exponent));
            }

            return (ch == '\b' || // Backspace
                    ch == 26 || // Ctrl+Z - Undo
                    ch == 3 || // Ctrl+C - Copy
                    ch == 22 || // Ctrl+V - Paste
                    ch == 24); // Ctrl+X - Cut
        }

        public static bool ValidIntChar(string val, char ch, int pos, bool unsigned)
        {
            var validchar = ch >= '0' && ch <= '9' || ch == 'x' && pos == 1 && val.Length >= 1 && val[0] == '0' && !val.Contains("x");
            // upper: if (ch == 'x' && pos == 1 && val.Length >= 1 && val[0] == '0' && !val.Contains("x")) validchar = true;
            if (val.Length >= 2 && val.Substring(0, 2) == "0x")
            {
                if (ch >= 'a' && ch <= 'f' || ch >= 'A' && ch <= 'F') validchar = true;
            }
            else if (!unsigned && ch == '-' && pos == 0 && !val.Contains("-")) validchar = true;

            // TODO: Validate integer sizes (unsigned and signed)
            if (validchar) return true;

            return (ch == '\b' || // Backspace
                    ch == 26 || // Ctrl+Z - Undo
                    ch == 3 || // Ctrl+C - Copy
                    ch == 22 || // Ctrl+V - Paste
                    ch == 24); // Ctrl+X - Cut
        }

        /// <summary>
        ///     Return the current time since system start in seconds.
        /// </summary>
        /// <returns>Time in seconds</returns>
        public static double GetTime()
        {
            double time = (Stopwatch.GetTimestamp() / (double)Stopwatch.Frequency);
            return time;
        }
    }
}
