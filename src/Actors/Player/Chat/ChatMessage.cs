namespace FLServer.Actors.Player.Chat
{
    public class SystemMessage
    {
        public string Name;
        public string Message;
    }

    public class LocalMessage
    {
        public string Name;
        public string Message;
    }

    public class ConsoleMessage
    {
        public string Message;

        public ConsoleMessage(string msg)
        {
            Message = msg;
        }
    }
}
