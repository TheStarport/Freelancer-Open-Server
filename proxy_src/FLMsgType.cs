using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FLOpenServerProxy
{
    class FLMsgType
    {
        public static float GetFloat(byte[] msg, ref int pos)
        {
            float value = BitConverter.ToSingle(msg, pos);
            pos += 4;
            return value;
        }

        public static string GetArray(byte[] msg, ref int pos, int len)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = pos; i < (pos + len); i++)
                sb.AppendFormat("{0:x2}.", msg[i]);
            pos += len;
            return sb.ToString();
        }

        public static uint GetUInt32(byte[] msg, ref int pos)
        {
            uint value = (((uint)msg[pos + 3]) << 24) | (((uint)msg[pos + 2]) << 16) | (((uint)msg[pos + 1]) << 8) | (uint)msg[pos];
            pos += 4;
            return value;
        }

        public static uint GetUInt16(byte[] msg, ref int pos)
        {
            uint value = (((uint)msg[pos + 1]) << 8) | (uint)msg[pos];
            pos += 2;
            return value;
        }

        public static byte GetUInt8(byte[] msg, ref int pos)
        {
            byte value = msg[pos];
            pos += 1;
            return value;
        }

        public static int GetInt8(byte[] msg, ref int pos) // TODO, test this
        {
            int value;
            if (msg[pos] > 127)
                value = (msg[pos] & 0x7f) - 128;
            else
                value = msg[pos];
            pos += 1;
            return value;
        }

        public static int GetInt16(byte[] msg, ref int pos)
        {
            int value = BitConverter.ToInt16(msg, pos);
            pos += 2;
            return value;
        }

        public static int GetInt32(byte[] msg, ref int pos)
        {
            int value = BitConverter.ToInt32(msg, pos);
            pos += 4;
            return value;
        }

        public static string GetAsciiStringLen8(byte[] msg, ref int pos)
        {
            int len = (int)GetUInt8(msg, ref pos);
            string value = new System.Text.ASCIIEncoding().GetString(msg, pos, len); pos += len;
            if (len > 0 && value[len - 1] == 0x0000)
                value = value.Substring(0, len - 1);
            return value;
        }

        public static string GetAsciiStringLen16(byte[] msg, ref int pos)
        {
            int len = (int)GetUInt16(msg, ref pos);
            string value = new System.Text.ASCIIEncoding().GetString(msg, pos, len); pos += len;
            if (len > 0 && value[len - 1] == 0x0000)
                value = value.Substring(0, len - 1);
            return value;
        }

        public static string GetAsciiStringLen32(byte[] msg, ref int pos)
        {
            int len = (int)GetUInt32(msg, ref pos);
            string value = new System.Text.ASCIIEncoding().GetString(msg, pos, len); pos += len;
            if (len > 0 && value[len - 1] == 0x0000)
                value = value.Substring(0, len - 1);
            return value;
        }


        public static string GetUnicodeString(byte[] msg, ref int pos)
        {
            int len = (int)GetUInt16(msg, ref pos); len *= 2;
            string value = new System.Text.UnicodeEncoding().GetString(msg, pos, len); pos += len;
            return value;
        }

        public static void AddArray(ref byte[] msg, byte[] tmp)
        {
            int len = msg.Length;
            Array.Resize(ref msg, len + tmp.Length);
            Array.Copy(tmp, 0, msg, len, tmp.Length);
        }

        public static void AddFloat(ref byte[] msg, float value)
        {
            byte[] tmp = BitConverter.GetBytes(value);
            int len = msg.Length;
            Array.Resize(ref msg, len + tmp.Length);
            Array.Copy(tmp, 0, msg, len, tmp.Length);
        }

        public static void AddDouble(ref byte[] msg, double value)
        {
            byte[] tmp = BitConverter.GetBytes(value);
            int len = msg.Length;
            Array.Resize(ref msg, len + tmp.Length);
            Array.Copy(tmp, 0, msg, len, tmp.Length);
        }


        public static void AddUInt8(ref byte[] msg, uint value)
        {
            byte[] tmp = new byte[1];
            tmp[0] = (byte)(value & 0xFF);
            int len = msg.Length;
            Array.Resize(ref msg, len + 1);
            Array.Copy(tmp, 0, msg, len, 1);
        }

        public static void AddInt8(ref byte[] msg, int value)
        {
            byte[] tmp = new byte[1];
            tmp[0] = (byte)(value & 0xFF);
            int len = msg.Length;
            Array.Resize(ref msg, len + 1);
            Array.Copy(tmp, 0, msg, len, 1);
        }


        public static void AddUInt16(ref byte[] msg, uint value)
        {
            byte[] tmp = new byte[2];
            tmp[0] = (byte)(value & 0xFF);
            tmp[1] = (byte)((value >> 8) & 0xFF);
            int len = msg.Length;
            Array.Resize(ref msg, len + 2);
            Array.Copy(tmp, 0, msg, len, 2);
        }

        public static void AddUInt32(ref byte[] msg, uint value)
        {
            byte[] tmp = new byte[4];
            tmp[0] = (byte)(value & 0xFF);
            tmp[1] = (byte)((value >> 8) & 0xFF);
            tmp[2] = (byte)((value >> 16) & 0xFF);
            tmp[3] = (byte)((value >> 24) & 0xFF);
            int len = msg.Length;
            Array.Resize(ref msg, len + 4);
            Array.Copy(tmp, 0, msg, len, 4);
        }
        public static void AddInt16(ref byte[] msg, int value)
        {
            byte[] tmp = new byte[2];
            tmp[0] = (byte)(value & 0xFF);
            tmp[1] = (byte)((value >> 8) & 0xFF);
            int len = msg.Length;
            Array.Resize(ref msg, len + 2);
            Array.Copy(tmp, 0, msg, len, 2);
        }

        public static void AddInt32(ref byte[] msg, int value)
        {
            byte[] tmp = new byte[4];
            tmp[0] = (byte)(value & 0xFF);
            tmp[1] = (byte)((value >> 8) & 0xFF);
            tmp[2] = (byte)((value >> 16) & 0xFF);
            tmp[3] = (byte)((value >> 24) & 0xFF);
            int len = msg.Length;
            Array.Resize(ref msg, len + 4);
            Array.Copy(tmp, 0, msg, len, 4);
        }

        public static void AddUnicodeStringLen0(ref byte[] msg, string value)
        {
            byte[] tmp = ASCIIEncoding.Unicode.GetBytes(value);

            int len = msg.Length;
            Array.Resize(ref msg, len + tmp.Length);
            Array.Copy(tmp, 0, msg, len, tmp.Length);
        }

        public static void AddUnicodeStringLen32(ref byte[] msg, string value)
        {
            AddUInt32(ref msg, (uint)value.Length);
            byte[] tmp = ASCIIEncoding.Unicode.GetBytes(value);

            int len = msg.Length;
            Array.Resize(ref msg, len + tmp.Length);
            Array.Copy(tmp, 0, msg, len, tmp.Length);
        }
        
        public static void AddUnicodeStringLen16(ref byte[] msg, string value)
        {
            AddUInt16(ref msg, (uint)value.Length);
            byte[] tmp = ASCIIEncoding.Unicode.GetBytes(value);

            int len = msg.Length;
            Array.Resize(ref msg, len + tmp.Length);
            Array.Copy(tmp, 0, msg, len, tmp.Length);
        }

        public static void AddUnicodeStringLen8(ref byte[] msg, string value)
        {
            AddUInt8(ref msg, (uint)value.Length);
            byte[] tmp = ASCIIEncoding.Unicode.GetBytes(value);

            int len = msg.Length;
            Array.Resize(ref msg, len + tmp.Length);
            Array.Copy(tmp, 0, msg, len, tmp.Length);
        }

        public static void AddAsciiStringLen0(ref byte[] msg, string value)
        {
            byte[] tmp = ASCIIEncoding.ASCII.GetBytes(value);
            int len = msg.Length;
            Array.Resize(ref msg, len + tmp.Length);
            Array.Copy(tmp, 0, msg, len, tmp.Length);
        }
        
        public static void AddAsciiStringLen8(ref byte[] msg, string value)
        {
            AddUInt8(ref msg, (uint)value.Length);
            byte[] tmp = ASCIIEncoding.ASCII.GetBytes(value);
            int len = msg.Length;
            Array.Resize(ref msg, len + tmp.Length);
            Array.Copy(tmp, 0, msg, len, tmp.Length);
        }

        public static void AddAsciiStringLen16(ref byte[] msg, string value)
        {
            AddUInt16(ref msg, (uint)value.Length);
            byte[] tmp = ASCIIEncoding.ASCII.GetBytes(value);
            int len = msg.Length;
            Array.Resize(ref msg, len + tmp.Length);
            Array.Copy(tmp, 0, msg, len, tmp.Length);
        }

        public static void AddAsciiStringLen32(ref byte[] msg, string value)
        {
            AddUInt32(ref msg, (uint)value.Length);
            byte[] tmp = ASCIIEncoding.ASCII.GetBytes(value);
            int len = msg.Length;
            Array.Resize(ref msg, len + tmp.Length);
            Array.Copy(tmp, 0, msg, len, tmp.Length);
        }
        
        static uint[] crctbl = {
                0x00000000,0x00A00100,0x01400200,0x01E00300,0x02800400,0x02200500,0x03C00600,0x03600700,
                0x05000800,0x05A00900,0x04400A00,0x04E00B00,0x07800C00,0x07200D00,0x06C00E00,0x06600F00,
                0x0A001000,0x0AA01100,0x0B401200,0x0BE01300,0x08801400,0x08201500,0x09C01600,0x09601700,
                0x0F001800,0x0FA01900,0x0E401A00,0x0EE01B00,0x0D801C00,0x0D201D00,0x0CC01E00,0x0C601F00,
                0x14002000,0x14A02100,0x15402200,0x15E02300,0x16802400,0x16202500,0x17C02600,0x17602700,
                0x11002800,0x11A02900,0x10402A00,0x10E02B00,0x13802C00,0x13202D00,0x12C02E00,0x12602F00,
                0x1E003000,0x1EA03100,0x1F403200,0x1FE03300,0x1C803400,0x1C203500,0x1DC03600,0x1D603700,
                0x1B003800,0x1BA03900,0x1A403A00,0x1AE03B00,0x19803C00,0x19203D00,0x18C03E00,0x18603F00,
                0x28004000,0x28A04100,0x29404200,0x29E04300,0x2A804400,0x2A204500,0x2BC04600,0x2B604700,
                0x2D004800,0x2DA04900,0x2C404A00,0x2CE04B00,0x2F804C00,0x2F204D00,0x2EC04E00,0x2E604F00,
                0x22005000,0x22A05100,0x23405200,0x23E05300,0x20805400,0x20205500,0x21C05600,0x21605700,
                0x27005800,0x27A05900,0x26405A00,0x26E05B00,0x25805C00,0x25205D00,0x24C05E00,0x24605F00,
                0x3C006000,0x3CA06100,0x3D406200,0x3DE06300,0x3E806400,0x3E206500,0x3FC06600,0x3F606700,
                0x39006800,0x39A06900,0x38406A00,0x38E06B00,0x3B806C00,0x3B206D00,0x3AC06E00,0x3A606F00,
                0x36007000,0x36A07100,0x37407200,0x37E07300,0x34807400,0x34207500,0x35C07600,0x35607700,
                0x33007800,0x33A07900,0x32407A00,0x32E07B00,0x31807C00,0x31207D00,0x30C07E00,0x30607F00,
                0x50008000,0x50A08100,0x51408200,0x51E08300,0x52808400,0x52208500,0x53C08600,0x53608700,
                0x55008800,0x55A08900,0x54408A00,0x54E08B00,0x57808C00,0x57208D00,0x56C08E00,0x56608F00,
                0x5A009000,0x5AA09100,0x5B409200,0x5BE09300,0x58809400,0x58209500,0x59C09600,0x59609700,
                0x5F009800,0x5FA09900,0x5E409A00,0x5EE09B00,0x5D809C00,0x5D209D00,0x5CC09E00,0x5C609F00,
                0x4400A000,0x44A0A100,0x4540A200,0x45E0A300,0x4680A400,0x4620A500,0x47C0A600,0x4760A700,
                0x4100A800,0x41A0A900,0x4040AA00,0x40E0AB00,0x4380AC00,0x4320AD00,0x42C0AE00,0x4260AF00,
                0x4E00B000,0x4EA0B100,0x4F40B200,0x4FE0B300,0x4C80B400,0x4C20B500,0x4DC0B600,0x4D60B700,
                0x4B00B800,0x4BA0B900,0x4A40BA00,0x4AE0BB00,0x4980BC00,0x4920BD00,0x48C0BE00,0x4860BF00,
                0x7800C000,0x78A0C100,0x7940C200,0x79E0C300,0x7A80C400,0x7A20C500,0x7BC0C600,0x7B60C700,
                0x7D00C800,0x7DA0C900,0x7C40CA00,0x7CE0CB00,0x7F80CC00,0x7F20CD00,0x7EC0CE00,0x7E60CF00,
                0x7200D000,0x72A0D100,0x7340D200,0x73E0D300,0x7080D400,0x7020D500,0x71C0D600,0x7160D700,
                0x7700D800,0x77A0D900,0x7640DA00,0x76E0DB00,0x7580DC00,0x7520DD00,0x74C0DE00,0x7460DF00,
                0x6C00E000,0x6CA0E100,0x6D40E200,0x6DE0E300,0x6E80E400,0x6E20E500,0x6FC0E600,0x6F60E700,
                0x6900E800,0x69A0E900,0x6840EA00,0x68E0EB00,0x6B80EC00,0x6B20ED00,0x6AC0EE00,0x6A60EF00,
                0x6600F000,0x66A0F100,0x6740F200,0x67E0F300,0x6480F400,0x6420F500,0x65C0F600,0x6560F700,
                0x6300F800,0x63A0F900,0x6240FA00,0x62E0FB00,0x6180FC00,0x6120FD00,0x60C0FE00,0x6060FF00};

        public static string FLNameToFile(string idstr)
        {
            byte[] accid = ASCIIEncoding.Unicode.GetBytes(idstr.ToLowerInvariant());
            uint hash = 0;
            for (int pos = 0; pos < accid.Length; pos++)
            {
                hash = crctbl[(hash & 0xFF) ^ (uint)accid[pos]] ^ (hash >> 8);
            }
            uint result = (hash & 0xFF000000) >> 24 | (hash & 0x00FF0000) >> 8 | (hash & 0x0000FF00) << 8 | (hash & 0x000000FF) << 24;
            return String.Format("{0:x2}-{1:x8}", idstr.Length, result);
        }
    }
}
