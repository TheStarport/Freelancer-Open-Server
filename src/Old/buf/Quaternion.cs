using System;

namespace FLServer
{
    public class Quaternion
    {
        public double I, J, K, W;

        public Quaternion()
        {
            W = 1;
        }

        public Quaternion(double i, double j, double k, double w)
        {
            I = i;
            J = j;
            K = k;
            W = w;
        }

        public Quaternion(double w, Vector v)
        {
            I = v.x;
            J = v.y;
            K = v.z;
            W = w;
        }

        private static double Rad2Deg(double rad)
        {
            return rad*(180/Math.PI);
        }

        private static double Deg2Rad(double deg)
        {
            return deg*(Math.PI/180);
        }

        /// <summary>
        ///     Set the quaternion to the rotation defined by the euler angles in the vector
        /// </summary>
        /// <param name="euler">Angles are in degrees</param>
        public static Quaternion EulerToQuaternion(Vector euler)
        {
            //The following uses code from
            //http://www.euclideanspace.com/maths/geometry/rotations/conversions/eulerToQuaternion/index.htm

            double c1 = Math.Cos(Deg2Rad(euler.y));
            double s1 = Math.Sin(Deg2Rad(euler.y));
            double c2 = Math.Cos(Deg2Rad(euler.z));
            double s2 = Math.Sin(Deg2Rad(euler.z));
            double c3 = Math.Cos(Deg2Rad(euler.x));
            double s3 = Math.Sin(Deg2Rad(euler.x));
            double w = Math.Sqrt(1.0 + c1*c2 + c1*c3 - s1*s2*s3 + c2*c3)/2.0;
            double w4 = (4.0*w);
            double i = (c2*s3 + c1*s3 + s1*s2*c3)/w4;
            double j = (s1*c2 + s1*c3 + c1*s2*s3)/w4;
            double k = (-s1*s3 + c1*s2*c3 + s2)/w4;

            return new Quaternion(i, j, k, w);
        }

        /// <summary>
        ///     Convert quaternion to euler angles. Angles are in degrees
        ///     The following uses code from
        ///     http://www.euclideanspace.com/maths/geometry/rotations/conversions/quaternionToEuler/index.htm
        /// </summary>
        /// <param name="q"></param>
        /// <returns></returns>
        public static Vector QuaternionToEuler(Quaternion q)
        {
            var euler = new Vector();

            double sqw = q.W*q.W;
            double sqx = q.I*q.I;
            double sqy = q.J*q.J;
            double sqz = q.K*q.K;

            // if normalised is one, otherwise is correction factor
            double unit = sqx + sqy + sqz + sqw;
            double test = q.I*q.J + q.K*q.W;
            // singularity at north pole
            if (test > 0.499*unit)
            {
                euler.y = Rad2Deg(2*Math.Atan2(q.I, q.W));
                euler.z = Rad2Deg(Math.PI/2);
                euler.x = 0;
                return euler;
            }
            // singularity at south pole
            if (test < -0.499*unit)
            {
                euler.y = Rad2Deg(-2*Math.Atan2(q.I, q.W));
                euler.z = Rad2Deg(-(Math.PI/2));
                euler.x = 0;
                return euler;
            }
            euler.x = Rad2Deg(Math.Atan2(2*q.I*q.W - 2*q.J*q.K, -sqx + sqy - sqz + sqw));
            euler.y = Rad2Deg(Math.Atan2(2*q.J*q.W - 2*q.I*q.K, sqx - sqy - sqz + sqw));
            euler.z = Rad2Deg(Math.Asin(2*test/unit));
            return euler;
        }

