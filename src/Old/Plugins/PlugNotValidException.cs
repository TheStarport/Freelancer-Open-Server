using System;

namespace FLServer.Plugins
{
    public class PlugNotValidException : Exception
    {
        public PlugNotValidException(Type type, string message)
            : base("The plug-in " + type.Name + " is not valid\n" + message)
        {
        }
    }
}