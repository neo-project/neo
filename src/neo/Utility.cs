using Akka.Actor;
using Akka.Event;
using Microsoft.Extensions.Configuration;
using Neo.Plugins;
using System;
using System.IO;

namespace Neo
{
    public static class Utility
    {
        private static readonly object _lock = new object();

        internal class Logger : ReceiveActor
        {
            public Logger()
            {
                Receive<InitializeLogger>(_ => Sender.Tell(new LoggerInitialized()));
                Receive<LogEvent>(e => Log(e.LogSource, (LogLevel)e.LogLevel(), e.Message.ToString()));
            }
        }

        static Utility()
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            lock (_lock)
            {
                using FileStream fs = new FileStream("error.log", FileMode.Create, FileAccess.Write, FileShare.None);
                using StreamWriter w = new StreamWriter(fs);

                if (e.ExceptionObject is Exception ex)
                {
                    PrintErrorLogs(w, ex);
                }
                else
                {
                    Log(e.ExceptionObject.GetType().Name, LogLevel.Error, e.ExceptionObject?.ToString());
                }
            }
        }

        private static void PrintErrorLogs(StreamWriter writer, Exception ex)
        {
            writer.WriteLine($"{DateTime.UtcNow.ToString()} [Error:{ex.GetType()}] ");
            writer.WriteLine(ex.Message);
            writer.WriteLine(ex.StackTrace);
            if (ex is AggregateException ex2)
            {
                foreach (Exception inner in ex2.InnerExceptions)
                {
                    writer.WriteLine();
                    PrintErrorLogs(writer, inner);
                }
            }
            else if (ex.InnerException != null)
            {
                writer.WriteLine();
                PrintErrorLogs(writer, ex.InnerException);
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
            if (level >= LogLevel.Error)
            {
                lock (_lock)
                {
                    using FileStream fs = new FileStream("error.log", FileMode.Create, FileAccess.Write, FileShare.None);
                    using StreamWriter w = new StreamWriter(fs);

                    w.WriteLine($"{DateTime.UtcNow.ToString()} [{level}:{source}] {message}");
                }
            }

            foreach (ILogPlugin plugin in Plugin.Loggers)
                plugin.Log(source, level, message);
        }
    }
}
