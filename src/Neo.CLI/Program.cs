// Copyright (C) 2015-2025 The Neo Project.
//
// Program.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.CLI;
using Serilog;
using Serilog.Events; // For LogEventLevel
using System; // For Enum, Console, Exception, StringComparison
using System.IO;
using System.Linq; // For parsing args

namespace Neo
{
    static class Program
    {
        static void Main(string[] args)
        {
            // Default Log Level
            var minimumLevel = LogEventLevel.Error;

            // Parse command line arguments for log level
            try
            {
                var logLevelArg = args.Select((value, index) => new { value, index })
                                    .FirstOrDefault(pair => pair.value.Equals("--loglevel", StringComparison.OrdinalIgnoreCase) || pair.value.Equals("-l", StringComparison.OrdinalIgnoreCase));

                if (logLevelArg != null && args.Length > logLevelArg.index + 1)
                {
                    var levelString = args[logLevelArg.index + 1];
                    if (Enum.TryParse<LogEventLevel>(levelString, ignoreCase: true, out var parsedLevel))
                    {
                        minimumLevel = parsedLevel;
                        Console.WriteLine($"Log level set to: {minimumLevel}"); // Early feedback
                    }
                    else
                    {
                        Console.Error.WriteLine($"Warning: Invalid log level '{levelString}'. Using default: {minimumLevel}");
                    }
                }
            }
            catch (Exception ex) // Catch potential errors during arg parsing
            {
                Console.Error.WriteLine($"Error parsing log level argument: {ex.Message}. Using default: {minimumLevel}");
            }

            // Programmatic Serilog Configuration
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Is(minimumLevel) // Set the base minimum level
                .Enrich.FromLogContext()
                // Use the extension method from Serilog.Enrichers.Thread package
                .Enrich.WithThreadId()
                // Console Sink (Restricted to Information+ regardless of minimumLevel, unless minimumLevel is higher)
                .WriteTo.Console(
                    restrictedToMinimumLevel: LogEventLevel.Information,
                    outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
                // File Sink (Captures from minimumLevel down)
                .WriteTo.File(
                    Path.Combine("Logs", "neo-node-.log"),
                    // removed restrictedToMinimumLevel here - let it inherit the global minimum
                    rollingInterval: RollingInterval.Day, // Ensure Serilog package is referenced for this enum
                    retainedFileCountLimit: 7,
                    buffered: true,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] ({SourceContext}) {Message:lj}{NewLine}{Exception}")
                .CreateLogger();

            // Wrap execution in try/catch/finally
            try
            {
                Log.Information("Starting Neo CLI with Minimum Log Level: {MinLogLevel}", minimumLevel);
                var mainService = new MainService();
                // Pass only the remaining args to MainService if needed, or let it handle them again
                mainService.Run(args);
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Application start-up failed");
            }
            finally
            {
                Log.Information("Stopping Neo CLI");
                Log.CloseAndFlush(); // Ensure logs are flushed before exit
            }
        }
    }
}
