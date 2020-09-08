using System.Diagnostics;
using System;
namespace FLServer.Actors
{
    public static class ServerUtils
    {

        public static string WelcomeMessage = @"Welcome, $$player$$!";
		public static TimeSpan SessionTimeout = TimeSpan.FromSeconds(15);
        public static double GameTime()
        {
            return (Stopwatch.GetTimestamp()/(double) Stopwatch.Frequency);
        }

    }
}
