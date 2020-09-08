using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace FOS_ng.Structures
{
    internal class UTFFile
    {
        private List<string> _partnames = new List<string>();

        /// <summary>
        ///     The list of hardpoints.
        /// </summary>
        public TreeNode Hardpoints { get; private set; }

        /// <summary>
        ///     The list of parts.
        /// </summary>
        public TreeNode Parts { get; private set; }

        /// <summary>
        ///     Try to load a UTF file. Throw an exception on failure.
        /// </summary>
        /// <param name="filePath">The file to load.</param>
        public TreeNode LoadUTFFile(string filePath)
        {
            byte[] buf;

            using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                buf = new byte[fs.Length];
                fs.Read(buf, 0, (int)fs.Length);
                fs.Close();
            }

            var pos = 0;
            var sig = Utilities.GetInt(buf, ref pos);
            var ver = Utilities.GetInt(buf, ref pos);
            if (sig != 0x20465455 || ver != 0x101)
                throw new Exception("Unsupported");

            // get node chunk info
            var nodeBlockOffset = Utilities.GetInt(buf, ref pos);
            var nodeSize = Utilities.GetInt(buf, ref pos);

            var unknown1 = Utilities.GetInt(buf, ref pos);
            var header_size = Utilities.GetInt(buf, ref pos);

            // get string chunk info
            var stringBlockOffset = Utilities.GetInt(buf, ref pos);
            var stringBlockSize = Utilities.GetInt(buf, ref pos);
            pos += 4;

            // get data chunk info
            var dataBlockOffset = Utilities.GetInt(buf, ref pos);

            // A dummy root node that we throw away.
            var dummyRoot = new TreeNode {Name = "DUMMYROOT"};

            // A node to store all the hardpoints.
            Hardpoints = new TreeNode {Name = "Hardpoints"};

            // A node to store all the parts.
            Parts = new TreeNode {Name = "Parts"};

            // Load the nodes recursively.
            ParseNode(buf, nodeBlockOffset, 0, stringBlockOffset, dataBlockOffset, dummyRoot);

            return dummyRoot.Values.First();
        }

        /// <summary>
        ///     Decode a node from the UTF file.
        /// </summary>
        /// <param name="buf">The byte array from the file.</param>
        /// <param name="nodeBlockStart">The offset in bytes to the start of the node block in buf</param>
        /// <param name="nodeStart">The offset to the current parent node in the node block</param>
        /// <param name="stringBlockOffset">The offset in bytes to the start of the string block in buf</param>
        /// <param name="dataBlockOffset">The offset in bytes to the start of the data block in buf</param>
        /// <param name="parent">The parent TreeNode</param>
        private void ParseNode(byte[] buf, int nodeBlockStart, int nodeStart, int stringBlockOffset, int dataBlockOffset,
            TreeNode parent)
        {
            int offset = nodeBlockStart + nodeStart;

            while (true)
            {
                int nodeOffset = offset;

                int peerOffset = Utilities.GetInt(buf, ref offset); // next node on same level
                int nameOffset = Utilities.GetInt(buf, ref offset); // string for this node
                int flags = Utilities.GetInt(buf, ref offset); // bit 4 set = intermediate, bit 7 set = leaf
                int zero = Utilities.GetInt(buf, ref offset); // always seems to be zero
                int childOffset = Utilities.GetInt(buf, ref offset);
                // next node in if intermediate, offset to data if leaf
                int allocatedSize = Utilities.GetInt(buf, ref offset); // leaf node only, 0 for intermediate
                int size = Utilities.GetInt(buf, ref offset); // leaf node only, 0 for intermediate
                int size2 = Utilities.GetInt(buf, ref offset); // leaf node only, 0 for intermediate
                int u1 = Utilities.GetInt(buf, ref offset); // timestamps. can be zero
                int u2 = Utilities.GetInt(buf, ref offset);
                int u3 = Utilities.GetInt(buf, ref offset);

                // Extract the node name
                int len = 0;

                // TODO: that's mad bro.
                // ReSharper disable once EmptyEmbeddedStatement
                for (int i = stringBlockOffset + nameOffset; i < buf.Length && buf[i] != 0; i++, len++) ;
                string name = Encoding.ASCII.GetString(buf, stringBlockOffset + nameOffset, len);

                // Extract data if this is a leaf.
                byte[] data;
                if ((flags & 0xFF) == 0x80)
                {
                    if (size != size2)
                        MessageBox.Show(@"Possible compression being used on " + name, @"Warning");

                    data = new byte[size];
                    Buffer.BlockCopy(buf, childOffset + dataBlockOffset, data, 0, size);
                }
                else
                {
                    data = new byte[0];
                }

                var node = new TreeNode {Data = data, Name = name};
                parent.Add(name,node);

                if (childOffset > 0 && flags == 0x10)
                    ParseNode(buf, nodeBlockStart, childOffset, stringBlockOffset, dataBlockOffset, node);
                if (Utilities.StrIEq(parent.Name, "Fixed", "Revolute"))
                    //TODO: test
                    //Hardpoints.Nodes.Add(name, name);
                    Hardpoints.Add(name, new TreeNode{Name = name});
                else if (Utilities.StrIEq(name, "Fix", "Trans", "Loose"))
                    AddParts(0xB0, data);
                else if (Utilities.StrIEq(name, "Pris", "Rev"))
                    AddParts(0xD0, data);
                else if (Utilities.StrIEq(name, "Sphere"))
                    AddParts(0xD4, data);

                if (peerOffset == 0)
                    break;

                offset = nodeBlockStart + peerOffset;
            }
        }

        /// <summary>
        ///     Add the parent and child names from the Cons(truct) nodes.
        /// </summary>
        /// <param name="size">size of each part</param>
        /// <param name="data">list of parts</param>
        private void AddParts(int size, byte[] data)
        {
            for (int i = 0; i < data.Length; i += size - 128)
            {
                string parent = Utilities.GetString(data, ref i, 64);
                string child = Utilities.GetString(data, ref i, 64);
                TreeNode nodes = Parts.Find(parent, true);
                TreeNode node;
                if (nodes == null)
                {
                    nodes = Parts.Find(child, true);
                    if (nodes == null)
                    {
                        node = Parts.Add(parent, parent);
                    }
                    else
                    {
                        //nodes is Child node now
                        node = nodes.Parent;
                        node.Remove(nodes.Name);
                        node = node.Add(parent, parent);
                        node.Add(nodes.Name,nodes);
                        return;
                    }
                }
                else
                {
                    node = nodes;
                }
                node.Add(child, child);
            }
        }

        /// <summary>
        ///     Save the data in the treeview displayed by this form back into
        ///     the specified file.
        /// </summary>
        /// <param name="filename"></param>
        public void SaveUTFFile(TreeNode root, string filename)
        {
            var stringBlock = new List<byte>();
            var nodeBlock = new List<byte>();
            var dataBlock = new List<byte>();

            // Build the string block with duplicate strings removed to
            // reduce the file size.
            var nameOffsets = new Dictionary<string, int>();
            BuildStringBlock(nameOffsets, stringBlock, root);

            // A dummy root node that we throw away.
            var dummyRoot = new TreeNode {Name = "DUMMYROOT"};
            dummyRoot.Add((TreeNode)(root.Clone()));

            // Built the nodes and data blocks
            BuildNode(nodeBlock, dataBlock, nameOffsets, dummyRoot);

            // Write the file.
            using (FileStream fs = File.Create(filename))
            {
                var w = new BinaryWriter(fs);

                const int nodeOffset = 0x38; // right after the header
                int stringOffset = nodeOffset + nodeBlock.Count;
                int stringBlockAlloc = (stringBlock.Count + 3) & ~3;
                int dataOffset = stringOffset + stringBlockAlloc;

                const int sig = 0x20465455;
                const int ver = 0x101;
                w.Write(sig);
                w.Write(ver);

                w.Write(nodeOffset);
                w.Write(nodeBlock.Count);
                w.Write(0);
                w.Write(0x2c);
                w.Write(stringOffset);
                w.Write(stringBlockAlloc);
                w.Write(stringBlock.Count);
                w.Write(dataOffset);
                w.Write(0);
                w.Write(0);
                w.Write(DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Utc).ToFileTime());

                w.Write(nodeBlock.ToArray());
                w.Write(stringBlock.ToArray());
                for (int i = stringBlock.Count; i < stringBlockAlloc; ++i)
                    w.Write((byte)0);
                w.Write(dataBlock.ToArray());

                fs.Close();
            }
        }

        /// <summary>
        ///     Add the children of a node to the relevant blocks.
        /// </summary>
        /// <param name="nodeBlock"></param>
        /// <param name="dataBlock"></param>
        /// <param name="nameOffsets">Array of string-index offsets in the string block</param>
        /// <param name="anode">The node to parse.</param>
        /// <returns>Offset to first byte in node block of the first node added to the node block</returns>
        private int BuildNode(List<byte> nodeBlock,
            List<byte> dataBlock,
            Dictionary<string, int> nameOffsets,
            TreeNode anode)
        {
            int firstNodeOffset = nodeBlock.Count;

            DateTime now = DateTime.Now;
            int datetime = ((now.Year - 1980) << 9) | (now.Month << 5) | (now.Day) |
                           (now.Hour << 27) | (now.Minute << 21) | ((now.Second >> 1) << 16);

            TreeNode childNode = anode.FirstNode();
            do
            {
                string name = childNode.Name;
                var data = childNode.Data as byte[];
                int allocLength = (data.Length + 3) & ~3;

                if (childNode.Count == 0)
                {
                    int thisNodeStart = nodeBlock.Count();

                    nodeBlock.AddRange(BitConverter.GetBytes(0)); // peer offset
                    nodeBlock.AddRange(BitConverter.GetBytes(nameOffsets[name]));
                    nodeBlock.AddRange(BitConverter.GetBytes(0x80)); // flags
                    nodeBlock.AddRange(BitConverter.GetBytes(0)); // zero
                    nodeBlock.AddRange(BitConverter.GetBytes(dataBlock.Count)); // data offset
                    dataBlock.AddRange(data);
                    for (int i = data.Length; i < allocLength; ++i)
                        dataBlock.Add(0);
                    nodeBlock.AddRange(BitConverter.GetBytes(allocLength)); // allocated data size
                    nodeBlock.AddRange(BitConverter.GetBytes(data.Length)); // data size
                    nodeBlock.AddRange(BitConverter.GetBytes(data.Length)); // data size2
                    nodeBlock.AddRange(BitConverter.GetBytes(datetime));
                    nodeBlock.AddRange(BitConverter.GetBytes(datetime));
                    nodeBlock.AddRange(BitConverter.GetBytes(datetime));

                    if (childNode.NextNode() != null)
                    {
                        int peerOffset = nodeBlock.Count;
                        nodeBlock.RemoveRange(thisNodeStart + 0, 4);
                        nodeBlock.InsertRange(thisNodeStart + 0, BitConverter.GetBytes(peerOffset));
                    }
                }
                else
                {
                    int thisNodeStart = nodeBlock.Count();

                    nodeBlock.AddRange(BitConverter.GetBytes(0)); // peer offset
                    nodeBlock.AddRange(BitConverter.GetBytes(nameOffsets[childNode.Name]));
                    nodeBlock.AddRange(BitConverter.GetBytes(0x10)); // flags
                    nodeBlock.AddRange(BitConverter.GetBytes(0)); // zero
                    nodeBlock.AddRange(BitConverter.GetBytes(0)); // child offset
                    nodeBlock.AddRange(BitConverter.GetBytes(0)); // allocated data size
                    nodeBlock.AddRange(BitConverter.GetBytes(0)); // data size
                    nodeBlock.AddRange(BitConverter.GetBytes(0)); // data size2
                    nodeBlock.AddRange(BitConverter.GetBytes(datetime));
                    nodeBlock.AddRange(BitConverter.GetBytes(datetime));
                    nodeBlock.AddRange(BitConverter.GetBytes(datetime));

                    int childNodeOffset = BuildNode(nodeBlock, dataBlock, nameOffsets, childNode);

                    if (childNode.NextNode() != null)
                    {
                        int peerOffset = nodeBlock.Count;
                        nodeBlock.RemoveRange(thisNodeStart + 0, 4);
                        nodeBlock.InsertRange(thisNodeStart + 0, BitConverter.GetBytes(peerOffset));
                    }

                    nodeBlock.RemoveRange(thisNodeStart + 16, 4);
                    nodeBlock.InsertRange(thisNodeStart + 16, BitConverter.GetBytes(childNodeOffset));
                }

                childNode = childNode.NextNode();
            } while (childNode != null);

            return firstNodeOffset;
        }

        /// <summary>
        ///     Scan the node names and produce a unique list of string and encode this into the string
        ///     array in the UTF file. Keep a record of string name to offets into the array.
        /// </summary>
        private void BuildStringBlock(Dictionary<string, int> names, List<byte> stringBlock, TreeNode parent)
        {
            if (!names.ContainsKey(parent.Name))
            {
                names[parent.Name] = stringBlock.Count;
                stringBlock.AddRange(Encoding.ASCII.GetBytes(parent.Name));
                stringBlock.Add(0);
            }

            foreach (var node in parent.Values)
            {
                // If this node has children then check the children
                if (node.Count > 0)
                {
                    BuildStringBlock(names, stringBlock, node);
                }
                // Otherwise add the node name to the string list.
                else
                {
                    if (!names.ContainsKey(node.Name))
                    {
                        names[node.Name] = stringBlock.Count;
                        stringBlock.AddRange(Encoding.ASCII.GetBytes(node.Name));
                        stringBlock.Add(0);
                    }
                }
            }
        }
    }
}
