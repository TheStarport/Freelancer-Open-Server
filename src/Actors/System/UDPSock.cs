using System;
using System.Net;
using System.Net.Sockets;
using Akka.Actor;

namespace FLServer.Actors.System
{
	/// <summary>
	/// That's a basic Actor-based UDP socket with everything the server needs.
	/// </summary>
	public class UdpSockReader :TypedActor, IHandle<byte[]>,IHandle<UdpSockReader.StartListeningMessage>,IHandle<string>
	{
		//private static readonly Logger logger = LogManager.GetCurrentClassLogger();
		public class StartListeningMessage 
		{
		}

		public struct SockRxMsg
		{
			public byte[] Message;
			public IPEndPoint EndPoint;

			public SockRxMsg(byte[] msg, IPEndPoint ep)
			{
				Message = msg;
				EndPoint = ep;
			}
		}


		public struct SockTxMsg
		{
			public byte[] Message;
			public IPEndPoint EndPoint;

			public SockTxMsg(byte[] msg, IPEndPoint ep)
			{
				Message = msg;
				EndPoint = ep;
			}
		}
		private readonly UdpClient _client;


		/// <summary>
		/// Initializes a new instance of the <see cref="FLServer.Actors.System.UdpSockReader"/> class.
		/// Socket is set up in listening mode, i.e. it will accept messages.
		/// </summary>
		/// <param name="port">Port.</param>
		public UdpSockReader(int port)
		{
			var localpt = new IPEndPoint(IPAddress.Any, port);
			_client = new UdpClient();


			if (Type.GetType("Mono.Runtime") == null)
			{
				const int sioUdpConnreset = -1744830452;
				_client.Client.IOControl((IOControlCode)sioUdpConnreset, new byte[] { 0, 0, 0, 0 }, null);
			}

			_client.Client.SetSocketOption(
				SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

			_client.Client.Bind(localpt);

		}

		/// <summary>
		/// Initiate the receive\send socket; currently it's used only for send-only sockets.
		/// </summary>
		/// <param name="ep"></param>
		/// <param name="port"></param>
		public UdpSockReader(IPEndPoint ep, int port)
		{
			// We need to bind it to server main port.
			// Stupid FL sends the message to where the reply came from;
			// We could use it in multi-server arch later.
			var localpt = new IPEndPoint(IPAddress.Any, port);
			_client = new UdpClient();
			_ep = ep;

			if (Type.GetType("Mono.Runtime") == null)
			{
				const int sioUdpConnreset = -1744830452;
				_client.Client.IOControl((IOControlCode)sioUdpConnreset, new byte[] { 0, 0, 0, 0 }, null);
			}

			_client.Client.SetSocketOption(
				SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
			_client.Client.Bind(localpt);

		}



		public void Handle(StartListeningMessage message)
		{
			if (!_client.Client.IsBound) {
				var log = Akka.Event.Logging.GetLogger(Context);
				log.Error("{0} has asked unbound socket to listen!",Context.Sender.Path);
				return;
			}

		    var parent = Context.Parent;
			while (true)
			{
				var remoteIpEndPoint = new IPEndPoint(IPAddress.Any, 0);
				var res = _client.Receive(ref remoteIpEndPoint);
				parent.Tell(new SockRxMsg(res,remoteIpEndPoint));
			}
		}




		/// <summary>
		/// Send the byte data to socket.
		/// </summary>
		/// <param name="message"></param>
		public void Handle(byte[] message)
		{

		   //logger.Trace("s>c packet {0}",BitConverter.ToString(message));
			_client.Send(message, message.Length, _ep);
		}

		private readonly IPEndPoint _ep;

		public void Handle(string message)
		{
			switch (message)
			{
				case "GetDPlaySession":
					var ret = Context.ActorSelection("../dplay-session").ResolveOne(TimeSpan.FromSeconds(1));
					ret.Wait(250);
					Sender.Tell(ret.Result);
					break;
			}
		}
	}
}
