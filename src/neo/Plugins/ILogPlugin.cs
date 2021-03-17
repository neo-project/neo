namespace Neo.Plugins
{
    /// <summary>
    /// A plug-in interface for logs.
    /// </summary>
    public interface ILogPlugin
    {
        /// <summary>
        /// Writes a new log to the plugin.
        /// </summary>
        /// <param name="source">The source of the log. Used to identify the producer of the log.</param>
        /// <param name="level">The level of the log.</param>
        /// <param name="message">The message of the log.</param>
        void Log(string source, LogLevel level, object message);
    }
}
