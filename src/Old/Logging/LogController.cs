namespace FLServer
{
    public enum LogType
    {
        GENERAL,
        ERROR,
        FL_MSG,
        FL_MSG2,
        DPLAY_MSG,
        CHEATING
    }

    public interface ILogController
    {
        void AddLog(LogType type, string format, params object[] args);
    }
}