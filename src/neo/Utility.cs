using Akka.Actor;
using Akka.Event;
using Microsoft.Extensions.Configuration;
using Neo.Plugins;
using System;

namespace Neo
{
    public static class Utility
    {
        internal class Logger : ReceiveActor
        {
            public Logger()
            {
                Receive<Debug>(e => OnLog(LogLevel.Debug, e.ToString()));
                Receive<Info>(e => OnLog(LogLevel.Info, e.ToString()));
                Receive<Warning>(e => OnLog(LogLevel.Warning, e.ToString()));
                Receive<Error>(e => OnLog(LogLevel.Error, e.ToString()));
                Receive<InitializeLogger>(_ => Init(Sender));
            }

            private void Init(IActorRef sender)
            {
                sender.Tell(new LoggerInitialized());
            }

            private void OnLog(LogLevel level, string message)
            {
                Log<NeoSystem>(level, message);
            }
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
            return new ConfigurationBuilder()
                .AddJsonFile(configFile, true)
                .Build();
        }

        public static void Log(string source, LogLevel level, string message)
        {
            foreach (ILogPlugin plugin in Plugin.Loggers)
                plugin.Log(source, level, message);
        }

        public static void Log<T>(LogLevel level, string message)
        {
            Log(typeof(T).Name, level, message);
        }
    }
}
