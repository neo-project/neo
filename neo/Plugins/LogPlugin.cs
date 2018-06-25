using System.Collections.Generic;

namespace Neo.Plugins
{
    public abstract class LogPlugin : Plugin
    {
        private static readonly List<LogPlugin> instances = new List<LogPlugin>();

        public new static IEnumerable<LogPlugin> Instances => instances;

        protected LogPlugin()
        {
            instances.Add(this);
        }

        public static void Log(string source, LogLevel level, string message)
        {
            foreach (LogPlugin plugin in instances)
                plugin.OnLog(source, level, message);
        }

        internal protected virtual void OnLog(string source, LogLevel level, string message) { }
    }
}
