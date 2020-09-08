using Akka.Actor;
using NLog;

namespace FLServer.Actors.Player.PlayerList
{
	/// <summary>
	/// Player list responder class.
	/// </summary>
	class PlayerListHandler: TypedActor, 
		IHandle<PlayerJoined>,
		IHandle<PlayerJoinEnumResponse>,
		IHandle<PlayerUpdate>,
		IHandle<SetListData>,IHandle<ActorRef>,IHandle<PlayerParted>
	{
		private SetListData _data;
		private ActorRef _socket;

		/// <summary>
		/// This one is used to set socket's ref.
		/// </summary>
		/// <param name="message"></param>
		public void Handle(ActorRef message)
		{
			_socket = message;
		}

		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		protected override void PostStop()
		{
			//TODO: send player depart to everyone
			Logger.Debug("Now sending depart {0}",_data.CharName);
			//Context.ActorSelection("../../*/listhandler").Tell(new PlayerParted(
			base.PostStop();
		}

		public void Handle(PlayerParted message)
		{
			//TODO: send player depart to client
		}

		/// <summary>
		/// Set responder's data.
		/// </summary>
		/// <param name="message">Data structure.</param>
		public void Handle(SetListData message)
		{
			_data = message;
		}

		/// <summary>
		/// New player in the list.
		/// </summary>
		/// <param name="message"></param>
		public void Handle(PlayerJoined message)
		{
			if (_data == null) return;
			//We tell him about us
			if (_data.FLPlayerID != message.FLPlayerID)
			{
				Sender.Tell(new PlayerJoinEnumResponse(_data.CharName, _data.FLPlayerID));
			}
			Sender.Tell(new PlayerUpdate(_data.GroupID, _data.FLPlayerID, _data.Rank, _data.SystemID));

			//We tell client about the newcomer
			{
				byte[] omsg = { 0x52, 0x02 };
				FLMsgType.AddUInt32(ref omsg, 1); // 1 = new, 2 = depart
				FLMsgType.AddUInt32(ref omsg, message.FLPlayerID); // player id
				FLMsgType.AddUInt8(ref omsg, message.Hide | _data.FLPlayerID == message.FLPlayerID ? 1u : 0u); // hide 1 = yes, 0 = no
				FLMsgType.AddUnicodeStringLen8(ref omsg, message.Name + "\0");
				//playerto.SendMsgToClient(omsg);
				_socket.Tell(omsg);
			}
		}


		/// <summary>
		/// Someone's joined or we just connected and receiving everyone's info.
		/// </summary>
		/// <param name="message"></param>
		public void Handle(PlayerJoinEnumResponse message)
		{
			if (_data == null) return;
			{
				byte[] omsg = { 0x52, 0x02 };
				FLMsgType.AddUInt32(ref omsg, 1); // 1 = new, 2 = depart
				FLMsgType.AddUInt32(ref omsg, message.FLPlayerID); // player id
				FLMsgType.AddUInt8(ref omsg, message.Hide ? 1u : 0u); // hide 1 = yes, 0 = no
				FLMsgType.AddUnicodeStringLen8(ref omsg, message.Name + "\0");
				//playerto.SendMsgToClient(omsg);
				_socket.Tell(omsg);
			}
		}


		public void Handle(PlayerUpdate message)
		{
			{
				//Rank update
				byte[] omsg = { 0x54, 0x02, 0x44, 0x00 };
				FLMsgType.AddUInt32(ref omsg, message.FLPlayerID);
				FLMsgType.AddUInt16(ref omsg, message.Rank);
				_socket.Tell(omsg);
			}

			{
				//System update
				byte[] omsg = { 0x54, 0x02, 0x84, 0x00 };
				FLMsgType.AddUInt32(ref omsg, message.FLPlayerID);
				FLMsgType.AddUInt32(ref omsg, message.SystemID);
				_socket.Tell(omsg);
			}

			{
				//Group update
				byte[] omsg = { 0x54, 0x02, 0x05, 0x00 };
				FLMsgType.AddUInt32(ref omsg, message.FLPlayerID);
				FLMsgType.AddUInt32(ref omsg, message.GroupID);
				_socket.Tell(omsg);
			}
		}



	}
}
