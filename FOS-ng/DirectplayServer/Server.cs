using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using FLDataFile;
using FOS_ng.Logging;
using FOS_ng.Player;
using FOS_ng.Universe;
using Ionic.Zlib;

namespace FOS_ng.DirectplayServer
{
    public class Server
    {
        /// <summary>
        ///     The freelancer instance GUID. Copied from the real freelancer server application.
        /// </summary>
        private static byte[] _applicationInstanceGUID =
        {
            0xa8, 0xc6, 0x27, 0x1d, 0x41, 0x66, 0xd8, 0x49, 0x89, 0xeb, 0x1e,
            0xbc, 0x42, 0x21, 0xca, 0xe9
        };

        /// <summary>
        ///     The freelancer GUID. Copied from the real freelancer server application.
        /// </summary>
        private static byte[] _applicationGUID =
        {
            0x26, 0xf0, 0x90, 0xa6, 0xf0, 0x26, 0x57, 0x4e, 0xac, 0xa0, 0xec, 0xf8,
            0x68, 0xe4, 0x8d, 0x21
        };

        private readonly Random _rand = new Random();

        /// <summary>
        ///     The secret unique ID for this particular server used to identify the server
        ///     and the accounts to the client.
        /// </summary>
        private string _serverId;

        /// <summary>
        ///     The Freelancer client version number used to determine if the client is allowed to
        ///     connect to this server.
        /// </summary>
        private string _serverVer;

        private MessagePump _pump;

        /// <summary>
        ///     The description to show on the server selection screen.
        /// </summary>
        private string _serverDesc;

        /// <summary>
        ///     The news to show on the character selection screen.
        /// </summary>
        private string _serverNews;

        private string _flPath;
        private string _dbPath;

        public DirectplayServer Dplay;


        public Server()
        {
            var masterThread = new Thread(ThreadRun);
            masterThread.Start();
        }


        /// <summary>
        ///     Allocate an unused FL player ID.
        /// </summary>
        /// <returns></returns>
        private uint NewPlayerID()
        {
            uint i = 0;
            while (IsPlayerIDInUse(++i))
            {
            }
            return i;
        }

                /// <summary>
        ///     Return true if the playerid is in use.
        /// </summary>
        /// <param name="playerid"></param>
        /// <returns></returns>
        private bool IsPlayerIDInUse(uint playerid)
        {
            return _pump.Players.Any(item => item.Value.FLPlayerID == playerid);
        }

