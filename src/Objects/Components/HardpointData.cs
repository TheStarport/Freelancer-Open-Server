using System.Windows.Forms;
using Jitter.LinearMath;

namespace FLServer.Objects
{
    public class HardpointData
    {
        private readonly string _name;
        public float Max;
        public float Min;
        public JVector Axis = new JVector();
        public JVector Position = new JVector();
        public bool Revolute;
        public JMatrix Rotation = new JMatrix();

        public HardpointData(TreeNode hardpoint)
        {
            _name = hardpoint.Name;

            var data = Read(hardpoint, "Position", 3);
            Position.X = data[0];
            Position.Y = data[1];
            Position.Z = data[2];

            data = Read(hardpoint, "Orientation", 9);
            Rotation.M11 = data[0];
            Rotation.M12 = data[1];
            Rotation.M13 = data[2];
            Rotation.M21 = data[3];
            Rotation.M22 = data[4];
            Rotation.M23 = data[5];
            Rotation.M31 = data[6];
            Rotation.M32 = data[7];
            Rotation.M33 = data[8];

            if (Utilities.StrIEq(hardpoint.Parent.Name, "Revolute"))
            {
                Revolute = true;

                data = Read(hardpoint, "Axis", 3);
                Axis.X = data[0];
                Axis.Y = data[1];
                Axis.Z = data[2];

                data = Read(hardpoint, "Min", 1);
                Min = data[0];

                data = Read(hardpoint, "Max", 1);
                Max = data[0];
            }
            else
                Revolute = false;
        }

        public string Name
        {
            get { return _name; }
        }

        private static float[] Read(TreeNode hardpoint, string name, int count)
        {
            var val = new float[count];
            try
            {
                var node = hardpoint.Nodes[name];
                var data = node.Tag as byte[];
                var pos = 0;

                for (var i = 0; i < count; ++i)
                    val[i] = Utilities.GetFloat(data, ref pos);
            }
            catch
            {
            }
            return val;
        }
    };
}