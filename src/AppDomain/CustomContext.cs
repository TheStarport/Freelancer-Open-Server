using System;
using System.Windows.Forms;
using FLDataFile;
using FLServer.Actors;
using FLServer.Logging;

namespace FLServer.AppDomain
{

    internal class CustomContext : ApplicationContext
    {
        private DPServer _server;
        private Init _init;
        private readonly Logger _logger;
        public string AccountDir;
        #region "singleton"

        private static CustomContext _currContext;

        public CustomContext CurrentContext
        {
            get { return _currContext; }
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="CustomContext" /> class.
        /// </summary>
        public CustomContext()
        {
            if (_currContext == null)
            {
                _currContext = this;
            }
        }

        #endregion

        /// <summary>
        ///     Initializes a new instance of the <see cref="CustomContext" /> class.
        /// </summary>
        public CustomContext(bool headless, LogSettings lset)
        {
            if (_currContext == null)
            {
                _currContext = this;
            }

            //merge with server settings?
            var cfgFile = new DataFile("flopenserver.cfg");
            AccountDir = cfgFile.GetSetting("server", "acct_path")[0];
            if (AccountDir == "")
            {
                AccountDir = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) +
                           @"\My Games\Freelancer\Accts\MultiPlayer";
            }
            _logger = new Logger(lset.LogToFile);
            _logger.LogMask[(int) LogType.ERROR] = true;
            _logger.LogMask[(int) LogType.GENERAL] = true;
            _logger.LogMask[(int) LogType.DPLAY_MSG] = lset.LogDPlay;
            _logger.LogMask[(int) LogType.FL_MSG] = lset.LogFLMsg;
            _logger.LogMask[(int) LogType.FL_MSG2] = lset.LogFLMsg2;



            _init = new Init();

            _init.Start(int.Parse(cfgFile.GetSetting("server", "port")[0]), cfgFile.GetSetting("server", "fl_path")[0]);
            //if (headless)
            //{
            //    _server = new DPServer(_logger);
            //}
            //else
            //{
            //    (new ControlWindow()).Show();
            //} 
        }

        public void StartServer()
        {
            _server = new DPServer(_logger);
        }

        public void StopServer()
        {
            if (_server != null)
            {
                _server.Dispose();
                _server = null;
            }

            _logger.AddLog(LogType.GENERAL, "Server stopped");
        }

        public DPServer GetServer
        {
            get { return _server ?? (_server = new DPServer(_logger)); }
        }

        public Logger GetLogger
        {
            get { return _logger; }
        }
    }
}