        public void ThreadRun()
        {

            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

            var cfgFile = new DataFile(@"fos.cfg");
            _flPath = cfgFile.GetSetting("server", "fl_path")[0];
            _dbPath = cfgFile.GetSetting("server", "acct_path")[0];

                if (_dbPath == "")
                    _dbPath = @"accounts.db";

                var tmpCfg = cfgFile.GetSetting("server", "server_id");
                if (tmpCfg != null)
                    _serverId = tmpCfg[0];

                tmpCfg = cfgFile.GetSetting("server", "version");
                if (tmpCfg != null)
                {
                    _serverVer = tmpCfg[0];
                }

                if (_serverVer == "")
                {
                    var myFileVersionInfo = FileVersionInfo.GetVersionInfo(_flPath + @"\EXE\Freelancer.exe");
                    var ver = new int[4];
                    ver[0] = myFileVersionInfo.FileMajorPart;
                    ver[1] = myFileVersionInfo.FileMinorPart;
                    ver[2] = myFileVersionInfo.FileBuildPart;
                    ver[3] = myFileVersionInfo.FilePrivatePart;

                    var sb = new StringBuilder();
                    foreach (var num in ver)
                    {
                        sb.Append(".");
                        if (num < 10)
                        {
                            sb.Append("0");
                        }
                        sb.Append(num);
                    }

                    _serverVer = sb.ToString().Substring(1);
                }


                _serverDesc = cfgFile.GetSetting("server", "description")[0];
                _serverNews = cfgFile.GetSetting("server", "news")[0];

                //TODO: move dis to Chat
                //foreach (FLDataFile.Setting set in cfgFile.GetSettings("intro_msg"))
                //    IntroMsg += set.Line;

                //foreach (FLDataFile.Setting set in cfgFile.GetSettings("banner_msg"))
                //    BannerMsg += set.Line;

                //foreach (FLDataFile.Setting set in cfgFile.GetSettings("user_help_msg"))
                //    _userHelpMsg += set.Line;

                //foreach (FLDataFile.Setting set in cfgFile.GetSettings("admin_help_msg"))
                //    AdminHelpMsg += set.Line;

            var name = cfgFile.GetSetting("server", "name")[0];
            var port = uint.Parse(cfgFile.GetSetting("server", "port")[0]);
            var maxPlayers = int.Parse(cfgFile.GetSetting("server", "max_players")[0]);

            //TODO: do we really need vanilla conf?
            //if (_serverId == null)
            //    ReadFLDefaults();

            Logger.AddLog(LogType.General, "cfgFile = " + cfgFile.Path);
            Logger.AddLog(LogType.General, "flPath = " + _flPath);
            Logger.AddLog(LogType.General, "dbPath = " + _dbPath);
            Logger.AddLog(LogType.General, "Server ID = " + _serverId);
            Logger.AddLog(LogType.General, "Version = " + _serverVer);
            Logger.AddLog(LogType.General, "Name = " + name);
            Logger.AddLog(LogType.General, "Port = " + port);
            Logger.AddLog(LogType.General, "Max P = " + maxPlayers);


            //ArchetypeDB.Load(FLPath, _log);
            //UniverseDB.Load(FLPath, _log);
            //News.Load(FLPath, _log);
            Logger.AddLog(LogType.General, "Ready to start");


                var arg = _serverVer.Split('.');
                if (arg.Count() != 4)
                {
                    Logger.AddLog(LogType.Error, "invalid server version string");
                    _serverVer = "0";
                }
                else
                {
                    //TODO: check the parsing
                    var major = uint.Parse(arg[0]) & 0xFF;
                    var minor = uint.Parse(arg[1]) & 0xFF;
                    var patch = uint.Parse(arg[2]) & 0xFF;
                    var build = uint.Parse(arg[3]) & 0xFF;
                    var version = (major << 24) | (minor << 16) | (patch << 8) | build;
                    _serverVer = version.ToString(CultureInfo.InvariantCulture);
                }
                
                
                


            // Try to start the direct play server. Complain if it fails but don't pass the exception on.
            try
            {
                Dplay = new DirectplayServer((int)port)
                {
                    ServerName = name + "\0",
                    ServerID = _serverId,
                    ServerDescription = _serverDesc + "\0",
                    ServerVersion = _serverVer,
                    MaxPlayers = (uint)maxPlayers
                };
                Logger.AddLog(LogType.General, "Server started");
            }
            catch (Exception e)
            {
                Logger.AddLog(LogType.Error, "Cannot open socket reason={0}", e.Message);
            }

            Dplay.GotMessage += _dplay_GotMessage;
            Dplay.PlayerConnected += _dplay_PlayerConnected;
            Dplay.PlayerDestroyed += _dplay_PlayerDestroyed;



            _pump = new MessagePump(this);


            // Run the event/timer processing loop for this thread.
            var currentTime = Utilities.GetTime();
            var running = true;
            while (running)
            {
                // Calculate the delta time.
                var delta = Utilities.GetTime() - currentTime;
                currentTime += delta;

                // Call the reactor to return the next event the process
                // and run any timer functions.
                var nextEvent = Run(currentTime, delta);


                if (nextEvent is DPGameRunnerPlayerUpdateEvent)
                {
                    // A game thread has sent a notification to say that the player has changed - either
                    // name, rank or system. We might change the assigned thread as a result.
                    // In any case, we notify all game threads of the current name, rank and system.
                    var serverEvent = nextEvent as DPGameRunnerPlayerUpdateEvent;

                    // Find runner for this system and if it's changed tell the old and new runners
                    // of the change in ownership.
                    var currRunner = serverEvent.runner;
                    var newRunner = _runners[serverEvent.system];
                    if (newRunner != currRunner)
                        serverEvent.runner = newRunner;

                    // Notify all game threads of the current player info so they can update their 
                    // player lists.
                    foreach (var runner in _runners.Values)
                        runner.AddEvent(serverEvent);
                }
                else if (nextEvent is DPSessionRxMessageFromClient)
                {
                    
                }
                else if (nextEvent is ReactorShutdownEvent)
                {
                    foreach (DPGameRunner runner in _runners.Values)
                        runner.AddEvent(new ReactorShutdownEvent());
                    running = false;
                }
            }

        }

        void _dplay_PlayerDestroyed(Session sess, string reason)
        {
            // The session has died. Tell all game threads that it is dead and the player
            // is gone.
            // TODO: save player?
            if (_pump.Players.ContainsKey(sess.DPlayID))
            {
                var player = _pump.Players[sess.DPlayID];
                _pump.Players.Remove(sess.DPlayID);

                foreach (var runner in _runners.Values)
                    runner.AddEvent(new DPGameRunnerPlayerDeletedEvent(player.FLPlayerID));
            }

            // Kill the dplay connection if it is not already dead
            //try
            //{
                //TODO: fixme: dplay.DestroyClient(server_event.dplayid, null);
            //}
            //catch
            //{
            //}
        }

        void _dplay_PlayerConnected(object sender, Session e)
        {

            // On receipt of a new connection, create a new player assigning it to a
            // random game thread. Notify all game threads of the new player.
            //var serverEvent = nextEvent as DPSessionConnectedEvent;

            var player = new Player.Player(e,e.DPlayID, NewPlayerID());

            _pump.PlayerUpdate(player);
        }

        void _dplay_GotMessage(Session sess, byte[] message)
        {
            

            switch (message[0])
            {
                // Decompress the message if it is compressed.
                case FLMsgType.MsgTypeCompressed:
                    using (var ms = new MemoryStream(message, 0, message.Length))
                    {
                        // TODO: it should be slow
                        using (var zs = new ZlibStream(ms, CompressionMode.Decompress))
                        {
                            var buf = new byte[32767];
                            int msgLength = zs.Read(buf, 0, buf.Length);
                            Array.Resize(ref buf, msgLength);
                            message = buf;
                        }
                    }
                    break;
                case 0x01:
                    return;
                // Otherwise dispatch the message to the controller.

            }

            
            Logger.AddLog(LogType.FLMsg, "c>s client={0} rx={1}", sess.Client, message);


            _pump.MessageFromClient(sess.DPlayID,message);
        }



    }
}
