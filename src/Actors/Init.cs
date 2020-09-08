using Akka.Actor;
using NLog;

namespace FLServer.Actors
{
	class Init
	{
		private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

		public async void Start(int port, string flPath)
		{

			Logger.Info("Initiating server");
			
			var config = Akka.Configuration.ConfigurationFactory.ParseString(
					@"akka {
					loggers = [""Akka.NLog.Event.NLog.NLogLogger,Akka.NLog""]
					loglevel = DEBUG
				}"
				);

			Logger.Info("Loading universe from {0}...",flPath);
			await GameDB.UniverseDB.LoadUniverse(flPath);

			Logger.Info("Init: Starting server...");
			var serverSystem = new ActorSystem("sessions", config);
			Logger.Trace("Actor system started, initiating server actor...");
			serverSystem.ActorOf(Props.Create(() => new Server(port)), "server");

		}
		//TODO: split to router?
		
	}

}
