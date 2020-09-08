using FLServer.Object.Solar;
using FLServer.Solar;
using FLServer.Physics;
namespace FLServer
{
    public class Action
    {
    }

    public class JumpAction : Action
    {
        public bool Activated = false;
        public Object.Solar.Solar DestinationSolar;
        public DockingObject DockingObj;
    }

    public class TradeLaneAction : Action
    {
        public DockingObject DockingObj;
    }

    public class DockAction : Action
    {
        public DockingObject DockingObj;
    }

    public class LaunchFromBaseAction : Action
    {
        public DockingObject DockingObj;
        public Quaternion Orientation;
        public Vector Position;
    }

    public class LaunchInSpaceAction : Action
    {
        public Quaternion Orientation;
        public Vector Position;
    }
}