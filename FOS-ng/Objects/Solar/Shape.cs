using System;

namespace FLServer.Object.Solar
{
    public abstract class Shape
    {
        protected readonly Matrix InverseOrientation;
        public readonly Matrix Orientation;
        public readonly Vector Position;

        protected Shape(Vector position, Matrix orientation)
        {
            Position = position;
            Orientation = orientation;
            InverseOrientation = orientation.Transpose();
        }

        public abstract bool IsInside(Vector position);
    }

    public class Sphere : Shape
    {
        public readonly double Radius;

        public Sphere(Vector position, Matrix orientation, double radius)
            : base(position, orientation)
        {
            Radius = radius;
        }

        public override bool IsInside(Vector position)
        {
            return position.DistSqr(Position) <= Radius * Radius;
        }
    }

    public class Ellipsoid : Shape
    {
        public Vector Axes;

        public Ellipsoid(Vector position, Matrix orientation, Vector axes)
            : base(position, orientation)
        {
            Axes = axes;
        }

        public override bool IsInside(Vector position)
        {
            var transformedPosition = (InverseOrientation * (position - Position)) / Axes;
            return transformedPosition.LengthSq() <= 1;
        }
    }

    public class Box : Shape
    {
        public Vector Axes;

        public Box(Vector position, Matrix orientation, Vector axes)
            : base(position, orientation)
        {
            Axes = axes;
        }

        public override bool IsInside(Vector position)
        {
            var transformedPosition = (InverseOrientation * (position - Position)) / Axes;
            return Math.Abs(transformedPosition.x) <= 0.5 && Math.Abs(transformedPosition.y) <= 0.5 &&
                   Math.Abs(transformedPosition.z) <= 0.5;
        }
    }

    public class Cylinder : Shape
    {
        public double Length;
        public double Radius;

        public Cylinder(Vector position, Matrix orientation, double radius, double length)
            : base(position, orientation)
        {
            Radius = radius;
            Length = length;
        }

        public override bool IsInside(Vector position)
        {
            var transformedPosition = (InverseOrientation * (position - Position));
            transformedPosition.z /= Length;
            return Math.Abs(transformedPosition.z) <= 0.5 &&
                   (transformedPosition.x * transformedPosition.x + transformedPosition.y * transformedPosition.y) <=
                   Radius;
        }
    }

    public class Ring : Cylinder
    {
        public double InnerRadius;

        public Ring(Vector position, Matrix orientation, double radius, double innerRadius, double length)
            : base(position, orientation, radius, length)
        {
            InnerRadius = innerRadius;
        }

        public override bool IsInside(Vector position)
        {
            var transformedPosition = (InverseOrientation * (position - Position));
            transformedPosition.z /= Length;
            var size2D = (transformedPosition.x * transformedPosition.x +
                             transformedPosition.y * transformedPosition.y);
            return Math.Abs(transformedPosition.z) <= 0.5 && size2D <= Radius && size2D > InnerRadius;
        }
    }
}