        /// <summary>
        ///     ++http://osdir.com/ml/games.devel.algorithms/2002-11/msg00318.html
        /// </summary>
        /// <param name="qin"></param>
        /// <returns></returns>
        public static Matrix QuaternionToMatrix(Quaternion qin)
        {
            var q = new Quaternion(qin.I, qin.J, qin.K, qin.W);
            q.Normalize();

            var m = new Matrix();
            var v = new Vector(q.I, q.J, q.K);
            double w = q.W;

            m.M00 = 1 - 2*v.y*v.y - 2*v.z*v.z;
            m.M01 = 2*v.x*v.y - 2*w*v.z;
            m.M02 = 2*v.x*v.z + 2*w*v.y;
            m.M10 = 2*v.x*v.y + 2*w*v.z;
            m.M11 = 1 - 2*v.x*v.x - 2*v.z*v.z;
            m.M12 = 2*v.y*v.z - 2*w*v.x;
            m.M20 = 2*v.x*v.z - 2*w*v.y;
            m.M21 = 2*v.y*v.z + 2*w*v.x;
            m.M22 = 1 - 2*v.x*v.x - 2*v.y*v.y;

            return m;
        }

        /// <summary>
        ///     http://www.euclideanspace.com/maths/geometry/rotations/conversions/matrixToQuaternion/index.htm
        ///     FLHook
        /// </summary>
        /// <param name="m"></param>
        /// <returns></returns>
        public static Quaternion MatrixToQuaternion(Matrix m)
        {
            var q = new Quaternion();

            double tr = m.M00 + m.M11 + m.M22;

            if (tr > 0)
            {
                var s = Math.Sqrt(tr + 1.0)*2; // S=4*qw 
                q.W = 0.25*s;
                q.I = (m.M21 - m.M12)/s;
                q.J = (m.M02 - m.M20)/s;
                q.K = (m.M10 - m.M01)/s;
            }
            else if ((m.M00 > m.M11) & (m.M00 > m.M22))
            {
                var s = Math.Sqrt(1.0 + m.M00 - m.M11 - m.M22)*2; // S=4*qx 
                q.W = (m.M21 - m.M12)/s;
                q.I = 0.25*s;
                q.J = (m.M01 + m.M10)/s;
                q.K = (m.M02 + m.M20)/s;
            }
            else if (m.M11 > m.M22)
            {
                var s = Math.Sqrt(1.0 + m.M11 - m.M00 - m.M22)*2; // S=4*qy
                q.W = (m.M02 - m.M20)/s;
                q.I = (m.M01 + m.M10)/s;
                q.J = 0.25*s;
                q.K = (m.M12 + m.M21)/s;
            }
            else
            {
                var s = Math.Sqrt(1.0 + m.M22 - m.M00 - m.M11)*2; // S=4*qz
                q.W = (m.M10 - m.M01)/s;
                q.I = (m.M02 + m.M20)/s;
                q.J = (m.M12 + m.M21)/s;
                q.K = 0.25*s;
            }

            q.Normalize();

            return q;
        }

        public Quaternion Normalize()
        {
            double norm = Math.Sqrt(I*I + J*J + K*K + W*W);
            I /= norm;
            J /= norm;
            K /= norm;
            W /= norm;

            return this;
        }

        public static Quaternion Identity()
        {
            return new Quaternion(0, 0, 0, 1);
        }

        public static Quaternion operator *(Quaternion a, double b)
        {
            return new Quaternion(a.I*b, a.J*b, a.K*b, a.W*b);
        }

        public static Quaternion operator *(Quaternion a, Quaternion b)
        {
            return new Quaternion
            {
                W = a.W*b.W - a.I*b.I - a.J*b.J - a.K*b.K,
                I = a.W*b.I + a.I*b.W + a.J*b.K - a.K*b.J,
                J = a.W*b.J + a.J*b.W + a.K*b.I - a.I*b.K,
                K = a.W*b.K + a.K*b.W + a.I*b.J - a.J*b.I
            };
        }

        /// <summary>
        ///     http://www.flipcode.com/documents/matrfaq.html#Q56
        /// </summary>
        /// <param name="axis"></param>
        /// <param name="angle"></param>
        /// <returns></returns>
        public static Quaternion AxisAngleToQuaternion(Vector axis, double angle)
        {
            double sinA = Math.Sin(angle/2);
            double cosA = Math.Cos(angle/2);

            var c = new Quaternion {I = axis.x*sinA, J = axis.y*sinA, K = axis.z*sinA, W = cosA};
            return c.Normalize();
        }
    }
}