namespace Neo.Plugins
{
    public interface ILogPlugin
    {
        void Log(string source, LogLevel level, string message);
    }
}
