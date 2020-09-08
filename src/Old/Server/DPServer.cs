using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using FLServer.buf;
using FLServer.DataWorkers;
using FLServer.Object.Base;
using FLServer.Object.Solar;
using FLServer.Player;
using FLServer.Server;
using FLServer.Solar;
using Ionic.Zlib;

namespace FLServer
{
    public class DPServer : Reactor
    {
        /// <summary>
        ///     The freelancer instance GUID. Copied from the real freelancer server application.
        /// </summary>
        public static byte[] ApplicationInstanceGUID =
        {
            0xa8, 0xc6, 0x27, 0x1d, 0x41, 0x66, 0xd8, 0x49, 0x89, 0xeb, 0x1e,
            0xbc, 0x42, 0x21, 0xca, 0xe9
        };

        /// <summary>
        ///     The freelancer GUID. Copied from the real freelancer server application.
        /// </summary>
        public static byte[] ApplicationGUID =
        {
            0x26, 0xf0, 0x90, 0xa6, 0xf0, 0x26, 0x57, 0x4e, 0xac, 0xa0, 0xec, 0xf8,
            0x68, 0xe4, 0x8d, 0x21
        };

        /// <summary>
        ///     The log message receiver.
        /// </summary>
        private readonly ILogController _log;

        private readonly Random _rand = new Random();

        /// <summary>
        /// </summary>
        private readonly Dictionary<StarSystem, DPGameRunner> _runners = new Dictionary<StarSystem, DPGameRunner>();

        /// <summary>
        ///     The path to the accounts directory.
        /// </summary>
        public string AcctPath;

        /// <summary>
        ///     Message shown for .help command
        /// </summary>
        public string AdminHelpMsg;

        /// <summary>
        ///     Message shown as chat text every 10 minutes or so.
        /// </summary>
        public string BannerMsg;

        /// <summary>
        /// </summary>
        private FLDataFile _cfgFile;

        private DirectPlayServer _dplay;

        /// <summary>
        ///     The path to the freelancer root directory.
        /// </summary>
        public string FLPath;

        /// <summary>
        ///     Message shown on first connection to server.
        /// </summary>
        public string IntroMsg;

        /// <summary>
        ///     A map of direct-play ids to freelancer player controllers. A player controller manages all
        ///     communications for a single player and will make connections to proxied servers if
        ///     necessary.
        /// </summary>
        public Dictionary<Session, Player.Player> Players = new Dictionary<Session, Player.Player>();

        /// <summary>
        ///     The description to show on the server selection screen.
        /// </summary>
        public string ServerDescription;

        /// <summary>
        ///     The secret unique ID for this particular server used to identify the server
        ///     and the accounts to the client.
        /// </summary>
        public string ServerID;

        /// <summary>
        ///     The news to show on the character selection screen.
        /// </summary>
        public string server_news;

        /// <summary>
        ///     The Freelancer client version number used to determine if the client is allowed to
        ///     connect to this server.
        /// </summary>
        public string server_version;

        /// <summary>
        ///     Message shown for /help command
        /// </summary>
        public string UserHelpMsg;

        /// <summary>
        ///     Start things running
        /// </summary>
        /// <param name="log"></param>
        public DPServer(ILogController log)
        {
            _log = log;

            // Start the game simulation thread
            var masterThread = new Thread(ThreadRun);
            masterThread.Start();
        }

        /// <summary>
        ///     Return true if the playerid is in use.
        /// </summary>
        /// <param name="playerid"></param>
        /// <returns></returns>
        private bool IsPlayerIDInUse(uint playerid)
        {
            return Players.Any(item => item.Value.FLPlayerID == playerid);
        }

        /// <summary>
        ///     Allocate an unused FL player ID.
        /// </summary>
        /// <returns></returns>
        private uint NewPlayerID()
        {
            uint i = 0;
// ReSharper disable once EmptyEmbeddedStatement
            while (IsPlayerIDInUse(++i)) ;
            return i;
        }

