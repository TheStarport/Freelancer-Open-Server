using System.IO;
using System.Text;
using FLServer.Physics;

namespace FLServer.DataWorkers
{
    internal class SurFile
    {
        // SUR File Structs compiled by Twex, Phantom Fox, Free Spirit, Dr Del, CCCP, shsan and Skyshooter
        // From FLModelTool and probably originally from Colin Sanby's SUR exporter/importer.

        // Structures
        /// <summary>
        /// </summary>
        /// <param name="filePath"></param>
        public SurFile(string filePath)
        {
            int pos = 0;
            byte[] d;
            using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                d = new byte[fs.Length];
                fs.Read(d, 0, (int) fs.Length);
                fs.Close();
            }

            // Read tag
            byte[] tag1 = FLMsgType.GetArray(d, ref pos, 4);
            float vers = FLMsgType.GetFloat(d, ref pos);

            while (pos < d.Length)
            {
                uint meshid = FLMsgType.GetUInt32(d, ref pos);
                uint count = FLMsgType.GetUInt32(d, ref pos);

                while (count-- > 0)
                {
                    byte[] tag2 = FLMsgType.GetArray(d, ref pos, 4);
                    if (TagCompare(tag2, "surf"))
                    {
                        uint size = FLMsgType.GetUInt32(d, ref pos);
                        ReadSurf(d, ref pos);
                    }
                    else if (TagCompare(tag2, "exts"))
                    {
                        var min = new Vector();
                        var max = new Vector();
                        min.x = FLMsgType.GetFloat(d, ref pos);
                        min.y = FLMsgType.GetFloat(d, ref pos);
                        min.z = FLMsgType.GetFloat(d, ref pos);
                        max.x = FLMsgType.GetFloat(d, ref pos);
                        max.y = FLMsgType.GetFloat(d, ref pos);
                        max.z = FLMsgType.GetFloat(d, ref pos);
                    }
                    else if (TagCompare(tag2, "!fxd"))
                    {
                    }
                    else if (TagCompare(tag2, "hpid"))
                    {
                        uint count2 = FLMsgType.GetUInt32(d, ref pos);
                        while (count2-- > 0)
                        {
                            uint mesh2 = FLMsgType.GetUInt32(d, ref pos);
                        }
                    }
                }
            }
        }

        private bool TagCompare(byte[] tag, string match)
        {
            byte[] tmp = Encoding.ASCII.GetBytes(match);
            for (int i = 0; i < tag.Length; i++)
            {
                if (tag[i] != match[i])
                    return false;
            }
            return true;
        }

        private void ReadSurf(byte[] d, ref int pos) // todo change to ref
        {
            Surface surf = Surface.Read(d, ref pos);

            int bits_beg = pos + (int) surf.bits_start - Surface.SIZE;
            int bits_end = pos + (int) surf.bits_end - Surface.SIZE;

            bool done = false;
            do
            {
                THeader th = THeader.Read(d, ref pos);
                for (int i = 0; i < th.th_TriangleCount; i++)
                {
                    Triangle tri = Triangle.Read(d, ref pos);
                }
                done = (th.th_VertArrayOffset == (THeader.SIZE + Triangle.SIZE*th.th_TriangleCount));
            } while (!done);

            while (pos < bits_beg)
            {
                Vertex vert = Vertex.Read(d, ref pos);
            }

            while (pos < bits_end)
            {
                BitHeader bh = BitHeader.Read(d, ref pos);
            }
        }

        private class BitHeader
        {
            public const int SIZE = 28;
            public readonly Vector m_Centre = new Vector();
            public readonly byte[] m_Scale = new byte[3]; // each is multiplied by radius, compared with centre
            public float m_Radius;
            public int m_offset_to_next_sibling;
            public int m_offset_to_triangles;

            public static BitHeader Read(byte[] d, ref int pos)
            {
                var o = new BitHeader();
                o.m_offset_to_next_sibling = FLMsgType.GetInt32(d, ref pos);
                o.m_offset_to_triangles = FLMsgType.GetInt32(d, ref pos);
                o.m_Centre.x = FLMsgType.GetFloat(d, ref pos);
                o.m_Centre.y = FLMsgType.GetFloat(d, ref pos);
                o.m_Centre.z = FLMsgType.GetFloat(d, ref pos);
                o.m_Radius = FLMsgType.GetFloat(d, ref pos);
                o.m_Scale[0] = FLMsgType.GetUInt8(d, ref pos);
                o.m_Scale[1] = FLMsgType.GetUInt8(d, ref pos);
                o.m_Scale[2] = FLMsgType.GetUInt8(d, ref pos);
                pos += 1; // padding
                return o;
            }
        }

