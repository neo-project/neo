using static Akka.Event.LogLevel;

namespace Neo
{
    /// <summary>
    /// Represents the level of logs.
    /// </summary>
    public enum LogLevel : byte
    {
        /// <summary>
        /// The debug log level.
        /// </summary>
        Debug = DebugLevel,

        /// <summary>
        /// The information log level.
        /// </summary>
        Info = InfoLevel,

        /// <summary>
        /// The warning log level.
        /// </summary>
        Warning = WarningLevel,

        /// <summary>
        /// The error log level.
        /// </summary>
        Error = ErrorLevel,

        /// <summary>
        /// The fatal log level.
        /// </summary>
        Fatal = Error + 1
    }
}
