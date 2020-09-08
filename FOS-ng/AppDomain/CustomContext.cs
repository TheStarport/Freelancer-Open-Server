using System;
using System.Windows.Forms;
using FOS_ng.Logging;

namespace FOS_ng.AppDomain
{
    struct ServerSettings
    {
        
    }
    internal class CustomContext : ApplicationContext
    {
        private DirectplayServer.Server _server;
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
            var cfgFile = new FLDataFile("flopenserver.cfg", false);
            AccountDir = cfgFile.GetSetting("server", "acct_path").Str(0);
            if (AccountDir == "")
            {
                AccountDir = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) +
                           @"\My Games\Freelancer\Accts\MultiPlayer";
            }
            _logger = new Logger(lset.LogToFile);
            _logger.LogMask[(int) LogType.Error] = true;
            _logger.LogMask[(int) LogType.General] = true;
            _logger.LogMask[(int) LogType.DplayMsg] = lset.LogDPlay;
            _logger.LogMask[(int) LogType.FLMsg] = lset.LogFLMsg;
            _logger.LogMask[(int) LogType.FLMsg2] = lset.LogFLMsg2;

            if (headless)
            {
                _server = new DPServer(_logger);
            }
            else
            {
                (new ControlWindow()).Show();
            }
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