namespace FLServer.Actors.Session.DPlaySession
{
    partial class DPlaySession
    {

        public class DPlayMessage
        {
            
        }

        public class ByteMessage : DPlayMessage
        {
            public byte[] Data;
            public int Position;

            public ByteMessage(byte[] data, int pos)
            {
                Data = data;
                Position = pos;
            }
        }

        public class ConnectRequest : DPlayMessage
        {
            public byte MsgID;
            public uint DPlayID;
            public byte RspID;

            public ConnectRequest(byte msgID, uint dPlayID, byte rspID)
            {
                MsgID = msgID;
                DPlayID = dPlayID;
                RspID = rspID;
            }



        }

        public struct SendNews
        {
            
        }

    }
}