        /// <summary>
        ///     Send a message to the dplay stack for transmission to a freelancer client.
        ///     Compress the message if it's longer than 80 bytes or so.
        /// </summary>
        /// <param name="dplayid"></param>
        /// <param name="msg"></param>
        public void SendMessage(Session session, byte[] msg)
        {
            if (msg[0] != 0xFF)
                _log.AddLog(LogType.FL_MSG, "s>c client={0} tx={1}", session.Client, msg);

            if (msg.Length > 0x50)
            {
                using (var ms = new MemoryStream())
                {
                    using (var zs = new ZlibStream(ms, CompressionMode.Compress))
                        zs.Write(msg, 0, msg.Length);
                    msg = ms.ToArray();
                }
            }

            _dplay.SendTo(session, msg);
        }

        public string GetConnectionInformation(Player.Player player)
        {
            return String.Format("ping={0} loss={1}/{2} rx/tx={3}/{4} tx_queue={5}", player.DPSess.Rtt,
                player.DPSess.LostRx, player.DPSess.LostTx,
                player.DPSess.BytesRx, player.DPSess.BytesTx,
                player.DPSess.UserData.Count + player.DPSess.UserDataPendingAck.Count
                );
        }

        /// <summary>
        ///     Shutdown the server and exit all threads.
        /// </summary>
        public void Dispose()
        {
            OnShutdown();
            if (_dplay != null)
            {
                _dplay.Dispose();
            }
        }

        public event EventHandler Shutdown;

        protected virtual void OnShutdown()
        {
            EventHandler handler = Shutdown;
            if (handler != null) handler(this, EventArgs.Empty);
        }

        public Player.Player FindPlayerByAccountID(string accountid)
        {
            return Players.Values.FirstOrDefault(player => player.AccountID == accountid);
        }

        /// <summary>
        ///     If vanilla flserver configuration files exist, then read the configuration
        ///     settings from these and override the settings in our the flos configuration file.
        /// </summary>
        private void ReadFLDefaults()
        {
            // Read flserver.cfg
            if (File.Exists(AcctPath + "\\flserver.cfg"))
            try
            {
                byte[] buf;
                using (
                    var fs = new FileStream(AcctPath + "\\flserver.cfg", FileMode.Open, FileAccess.Read,
                        FileShare.ReadWrite))
                {
                    buf = new byte[fs.Length];
                    fs.Read(buf, 0, (int) fs.Length);
                    fs.Close();
                }

                int pos = 0;
                FLMsgType.GetUInt32(buf, ref pos); // dunno
                string server_name = FLMsgType.GetUnicodeString(buf, ref pos, 0x21);
                string server_description = FLMsgType.GetUnicodeString(buf, ref pos, 0x81);
                string server_password = FLMsgType.GetUnicodeString(buf, ref pos, 0x11);
                FLMsgType.GetUInt8(buf, ref pos); // dunno probably "allow new players"
                FLMsgType.GetUInt8(buf, ref pos); // dunno probably "make your server internet access.."
                FLMsgType.GetUInt32(buf, ref pos); // number of players
                FLMsgType.GetUInt8(buf, ref pos); // dunno probably "allow  players to harm"
            }
            catch
            {
            }

            // Read accounts.cfg
            try
            {
                byte[] buf;
                using (
                    var fs = new FileStream(AcctPath + "\\accounts.cfg", FileMode.Open, FileAccess.Read,
                        FileShare.ReadWrite))
                {
                    buf = new byte[fs.Length];
                    fs.Read(buf, 0, (int) fs.Length);
                    fs.Close();
                }

                int pos = 0;
                FLMsgType.GetUInt32(buf, ref pos); // dunno
                FLMsgType.GetUInt32(buf, ref pos); // dunno
                FLMsgType.GetAsciiString(buf, ref pos, 0x24); // server id sig
                ServerID = FLMsgType.GetAsciiString(buf, ref pos, 0x24);
            }
            catch
            {
            }
        }

        public void ThreadRun()
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

