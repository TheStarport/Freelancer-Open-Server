using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace FLOpenServerProxy
{
    class Logger : Reactor
    {
        class LogEvent : ReactorEvent
        {
            public string message;

            public LogEvent(string message)
            {
                this.message = message;
            }
        }

        System.Threading.Thread thread;
        string logpath = "";

        bool off = false;
        bool console = false;

        public Logger(string logpath)
        {
            this.logpath = logpath;

            if (this.logpath == "console")
                console = true;
            else if (this.logpath == "off")
                off = true;

            thread = new System.Threading.Thread(ThreadRun);
            thread.Priority = System.Threading.ThreadPriority.Lowest;
            thread.Start();
        }

        public void AddLog(string message)
        {
            if (!off)
            {
                AddEvent(new LogEvent(DateTime.UtcNow.ToShortDateString() + " " + DateTime.UtcNow.ToLongTimeString() + ": " + message));
            }
        }

        void ThreadRun()
        {
            System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
            double current_time = GetTime();

            bool running = true;
            while (running)
            {
                // Calculate the delta time.
                double delta = GetTime() - current_time;
                current_time += delta;

                // Call the reactor to return the next event the process
                // and run any timer functions.
                ReactorEvent next_event = Run(current_time, delta);
                if (next_event is ReactorShutdownEvent)
                {
                    running = false;
                }
                else if (next_event is LogEvent)
                {
                    LogEvent log_event = next_event as LogEvent;

                    if (console)
                    {
                        Console.WriteLine(log_event.message);
                    }
                    else if (logpath.Length > 0)
                    {
                        StreamWriter writer = File.AppendText(logpath);
                        writer.WriteLine(log_event.message);
                        writer.Close();
                    }
                }

                System.Threading.Thread.Sleep(0);
            }
        }
    }
}
