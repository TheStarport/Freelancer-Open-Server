using FLServer.DataWorkers;
using FLServer.Physics;

namespace FLServer.AI
{
    public class DebugAI: ShipAI
    {

        public DebugAI(Old.Object.Ship.Ship ship) : base(ship)
        {
            waypoints.Add(new Waypoint(new Vector(-29292, -892, -27492), 0, FLUtility.CreateID("li01")));
            waypoints.Add(new Waypoint(new Vector(-30689, -600, -28092), 0, FLUtility.CreateID("li01")));
            waypoints.Add(new Waypoint(new Vector(-33021, -124, -27880), 0, FLUtility.CreateID("li01")));
            waypoints.Add(new Waypoint(new Vector(-35185, -138, -26487), 0, FLUtility.CreateID("li01")));
        }
    }
}
