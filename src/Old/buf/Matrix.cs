using System;

namespace FLServer
{
    public class Matrix
    {
        public double M00, M01, M02;
        public double M10, M11, M12;
        public double M20, M21, M22;

        public Matrix()
        {
            M00 = M11 = M22 = 1;

            M01 = M02 = M10 = M20 = M21 = M12 = 0;
        }

        public Matrix(Matrix source)
        {
            M00 = source.M00;
            M01 = source.M01;
            M02 = source.M02;

            M10 = source.M10;
            M11 = source.M11;
            M12 = source.M12;

            M20 = source.M20;
            M21 = source.M21;
            M22 = source.M22;
        }

        private static double Rad2Deg(double rad)
        {
            return rad*(180/Math.PI);
        }

        private static double Deg2Rad(double deg)
        {
            return deg*(Math.PI/180);
        }


        public Vector GetBackward()
        {
            return new Vector(M02, M12, M22);
        }

        public Vector GetUp()
        {
            return new Vector(M01, M11, M21);
        }

        public Vector GetRight()
        {
            return new Vector(M00, M10, M20);
        }

        /// <summary>
        ///     Convert a matrix into euler angles in degrees
        ///     http://khayyam.kaplinski.com/2011/02/converting-rotation-matrix-to.html
        /// </summary>
        /// <param name="m">The matrix to convert</param>
        /// <returns>Euler angles in radians</returns>
        public static Vector MatrixToEuler(Matrix m)
        {
            var vec = new Vector();
            var x = new Vector(m.M00, m.M10, m.M20);
            var y = new Vector(m.M01, m.M11, m.M21);
            var z = new Vector(m.M02, m.M12, m.M22);

            double h = Math.Sqrt(x.x*x.x + x.y*x.y);
            if (h > 1/524288.0)
            {
                vec.x = Math.Atan2(y.z, z.z);
                vec.y = Math.Atan2(-x.z, h);
                vec.z = Math.Atan2(x.y, x.x);
            }
            else
            {
                vec.x = Math.Atan2(-z.y, y.y);
                vec.y = Math.Atan2(-x.z, h);
                vec.z = 0;
            }

            return vec;
        }

        /// <summary>
        ///     Convert a matrix into euler angles in degrees
        ///     http://khayyam.kaplinski.com/2011/02/converting-rotation-matrix-to.html
        /// </summary>
        /// <param name="m">The matrix to convert</param>
        /// <returns>Euler angles in degrees</returns>
        public static Vector MatrixToEulerDeg(Matrix m)
        {
            var vec = new Vector();
            var x = new Vector(m.M00, m.M10, m.M20);
            var y = new Vector(m.M01, m.M11, m.M21);
            var z = new Vector(m.M02, m.M12, m.M22);

            double h = Math.Sqrt(x.x*x.x + x.y*x.y);
            if (h > 1/524288.0)
            {
                vec.x = Math.Atan2(y.z, z.z);
                vec.y = Math.Atan2(-x.z, h);
                vec.z = Math.Atan2(x.y, x.x);
            }
            else
            {
                vec.x = Math.Atan2(-z.y, y.y);
                vec.y = Math.Atan2(-x.z, h);
                vec.z = 0;
            }

            vec.x = Rad2Deg(vec.x);
            vec.y = Rad2Deg(vec.y);
            vec.z = Rad2Deg(vec.z);

            return vec;
        }

        /// <summary>
        ///     Convert euler angles in radians to matrix.
        /// </summary>
        /// <param name="euler">
        ///     Euler angles in radians</param>
        ///     <returns></returns>
        public static Matrix EulerToMatrix(Vector euler)
        {
            double cp = Math.Cos(euler.x);
            double sp = Math.Sin(euler.x);
            double cy = Math.Cos(euler.y);
            double sy = Math.Sin(euler.y);
            double cr = Math.Cos(euler.z);
            double sr = Math.Sin(euler.z);

            return new Matrix
            {
                M00 = cy*cr,
                M01 = sp*sy*cr - cp*sr,
                M02 = cp*sy*cr + sp*sr,
                M10 = cy*sr,
                M11 = sp*sy*sr + cp*cr,
                M12 = cp*sy*sr - sp*cr,
                M20 = -sy,
                M21 = sp*cy,
                M22 = cp*cy
            };
        }

        /// <summary>
        ///     Convert euler angles in degrees to matrix.
        /// </summary>
        /// <param name="euler">
        ///     Euler angles in degrees</param>
        ///     <returns></returns>
        public static Matrix EulerDegToMatrix(Vector euler)
        {
            double cp = Math.Cos(Deg2Rad(euler.x));
            double sp = Math.Sin(Deg2Rad(euler.x));
            double cy = Math.Cos(Deg2Rad(euler.y));
            double sy = Math.Sin(Deg2Rad(euler.y));
            double cr = Math.Cos(Deg2Rad(euler.z));
            double sr = Math.Sin(Deg2Rad(euler.z));

            return new Matrix
            {
                M00 = cy*cr,
                M01 = sp*sy*cr - cp*sr,
                M02 = cp*sy*cr + sp*sr,
                M10 = cy*sr,
                M11 = sp*sy*sr + cp*cr,
                M12 = cp*sy*sr - sp*cr,
                M20 = -sy,
                M21 = sp*cy,
                M22 = cp*cy
            };
        }

