using System.Windows.Forms;
using FLServer.Physics;


namespace FLServer.Old
{
    public class HardpointData
    {
        private readonly string _name;
        public float Max;
        public float Min;
        public Vector Axis = new Vector();
        public Vector Position = new Vector();
        public bool Revolute;
        public Matrix Rotation = new Matrix();

        public HardpointData(TreeNode hardpoint)
        {
            _name = hardpoint.Name;

            var data = Read(hardpoint, "Position", 3);
            Position.x = data[0];
            Position.y = data[1];
            Position.z = data[2];

            data = Read(hardpoint, "Orientation", 9);
            Rotation.M00 = data[0];
            Rotation.M01 = data[1];
            Rotation.M02 = data[2];
            Rotation.M10 = data[3];
            Rotation.M11 = data[4];
            Rotation.M12 = data[5];
            Rotation.M20 = data[6];
            Rotation.M21 = data[7];
            Rotation.M22 = data[8];

            if (Utilities.StrIEq(hardpoint.Parent.Name, "Revolute"))
            {
                Revolute = true;

                data = Read(hardpoint, "Axis", 3);
                Axis.x = data[0];
                Axis.y = data[1];
                Axis.z = data[2];

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