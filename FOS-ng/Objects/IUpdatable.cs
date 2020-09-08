namespace FOS_ng.Objects
{
    public interface IUpdatable
    {
        bool Update(float deltaSeconds);
        bool LongUpdate(float deltaSeconds);
    }
}
