using static Akka.Event.LogLevel;

namespace Neo
{
    public enum LogLevel : byte
    {
        Debug = DebugLevel,
        Info = InfoLevel,
        Warning = WarningLevel,
        Error = ErrorLevel,
        Fatal = Error + 1
    }
}
