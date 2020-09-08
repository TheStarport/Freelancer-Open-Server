using Akka.Actor;
using Jitter.LinearMath;

namespace FLServer.Objects.SimObject
{
    public abstract partial class SimObject : UntypedActor
    {

        /// <summary>
        /// Range at which we accept update messages.
        /// </summary>
        private const int UpdateRange = 25000;

        private readonly bool _stationary;

        /// <summary>
        ///     The archetype id of the object.
        /// </summary>
		protected GameDB.Arch.Archetype Arch;

        /// <summary>
        ///     The health of this object, from 0 to 1.
        /// </summary>
        protected float Health;

        /// <summary>
        ///     The ID of this object in space.
        /// </summary>
        protected uint Objid;

        /// <summary>
        ///     The current orientation of this object.
        /// </summary>
        protected JMatrix Orientation = new JMatrix();

        /// <summary>
        ///     The current position of this object
        /// </summary>
        protected JVector Position = new JVector();

        /// <summary>
        ///     The current throttle position of this object.
        /// </summary>
        protected float Throttle;

        /// <summary>
        ///     Create the simulation object.
        /// </summary>
        protected SimObject()
        {
            //ExpireAfter(0.1);

            //TODO: check dis
#pragma warning disable 184
             _stationary = (this is Object.Solar.Solar);
#pragma warning restore 184
        }

        /// <summary>
        ///     Return the actual or estimated position of this simobject.
        /// </summary>
        /// <returns></returns>
        public abstract JVector ExtrapolatedPosition();

        protected abstract void InternalHandle(object message);

        protected override void OnReceive(object message)
        {
            //TODO: Length or LengthSquared?
            if (message is GenericMessage)
            {
                if ((((GenericMessage) message).Position - Position).LengthSquared() > UpdateRange) return;
            }
            InternalHandle(message);
        }
    }
}