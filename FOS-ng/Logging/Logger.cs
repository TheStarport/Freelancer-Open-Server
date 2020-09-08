using System;
using System.IO;
using System.Net;
using System.Text;

namespace FOS_ng.Logging
{
    public struct LogSettings
    {
        public bool LogToFile;
        public bool LogDPlay;
        public bool LogFLMsg;
        public bool LogFLMsg2;
        public LogSettings(bool logToFile, bool logDPlay, bool logFLMsg, bool logFLMsg2) : this()
        {
            LogToFile = logToFile;
            LogDPlay = logDPlay;
            LogFLMsg = logFLMsg;
            LogFLMsg2 = logFLMsg2;
        }
    }

    public static class Logger
    {
        public delegate void MessageHandler(string msg);

        public static bool[] LogMask = new bool[6];

        private static StreamWriter _fileStream;
        private static bool _filelog;

        static Logger()
        {
            LogMask[(int) LogType.Cheating] = true;
        }


        public static void AddLog(LogType type, string format, params object[] args)
        {
            if (!LogMask[(int) type])
                return;

            AddLog(type, String.Format(format, args));
        }

        public static event MessageHandler OnMessage;

        public static void SetFileLogging(bool set)
        {
            _filelog = set;
            if (set)
            {
                if (_fileStream == null)
                {
                    _fileStream = new StreamWriter(@".\server.log", true) {AutoFlush = true};
                }
            }
            else
            {
                if (_fileStream != null)
                {
                    _fileStream.Close();
                }
            }
        }

        public static void AddLog(LogType type, string message)
        {
            if (!LogMask[(int) type])
                return;

            message = DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToLongTimeString() + ": " + message;

            if (_filelog)
            {
                _fileStream.WriteLine(message);
            }

            if (OnMessage != null)
            {
                OnMessage(message);
            }

            Console.WriteLine(message);
        }
    }
}