namespace FLServer
{
    public class OrientedBoundingBox
    {
        public Vector Max;
        public Vector Min;

        public bool IsInside(Vector transformedVector)
        {
            if (Min.x > transformedVector.x || Max.x < transformedVector.x)
                return false;

            if (Min.y > transformedVector.y || Max.y < transformedVector.y)
                return false;

            if (Min.z > transformedVector.z || Max.z < transformedVector.z)
                return false;

            return true;
        }
    }
}