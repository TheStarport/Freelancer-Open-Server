using System;
using System.IO;
using System.Net;
using System.Text;
using FLServer.Physics;

namespace FLServer.Logging
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

    internal class Logger : ILogController
    {
        public delegate void MessageHandler(string msg);

        public bool[] LogMask = new bool[6];

        private StreamWriter _fileStream;
        private bool _filelog;

        public Logger()
        {
            LogMask[(int) LogType.CHEATING] = true;
        }

        public Logger(bool logToFile)
        {
            if (logToFile)
            {
                _filelog = true;
                _fileStream = new StreamWriter(@".\server.log", true) {AutoFlush = true};
            }
        }

        public void AddLog(LogType type, string format, params object[] args)
        {
            if (!LogMask[(int) type])
                return;

            for (int i = 0; i < args.Length; i++)
            {
                object obj = args[i];
                if (obj is byte[])
                {
                    args[i] = HexToAscii((byte[]) obj);
                }
                else if (obj is Vector)
                {
                    args[i] = String.Format("{0},{1},{2}", ((Vector) obj).x, ((Vector) obj).y, ((Vector) obj).z);
                }
                else if (obj is Quaternion)
                {
                    args[i] = String.Format("{0},{1},{2},{3}", ((Quaternion) obj).W, ((Quaternion) obj).I,
                        ((Quaternion) obj).J, ((Quaternion) obj).K);
                }
                else if (obj is IPEndPoint)
                {
                    args[i] = String.Format("{0}:{1}", ((IPEndPoint) obj).Address, ((IPEndPoint) obj).Port);
                }
            }

            AddLog(type, String.Format(format, args));
        }

        public event MessageHandler OnMessage;

        public void SetFileLogging(bool set)
        {
            _filelog = set;
            if (set)
            {
                if (_fileStream == null)
                {
                    _fileStream = new StreamWriter(@".\server.log", true);
                    _fileStream.AutoFlush = true;
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

        public void AddLog(LogType type, string message)
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

        private string HexToAscii(byte[] pkt)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < pkt.Length; i++)
                sb.AppendFormat("{0:X2}:", pkt[i]);
            return sb.ToString();
        }
    }
}