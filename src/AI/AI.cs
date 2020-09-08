using System.Linq;
using FLServer.Object;
using FLServer.Old.Object;
using FLServer.Server;

namespace FLServer.AI
{
    //TODO: make interface to all AI-able objects (loadout, powergen, etc)

    public class AI
    {

        protected SimObject SelectTarget(Old.Object.Ship.Ship ship, DPGameRunner runner)
        {
            return runner.Objects.Values.Where(obj => obj != ship).FirstOrDefault(obj => obj is Old.Object.Ship.Ship && obj.Position.DistanceTo(ship.Position) < 5000);
        }

        public virtual void Update(Old.Object.Ship.Ship ship, DPGameRunner server, double seconds)
        {
            
        }
    }
}