            try
            {
                _cfgFile = new FLDataFile("flopenserver.cfg", false);
                FLPath = _cfgFile.GetSetting("server", "fl_path").Str(0);
                AcctPath = _cfgFile.GetSetting("server", "acct_path").Str(0);

                if (AcctPath == "")
                {
                    AcctPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) +
                               @"\My Games\Freelancer\Accts\MultiPlayer";
                }

                if (_cfgFile.SettingExists("server", "server_id"))
                    ServerID = _cfgFile.GetSetting("server", "server_id").Str(0);

                if (_cfgFile.SettingExists("server", "version"))
                {
                    server_version = _cfgFile.GetSetting("server", "version").Str(0);
                }

                if (server_version == "")
                {
                    FileVersionInfo myFileVersionInfo = FileVersionInfo.GetVersionInfo(FLPath + @"\EXE\Freelancer.exe");
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

                    server_version = sb.ToString().Substring(1);
                }


                ServerDescription = _cfgFile.GetSetting("server", "description").Str(0);
                server_news = _cfgFile.GetSetting("server", "news").Str(0);

                foreach (FLDataFile.Setting set in _cfgFile.GetSettings("intro_msg"))
                    IntroMsg += set.Line;

                foreach (FLDataFile.Setting set in _cfgFile.GetSettings("banner_msg"))
                    BannerMsg += set.Line;

                foreach (FLDataFile.Setting set in _cfgFile.GetSettings("user_help_msg"))
                    UserHelpMsg += set.Line;

                foreach (FLDataFile.Setting set in _cfgFile.GetSettings("admin_help_msg"))
                    AdminHelpMsg += set.Line;
            }
            catch (Exception e)
            {
                _log.AddLog(LogType.ERROR, "error: flopenserver.cfg not found or missing parameter " + e.Message);
                return;
            }

            string name = _cfgFile.GetSetting("server", "name").Str(0);
            uint port = _cfgFile.GetSetting("server", "port").UInt(0);
            var max_players = (int) _cfgFile.GetSetting("server", "max_players").UInt(0);

            if (ServerID == null)
                ReadFLDefaults();

            _log.AddLog(LogType.GENERAL, "cfg_file = " + _cfgFile.FilePath);
            _log.AddLog(LogType.GENERAL, "fl_path = " + FLPath);
            _log.AddLog(LogType.GENERAL, "acct_path = " + AcctPath);
            _log.AddLog(LogType.GENERAL, "server_id = " + ServerID);
            _log.AddLog(LogType.GENERAL, "version = " + server_version);
            _log.AddLog(LogType.GENERAL, "name = " + name);
            _log.AddLog(LogType.GENERAL, "port = " + port);
            _log.AddLog(LogType.GENERAL, "max_players = " + max_players);


            ArchetypeDB.Load(FLPath, _log);
            UniverseDB.Load(FLPath, _log);
            News.Load(FLPath, _log);
            _log.AddLog(LogType.GENERAL, "Ready to start");

            try
            {
                string[] arg = server_version.Split('.');
                uint major = uint.Parse(arg[0]) & 0xFF;
                uint minor = uint.Parse(arg[1]) & 0xFF;
                uint patch = uint.Parse(arg[2]) & 0xFF;
                uint build = uint.Parse(arg[3]) & 0xFF;
                uint version = (major << 24) | (minor << 16) | (patch << 8) | build;
                server_version = version.ToString(CultureInfo.InvariantCulture);
            }
            catch
            {
                _log.AddLog(LogType.ERROR, "invalid server version string");
                server_version = "0";
            }

            // Try to start the direct play server. Complain if it fails but don't pass the exception on.
            try
            {
                _dplay = new DirectPlayServer(this, _log, (int) port)
                {
                    server_name = name + "\0",
                    server_id = ServerID,
                    server_description = ServerDescription + "\0",
                    server_version = server_version,
                    max_players = (uint) max_players
                };
                _log.AddLog(LogType.GENERAL, "Server started");
            }
            catch (Exception e)
            {
                _log.AddLog(LogType.ERROR, "Cannot open socket reason={0}", e.Message);
            }

