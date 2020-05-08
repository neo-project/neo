namespace Neo.Plugins
{
    public interface ILogPlugin
    {
        int Order { get; }
        void Log(string source, LogLevel level, object message);
    }
}