        private class Side
        {
            public bool flag;
            public ushort offset;
            public ushort vertex;

            public static Side Read(byte[] d, ref int pos)
            {
                var o = new Side();
                o.vertex = FLMsgType.GetUInt16(d, ref pos);
                ushort arg = FLMsgType.GetUInt16(d, ref pos);
                o.offset = (ushort) (arg & 0x7FFF);
                o.flag = ((arg >> 15) & 1) == 1;
                return o;
            }
        }

        private class Surface
        {
            public const int SIZE = 48;

            public readonly Vector center = new Vector();
            public readonly Vector inertia = new Vector();
            public uint bits_end;
            public uint bits_start; // number of bytes to start of bits section
            public float radius;
            public uint scale; // some sort of multiplier for the radius

            public static Surface Read(byte[] d, ref int pos)
            {
                var o = new Surface();
                o.center.x = FLMsgType.GetFloat(d, ref pos);
                o.center.y = FLMsgType.GetFloat(d, ref pos);
                o.center.z = FLMsgType.GetFloat(d, ref pos);
                o.inertia.x = FLMsgType.GetFloat(d, ref pos);
                o.inertia.y = FLMsgType.GetFloat(d, ref pos);
                o.inertia.z = FLMsgType.GetFloat(d, ref pos);
                o.radius = FLMsgType.GetFloat(d, ref pos);
                o.scale = FLMsgType.GetUInt8(d, ref pos);
                o.bits_end = FLMsgType.GetUInt24(d, ref pos);
                o.bits_start = FLMsgType.GetUInt32(d, ref pos);
                pos += 12; // padding
                return o;
            }
        }

        // Triangle Group Header
        private struct THeader
        {
            public const int SIZE = 16;
            public uint th_MeshID;
            public uint th_QtyRefVerts;
            public short th_TriangleCount;
            public uint th_Type;
            public uint th_VertArrayOffset;

            public static THeader Read(byte[] d, ref int pos)
            {
                var o = new THeader();

                o.th_VertArrayOffset = FLMsgType.GetUInt32(d, ref pos);
                o.th_MeshID = FLMsgType.GetUInt32(d, ref pos);
                o.th_Type = FLMsgType.GetUInt8(d, ref pos);
                o.th_QtyRefVerts = FLMsgType.GetUInt24(d, ref pos);
                o.th_TriangleCount = (short) FLMsgType.GetUInt16(d, ref pos);
                pos += 2; //padding
                return o;
            }
        }

        private class Triangle
        {
            public const int SIZE = 16;
            public readonly Side[] m_Vertex = new Side[3];

            public uint m_TriNumber;
            public uint m_flag;
            public uint m_tri_op;
            public uint m_unk; // tested for zero (which they all are), but not used

            public static Triangle Read(byte[] d, ref int pos)
            {
                var o = new Triangle();
                uint arg = FLMsgType.GetUInt32(d, ref pos);
                o.m_TriNumber = (arg >> 0) & 0xFFF;
                o.m_tri_op = (arg >> 12) & 0xFFF;
                o.m_unk = (arg >> 24) & 0x7F;
                o.m_flag = arg >> 31;

                o.m_Vertex[0] = Side.Read(d, ref pos);
                o.m_Vertex[1] = Side.Read(d, ref pos);
                o.m_Vertex[2] = Side.Read(d, ref pos);
                return o;
            }
        }

        private class Vertex
        {
            public readonly Vector point = new Vector();
            public uint mesh;

            public static Vertex Read(byte[] d, ref int pos)
            {
                var o = new Vertex();
                o.point.x = FLMsgType.GetFloat(d, ref pos);
                o.point.y = FLMsgType.GetFloat(d, ref pos);
                o.point.z = FLMsgType.GetFloat(d, ref pos);
                o.mesh = FLMsgType.GetUInt32(d, ref pos);
                return o;
            }
        }
    }
}