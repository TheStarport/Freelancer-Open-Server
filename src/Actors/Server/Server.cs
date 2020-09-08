using Akka.Actor;
using Akka.Event;
using FLServer.Actors.System;

// ReSharper disable once CheckNamespace
namespace FLServer.Actors
{
	public partial class Server : TypedActor, 
		IHandle<UdpSockReader.SockRxMsg>, 
		IHandle<DeathPactException>,
		IHandle<string>
	{
		readonly LoggingAdapter _log = Akka.Event.Logging.GetLogger(Context);

		public string ServerID;
		public string ServerVersion;
		public string ServerDescription;
		public string ServerName;
		public bool IsPassworded;
		public bool NoDpnSvr;
		public byte[] ApplicationInstanceGUID;
		public byte[] ApplicationGUID;

		private readonly int _port;

		public Server(int message)
		{
			var sock = Context.ActorOf(Props.Create(() => new UdpSockReader(message)), "reader-socket");
			Context.Watch(sock);
			sock.Tell(new UdpSockReader.StartListeningMessage());
			var glob = Context.ActorOf<Globals>("globals");
			Context.Watch(glob);
			_port = message;
			Context.System.EventStream.Subscribe(Self, typeof(DeadLetter));
			_log.Info("{0} Server started on port {1}", Context.Self.Path, message);
			//var router= Context.System.ActorOf(new Props().WithRouter(new ConsistentHashingGroup("player/*")),"player-router");
		}


		public void Handle(UdpSockReader.SockRxMsg message)
		{
			
			if (message.Message.Length < 2)
				return;

// ReSharper disable once RedundantToStringCall
			var path = "sessions/" + message.EndPoint.ToString();
			var session = Context.Child(path);
			if (session.IsNobody())
			{
// ReSharper disable once RedundantToStringCall
				session = Context.ActorOf(Props.Create(()=> new Session.Session(message.EndPoint,_port)),path);
				_log.Debug("New UDP session: {0}", message.EndPoint.ToString());
			}

			session.Tell(new Session.Session.RxMessage(message.Message));
			//Context.ActorSelection("asdasdasd").Tell(1234);
		}

		/// <summary>
		/// This one receives exceptions thrown by children (we watch sockets).
		/// </summary>
		/// <param name="message"></param>
		public void Handle(DeathPactException message)
		{
			_log.Warn("DeadPact letter: {0}, {2}", message.DeadActor.Path, message.Message);
		}

		public void Handle(string message)
		{
			switch (message)
			{
				case "NewPlayer":
					var player = Context.ActorOf<PlayerActor>("player/" + NewPlayerID());
					Sender.Tell(player);
					break;
			}
		}

		/// <summary>
		///     Return true if the playerid is in use.
		/// </summary>
		/// <param name="playerid"></param>
		/// <returns></returns>
		private static bool IsPlayerIDInUse(uint playerid)
		{
			return (!Equals(Context.Child("player/" + playerid), Nobody.Instance));
		}

		/// <summary>
		///     Allocate an unused FL player ID.
		/// </summary>
		/// <returns></returns>
		private static uint NewPlayerID()
		{
			uint i = 0;
			// ReSharper disable once EmptyEmbeddedStatement
			while (IsPlayerIDInUse(++i))
				;
			return i;
		}
	}
}
