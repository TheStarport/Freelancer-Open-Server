using System;

namespace FLServer.Plugins
{
    public delegate void LogHandler(string message);


    public interface IFOSPlugin
    {
        //IPlugData[] GetData();
        //PlugDataEditControl GetEditControl(IPlugData Data);

        event LogHandler LogEvent;
        bool Save(string path);
    }


    [AttributeUsage(AttributeTargets.Class)]
    public class PlugDisplayNameAttribute : Attribute
    {
        private readonly string _displayName;

        public PlugDisplayNameAttribute(string displayName)
        {
            _displayName = displayName;
        }

        public override string ToString()
        {
            return _displayName;
        }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class PlugDescriptionAttribute : Attribute
    {
        private readonly string _description;

        public PlugDescriptionAttribute(string description)
        {
            _description = description;
        }


        public override string ToString()
        {
            return _description;
        }
    }
}