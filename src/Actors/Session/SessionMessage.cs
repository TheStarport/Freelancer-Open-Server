namespace FLServer.Actors.Session
{
	partial class Session
	{
		

		public class Message
		{

		}

		public class ByteMessage : Message
		{
			public byte[] Bytes;

			public ByteMessage(byte[] msg)
			{
				Bytes = msg;
			}
		}

		public class TxMessage : ByteMessage
		{
			public TxMessage(byte[] msg) : base(msg)
			{
			}
		}

		public class RxMessage : ByteMessage
		{
			public RxMessage(byte[] msg)
				: base(msg)
			{
			}
		}

		public class EnumRequest : Message
		{
			public ushort EnumPayload;

			public EnumRequest(ushort enumPayload)
			{
				EnumPayload = enumPayload;
			}
		}

		public class ConnectingMessage : Message
		{
			
		}


	}
}