            _dplay.GotMessage += _dplay_GotMessage;
            _dplay.PlayerConnected += _dplay_PlayerConnected;
            _dplay.PlayerDestroyed += _dplay_PlayerDestroyed;

            // Kick off the game runner threads. We have one thread per system. New
            // players who have not selected a character are assigned to a random
            // runner thread.            
            uint baseObjid = 1;
            foreach (var starSystem in UniverseDB.Systems.Values)
            {
                _runners[starSystem] = new DPGameRunner(this, _log, baseObjid, starSystem);
                baseObjid += 10000;
            }
        }

        void _dplay_PlayerDestroyed(Session sess, string reason)
        {
            _log.AddLog(LogType.GENERAL, "Player destroyed client={0} reason={1}", sess.Client, reason);

            //AddEvent(new DPSessionTerminatedEvent(sess));

            // The session has died. Tell all game threads that it is dead and the player
            // is gone.
            // TODO: save player?
            if (!Players.ContainsKey(sess)) return;

            var player = Players[sess];
            Players.Remove(sess);

            //foreach (var runner in _runners.Values)
                //runner.AddEvent(new DPGameRunnerPlayerDeletedEvent(player.FLPlayerID));

            player.OnPlayerDeleted();

            // Kill the dplay connection if it is not already dead
            //try
            //{
                /* fixme: dplay.DestroyClient(server_event.dplayid, null); */
            //}
            //catch
            //{
            //}

        }



        void _dplay_PlayerConnected(object sender, Session e)
        {
            _log.AddLog(LogType.GENERAL, "Player created name={0}", e.Client);

            var defaultRunner = _runners.ElementAt(_rand.Next(_runners.Count - 1)).Value;

            var player = new Player.Player(e, _log, NewPlayerID(), defaultRunner);
            player.RunnerUpdate += player_RunnerUpdate;
            Players[e] = player;
            defaultRunner.LinkPlayer(player);
            player.Update();

        }

        void player_RunnerUpdate(Player.Player sender)
        {
            // A game thread has sent a notification to say that the player has changed - either
            // name, rank or system. We might change the assigned thread as a result.
            // In any case, we notify all game threads of the current name, rank and system.
            //var serverEvent = nextEvent as DPGameRunnerPlayerUpdateEvent;

            // Find runner for this system and if it's changed tell the old and new runners
            // of the change in ownership.
            var currRunner = sender.Runner;
            if (sender.Ship.System == null) return;
            var newRunner = _runners[sender.Ship.System];

            if (newRunner == currRunner || newRunner == null) return;

            sender.Runner = newRunner;
            newRunner.LinkPlayer(sender);
            sender.Update();


            // Notify all game threads of the current player info so they can update their 
            // player lists.
            //foreach (var runner in _runners.Values)
                //runner.AddEvent(serverEvent);
        }

        void _dplay_GotMessage(Session sess, byte[] message)
        {
            // Decompress the message if it is compressed.
            if (message[0] == FLMsgType.MSG_TYPE_COMPRESSED)
            {
                using (var ms = new MemoryStream(message, 0, message.Length))
                {
                    using (var zs = new ZlibStream(ms, CompressionMode.Decompress))
                    {
                        var buf = new byte[32767];
                        int msgLength = zs.Read(buf, 0, buf.Length);
                        Array.Resize(ref buf, msgLength);
                        message = buf;
                    }
                }
            }

            // Otherwise dispatch the message to the controller.
            if (message[0] != 0x01) //not a typo, log junk cleaning
                _log.AddLog(LogType.FL_MSG, "c>s client={0} rx={1}", sess.Client, message);

            //AddEvent(new DPSessionRxMessageFromClient(sess, message));


            if (!Players.ContainsKey(sess)) return;
            var player = Players[sess];
            //player.Runner.AddEvent(new DPGameRunnerRxMsgEvent(player, message));
            player.OnRxMsgToRunner(message);
        }
    }
}