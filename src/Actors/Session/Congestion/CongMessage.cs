namespace FLServer.Actors.Session.Congestion
{
    partial class Congestion
    {

        public class AckUserData
        {
            public byte NRcv;

            public AckUserData(byte nrcv)
            {
                NRcv = nrcv;
            }
        }

        public struct AckIfInWindow
        {
            public byte Sequence;

            public AckIfInWindow(byte seq)
            {
                Sequence = seq;
            }
        }

        public struct DoRetryOnSACKMessage
        {
            public uint Mask;
            public byte NRcv;

            public DoRetryOnSACKMessage(uint mask, byte nrcv)
            {
                Mask = mask;
                NRcv = nrcv;
            }
        }

        public struct CheckIfOutOfOrder
        {
            public byte[] Data;
            public byte Sequence;

            public CheckIfOutOfOrder(byte seq, byte[] data)
            {
                Sequence = seq;
                Data = data;
            }
        }
    }
}
