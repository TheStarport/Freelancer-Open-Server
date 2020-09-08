using Akka.Actor;
using FLServer.Objects;

namespace FLServer.Actors.Player.Ship
{

    /// <summary>
    /// This class should be the proxy to SimObject later.
    /// </summary>
    class PlayerShip : TypedActor,IHandle<AskHullStatus>,IHandle<ShipData>,IHandle<AskShipData>
    {
    
        ShipData _shipData = new ShipData();

        public void Handle(AskHullStatus message)
        {
            byte[] omsg = { 0x49, 0x02 };

            FLMsgType.AddFloat(ref omsg, _shipData.Health);
            Context.Sender.Tell(omsg);
        }

        public void Handle(ShipData message)
        {
            _shipData = message;
            //TODO: pass on to ship
        }

        public void Handle(AskShipData message)
        {
            Context.Sender.Tell(_shipData);
        }

    }
}
