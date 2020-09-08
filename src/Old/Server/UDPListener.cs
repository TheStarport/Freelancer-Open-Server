using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace FLServer.Server
{
	class UdpListener : UdpBase
	{
		public UdpListener(int port)
			: this(new IPEndPoint(IPAddress.Any, port))
		{
		}

		public UdpListener(IPEndPoint endpoint)
		{
			Client = new UdpClient(endpoint);



			// Apply iocntl to ignore ICMP responses from hosts when we send them
			// traffic otherwise the socket chucks and exception and dies. Ignore this
			// under mono.


			
#if __MonoCS__
			const int sioUdpConnreset = 0;
#else
			const int sioUdpConnreset = -1744830452;
#endif
			Client.Client.IOControl((IOControlCode)sioUdpConnreset, new byte[] { 0, 0, 0, 0 }, null);



		}

		public void Send(byte[] message, IPEndPoint endpoint)
		{
			Client.Send(message, message.Length, endpoint);
		}

		public void Close()
		{
			Client.Close();
		}

	}



	abstract class UdpBase
	{
		public UdpClient Client;

		protected UdpBase()
		{
			Client = new UdpClient();
		}

		public async Task<Received> Receive()
		{
			var result = await Client.ReceiveAsync();
			return new Received
			{
				Message = result.Buffer,
				Sender = result.RemoteEndPoint
			};
		}
	}

	public struct Received
	{
		public IPEndPoint Sender;
		public byte[] Message;
	}

}
