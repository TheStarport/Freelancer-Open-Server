using Akka.Actor;

namespace FLServer.Actors
{
	public class Globals : TypedActor,
		IHandle<Session.Session.EnumRequest>,
		IHandle<string>
	{

		public class ServerInfo
		{
			public uint MaxPlayers;
			public int CurrentPlayers;
			public string ServerName;

			public byte[] AppGUID;
			public byte[] InstanceGUID;

			public ServerInfo(uint maxPlayers, int curPlayers, string sName, byte[] appGUID, byte[] instGUID)
			{
				MaxPlayers = maxPlayers;
				CurrentPlayers = curPlayers;
				ServerName = sName;
				AppGUID = appGUID;
				InstanceGUID = instGUID;
			}
		}

		/// <summary>
		///     The freelancer instance GUID. Copied from the real freelancer server application.
		/// </summary>
		public static byte[] ApplicationInstanceGUID =
		{
			0xa8, 0xc6, 0x27, 0x1d, 0x41, 0x66, 0xd8, 0x49, 0x89, 0xeb, 0x1e,
			0xbc, 0x42, 0x21, 0xca, 0xe9
		};

		/// <summary>
		///     The dplay GUID. Copied from the real freelancer server application.
		/// </summary>
		public static byte[] ApplicationGUID =
		{
			0x26, 0xf0, 0x90, 0xa6, 0xf0, 0x26, 0x57, 0x4e, 0xac, 0xa0, 0xec, 0xf8,
			0x68, 0xe4, 0x8d, 0x21
		};


		private string ServerVersion = "810352641"; //Disco's current 48.77.00.01
		private string ServerID = "00000000-00000000-00000000-00000000";
		private string ServerDesc = "Some Cool Servo! \nWelp. \0"; // must be nullterm'd
		private string ServerName = "Freelancer Open Server\0";
		private string ServerNews = "Newz here!\n We are very very happy to see this.";
		private uint MaxPlayers = 337;
		private int CurrentPlayers = 31;


		public void Handle(string message)
		{
			switch (message)
			{
				case "GetInfo":
					Context.Sender.Tell(new ServerInfo(
						MaxPlayers,CurrentPlayers,ServerName,
						ApplicationGUID,ApplicationInstanceGUID));
					break;
				case "GetNews":
					Sender.Tell(ServerNews);
					break;
			}
		}

		public void Handle(Session.Session.EnumRequest message)
		{
			var sock = Context.ActorSelection(Context.Sender.Path.Child("socket"));


			// TODO: we could need password field later
			const bool password = false;
			const bool nodpnsvr = true;

			var applicationData = new byte[0];
			FLMsgType.AddAsciiStringLen0(ref applicationData,
				"1:1:" + ServerVersion + ":-1910309061:" + ServerID + ":");
			FLMsgType.AddUnicodeStringLen0(ref applicationData, ServerDesc);

			byte[] pkt = { 0x00, 0x03 };
			FLMsgType.AddUInt16(ref pkt, message.EnumPayload);
			FLMsgType.AddUInt32(ref pkt, 0x58 + (uint)ServerName.Length * 2); // ReplyOffset
			FLMsgType.AddUInt32(ref pkt, (uint)applicationData.Length); // ReplySize/ResponseSize
			FLMsgType.AddUInt32(ref pkt, 0x50); // ApplicationDescSize
			FLMsgType.AddUInt32(ref pkt, (password ? 0x80u : 0x00u) | (nodpnsvr ? 0x40u : 0x00u));
			// ApplicationDescFlags
			FLMsgType.AddUInt32(ref pkt, MaxPlayers + 1); // MaxPlayers
			FLMsgType.AddUInt32(ref pkt, (uint)CurrentPlayers + 1); // CurrentPlayers
			FLMsgType.AddUInt32(ref pkt, 0x58); // SessionNameOffset
			FLMsgType.AddUInt32(ref pkt, (uint)ServerName.Length * 2); // SessionNameSize
			FLMsgType.AddUInt32(ref pkt, 0); // PasswordOffset
			FLMsgType.AddUInt32(ref pkt, 0); // PasswordSize
			FLMsgType.AddUInt32(ref pkt, 0); // ReservedDataOffset
			FLMsgType.AddUInt32(ref pkt, 0); // ReservedDataSize
			FLMsgType.AddUInt32(ref pkt, 0); // ApplicationReservedDataOffset
			FLMsgType.AddUInt32(ref pkt, 0); // ApplicationReservedDataSize
			FLMsgType.AddArray(ref pkt, ApplicationInstanceGUID); // ApplicationInstanceGUID
			FLMsgType.AddArray(ref pkt, ApplicationGUID); // ApplicationGUID
			FLMsgType.AddUnicodeStringLen0(ref pkt, ServerName); // SessionName
			FLMsgType.AddArray(ref pkt, applicationData); // ApplicationData

			sock.Tell(pkt, Context.Self);
		}


	}
}
