using System;
using System.Globalization;
using System.Windows.Forms;
using FLServer.AppDomain;
using FLServer.Logging;

namespace FLServer
{
    internal static class Program
    {
        /// <summary>
        ///     The main entry point for the application.
        /// </summary>
        [STAThread]
        private static void Main(string[] args)
        {
            bool headless = false;
            bool logDplay = false;
            bool logFlmsg = false;
            bool logFlmsg2 = false;
            var logFile = false;
            foreach (string arg in args)
            {
                switch (arg)
                {
                    case "--headless":
                        headless = true;
                        break;
                    case "--log-dplay":
                        logDplay = true;
                        break;
                    case "--log-flmsg":
                        logFlmsg = true;
                        break;
                    case "--log-flmsg2":
                        logFlmsg2 = true;
                        break;
                    case "--log-file":
                        logFile = true;
                        break;
                }
            }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.CurrentCulture = CultureInfo.InvariantCulture;
            CustomContext curContext =
                (new CustomContext(headless, new LogSettings(logFile, logDplay, logFlmsg, logFlmsg2)))
                    .CurrentContext;
            Application.Run(curContext);
            //var init = new Actors.Init(2310);
        }
    }
}