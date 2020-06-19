using Akka.Actor;
using Akka.Event;
using Microsoft.Extensions.Configuration;
using Neo.Plugins;
using System;
using System.IO;
using System.Reflection;
using System.Text;

namespace Neo
{
    public static class Utility
    {
        internal class Logger : ReceiveActor
        {
            public Logger()
            {
                Receive<InitializeLogger>(_ => Sender.Tell(new LoggerInitialized()));
                Receive<LogEvent>(e => Log(e.LogSource, (LogLevel)e.LogLevel(), e.Message));
            }
        }

        public static Encoding StrictUTF8 { get; }

        static Utility()
        {
            StrictUTF8 = (Encoding)Encoding.UTF8.Clone();
            StrictUTF8.DecoderFallback = DecoderFallback.ExceptionFallback;
            StrictUTF8.EncoderFallback = EncoderFallback.ExceptionFallback;
        }

        /// <summary>
        /// Load configuration with different Environment Variable
        /// </summary>
        /// <param name="config">Configuration</param>
        /// <returns>IConfigurationRoot</returns>
        public static IConfigurationRoot LoadConfig(string config)
        {
            var env = Environment.GetEnvironmentVariable("NEO_NETWORK");
            var configFile = string.IsNullOrWhiteSpace(env) ? $"{config}.json" : $"{config}.{env}.json";

            // Working directory
            var file = Path.Combine(Environment.CurrentDirectory, configFile);
            if (!File.Exists(file))
            {
                // EntryPoint folder
                file = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), configFile);
                if (!File.Exists(file))
                {
                    // neo.dll folder
                    file = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), configFile);
                    if (!File.Exists(file))
                    {
                        // default config
                        return new ConfigurationBuilder().Build();
                    }
                }
            }

            return new ConfigurationBuilder()
                .AddJsonFile(file, true)
                .Build();
        }

        public static void Log(string source, LogLevel level, object message)
        {
            foreach (ILogPlugin plugin in Plugin.Loggers)
                plugin.Log(source, level, message);
        }
    }
}
