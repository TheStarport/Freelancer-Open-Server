using Jitter.LinearMath;

// ReSharper disable once CheckNamespace
namespace FLServer.Objects.SimObject
{
    partial class SimObject
    {

        private struct GenericMessage
        {
            public readonly JVector Position;
            public GenericMessage(JVector position)
            {
                Position = position;
            }
        }

    }
}