        /// <summary>
        ///     Transform the vector supplied using the transform defined
        ///     by this matrix.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        ///     <returns></returns>
        public static Vector operator *(Matrix a, Vector b)
        {
            var c = new Vector
            {
                x = a.M00*b.x + a.M01*b.y + a.M02*b.z,
                y = a.M10*b.x + a.M11*b.y + a.M12*b.z,
                z = a.M20*b.x + a.M21*b.y + a.M22*b.z
            };
            return c;
        }

        /// <summary>
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static Matrix operator *(Matrix a, Matrix b)
        {
            var m = new Matrix
            {
                M00 = a.M00*b.M00 + a.M01*b.M10 + a.M02*b.M20,
                M01 = a.M00*b.M01 + a.M01*b.M11 + a.M02*b.M21,
                M02 = a.M00*b.M02 + a.M01*b.M12 + a.M02*b.M22,
                M10 = a.M10*b.M00 + a.M11*b.M10 + a.M12*b.M20,
                M11 = a.M10*b.M01 + a.M11*b.M11 + a.M12*b.M21,
                M12 = a.M10*b.M02 + a.M11*b.M12 + a.M12*b.M22,
                M20 = a.M20*b.M00 + a.M21*b.M10 + a.M22*b.M20,
                M21 = a.M20*b.M01 + a.M21*b.M11 + a.M22*b.M21,
                M22 = a.M20*b.M02 + a.M21*b.M12 + a.M22*b.M22
            };
            return m;
        }

        /// <summary>
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static Matrix operator *(Matrix a, double b)
        {
            var m = new Matrix(a);
            m.M00 *= b;
            m.M01 *= b;
            m.M02 *= b;

            m.M10 *= b;
            m.M11 *= b;
            m.M12 *= b;

            m.M20 *= b;
            m.M21 *= b;
            m.M22 *= b;

            return m;
        }

        /// <summary>
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static Matrix operator /(Matrix a, double b)
        {
            var m = new Matrix(a);
            m.M00 /= b;
            m.M01 /= b;
            m.M02 /= b;

            m.M10 /= b;
            m.M11 /= b;
            m.M12 /= b;

            m.M20 /= b;
            m.M21 /= b;
            m.M22 /= b;

            return m;
        }

        /// <summary>
        ///     Change the orientation of the Matrix a by rotating it 180 degrees
        ///     around the z-axis
        /// </summary>
        /// <param name="a"></param>
        /// <returns></returns>
        public static Matrix TurnAround(Matrix a)
        {
            return EulerDegToMatrix(new Vector(0, 180, 0))*a;
        }

        /// <summary>
        ///     Creates a view matrix (without translation) with the origin at position and oriented towards lookat
        /// </summary>
        /// <param name="position"></param>
        /// <param name="lookat"></param>
        /// <returns></returns>
        public static Matrix CreateLookAt(Vector position, Vector lookat)
        {
            Vector zaxis = (position - lookat).Normalize();
            Vector xaxis = Vector.Y().Cross(zaxis).Normalize();
            Vector yaxis = zaxis.Cross(xaxis);

            return new Matrix
            {
                M00 = xaxis.x,
                M10 = xaxis.y,
                M20 = xaxis.z,
                M01 = yaxis.x,
                M11 = yaxis.y,
                M21 = yaxis.z,
                M02 = zaxis.x,
                M12 = zaxis.y,
                M22 = zaxis.z
            };
        }

        // http://inside.mines.edu/~gmurray/ArbitraryAxisRotation/ 5.2
        public static Matrix CreateRotationAboutAxis(Vector axis_, double cosine)
        {
            var axis = axis_.Normalize();

            double sine = Math.Sqrt(1 - cosine*cosine);

            return new Matrix
            {
                M00 = axis.x*axis.x + (1 - axis.x*axis.x)*cosine,
                M01 = axis.y*axis.x*(1 - cosine) - axis.z*sine,
                M02 = axis.z*axis.x*(1 - cosine) + axis.y*sine,
                M10 = axis.x*axis.y*(1 - cosine) + axis.z*sine,
                M11 = axis.y*axis.y + (1 - axis.y*axis.y)*cosine,
                M12 = axis.z*axis.y*(1 - cosine) - axis.x*sine,
                M20 = axis.x*axis.z*(1 - cosine) - axis.y*sine,
                M21 = axis.y*axis.z*(1 - cosine) + axis.x*sine,
                M22 = axis.z*axis.z + (1 - axis.z*axis.z)*cosine
            };
        }

        // http://www.dr-lex.be/random/matrix_inv.html
        public double Det()
        {
            return M00*(M22*M11 - M21*M12) - M10*(M22*M01 - M21*M02) + M20*(M12*M01 - M11*M02);
        }

        public Matrix Transpose()
        {
            return new Matrix(this) {M01 = M10, M02 = M20, M10 = M01, M12 = M21, M20 = M02, M21 = M12};
        }

        // http://www.dr-lex.be/random/matrix_inv.html
        public Matrix Invert()
        {
            var i = new Matrix();

            double det = Det();
            if (det == 0)
                throw new ArgumentException();

            i.M00 = M22*M11 - M21*M12;
            i.M01 = -(M22*M01 - M21*M02);
            i.M02 = M12*M01 - M11*M02;

            i.M10 = -(M22*M10 - M20*M12);
            i.M11 = M22*M00 - M20*M02;
            i.M12 = -(M12*M00 - M10*M02);

            i.M20 = M21*M10 - M20*M11;
            i.M21 = -(M21*M00 - M20*M01);
            i.M22 = M11*M00 - M10*M01;

            i /= det;

            return i;
        }
    }
}