using System;
using ProtoBuf;

namespace FLServer.Physics
{
    [Serializable]
    [ProtoContract]
    public class Vector
    {
        [ProtoMember(1)]
        public double x;
        [ProtoMember(2)]
        public double y;
        [ProtoMember(3)]
        public double z;

        public Vector()
        {
            x = 0;
            y = 0;
            z = 0;
        }

        public Vector(Vector source)
        {
            x = source.x;
            y = source.y;
            z = source.z;
        }

        public Vector(double ix, double iy, double iz)
        {
            x = ix;
            y = iy;
            z = iz;
        }

        public void Zero()
        {
            z = 0;
            y = 0;
            x = 0;
        }

        public override string ToString()
        {
            return x + ", " + y + ", " + z;
        }

        public static Vector X1()
        {
            return new Vector(1, 0, 0);
        }

        public static Vector Y1()
        {
            return new Vector(0, 1, 0);
        }

        public static Vector Z1()
        {
            return new Vector(0, 0, 1);
        }

        /// <summary>
        ///     Determine the square of the distance to VEC.
        /// </summary>
        /// <param name="vec"></param>
        /// <returns></returns>
        public double DistSqr(Vector vec)
        {
            return ((x - vec.x)*(x - vec.x) +
                    (y - vec.y)*(y - vec.y) +
                    (z - vec.z)*(z - vec.z));
        }

        // Determine the distance to VEC.
        public double DistanceTo(Vector vec)
        {
            return Math.Sqrt(DistSqr(vec));
        }

        public bool IsEqual(Vector p)
        {
            return x == p.x && y == p.y && z == p.z;
        }

        public static Vector operator *(Vector v, double a)
        {
            return new Vector(v.x*a, v.y*a, v.z*a);
        }

        public static Vector operator *(double a, Vector v)
        {
            return new Vector(v.x*a, v.y*a, v.z*a);
        }

        public static Vector operator /(Vector v, double a)
        {
            return new Vector(v.x/a, v.y/a, v.z/a);
        }

        public static Vector operator +(Vector a, Vector b)
        {
            return new Vector(a.x + b.x, a.y + b.y, a.z + b.z);
        }

        public static Vector operator -(Vector a, Vector b)
        {
            return new Vector(a.x - b.x, a.y - b.y, a.z - b.z);
        }

        public static Vector operator *(Vector a, Vector b)
        {
            return new Vector(a.x*b.x, a.y*b.y, a.z*b.z);
        }

        public static Vector operator /(Vector a, Vector b)
        {
            return new Vector(a.x/b.x, a.y/b.y, a.z/b.z);
        }

        public static Vector operator -(Vector a)
        {
            return new Vector(-a.x, -a.y, -a.z);
        }

        public double LengthSq()
        {
            return x*x + y*y + z*z;
        }

        public double Length()
        {
            return Math.Sqrt(LengthSq());
        }

        public double Dot(Vector p)
        {
            return x*p.x + y*p.y + z*p.z;
        }

        public Vector Cross(Vector p)
        {
            return new Vector(
                (y*p.z) - (z*p.y),
                (z*p.x) - (x*p.z),
                (x*p.y) - (y*p.x));
        }

        public Vector Normalize()
        {
            var a = new Vector(x, y, z);
            double magnitude = Math.Sqrt(a.x*a.x + a.y*a.y + a.z*a.z);
            if (magnitude > 0)
            {
                a.x /= magnitude;
                a.y /= magnitude;
                a.z /= magnitude;
            }
            return a;
        }

        public Vector Inverse()
        {
            return new Vector(-x, -y, -z);
        }
    }
}