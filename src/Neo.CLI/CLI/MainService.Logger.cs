// Copyright (C) 2015-2025 The Neo Project.
//
// MainService.Logger.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.Extensions.Configuration; // Needed for config loading
using Neo.ConsoleService;
using Neo.Extensions;
//using Neo.Json; // Not directly used in this part
//using Neo.SmartContract.Native; // Not directly used in this part
using Serilog;
using Serilog.Core;
using Serilog.Events;
using System;
using System.IO;
using System.Linq;

namespace Neo.CLI
{
    public partial class MainService // : ConsoleServiceBase, IWalletProvider (Base class and interfaces defined in the primary part)
    {
        // Runtime Log State Tracking
        private LogEventLevel _currentLogLevel;
        private bool _isConsoleSinkPresent; // Tracks if the console sink *should* be part of the config
        private bool _isFileSinkPresent;   // Tracks if the file sink *should* be part of the config
        private bool _isConsoleLogVisible; // Tracks if the console sink is currently showing output (vs. hidden)

        // Method to configure Serilog based ONLY on settings
        private void ConfigureLoggerFromSettings()
        {
            // Defaults
            LogEventLevel minimumLevel = LogEventLevel.Information;
            bool configConsoleOutput = true; // Default: console enabled if not specified
            string configLogPath = "Logs"; // Default log path
            bool configFileActive = false; // Default: file disabled

            // --- 1. Load Settings --- 
            try
            {
                if (Settings.Default == null) {
                     Console.Error.WriteLine("Warning: Settings.Default is null during logger configuration. Using hardcoded defaults.");
                     try { Settings.Initialize(new ConfigurationBuilder().Build()); } catch { }
                }

                // Get logger settings from Settings.Default, using defaults if null
                configLogPath = Settings.Default?.Logger?.Path ?? configLogPath;
                configConsoleOutput = Settings.Default?.Logger?.ConsoleOutput ?? configConsoleOutput;
                configFileActive = Settings.Default?.Logger?.Active ?? configFileActive;

                Log.Debug("Using settings: LogPath={path}, ConsoleOutput={console}, Active={active}", configLogPath, configConsoleOutput, configFileActive);

                // Display initial status based on settings
                if (!configFileActive) Console.WriteLine("File logging is disabled in settings.");
                if (!configConsoleOutput) Console.WriteLine("Console logging disabled in settings.");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error accessing settings: {ex.Message}. Using hardcoded logger defaults.");
                // Reset to hardcoded defaults if settings access fails
                minimumLevel = LogEventLevel.Information;
                configConsoleOutput = true;
                configLogPath = "Logs";
                configFileActive = false;
            }

            // --- 2. Configure Serilog --- 
            var logConfig = new LoggerConfiguration()
                .MinimumLevel.Is(minimumLevel) // Always use the hardcoded default level
                .Enrich.FromLogContext()
                .Enrich.WithThreadId();

            // --- 3. Configure Console Sink --- 
            bool actualConsoleSinkPresent = false;
            if (configConsoleOutput) // Use the setting directly
            {
                logConfig = logConfig.WriteTo.Console(
                    outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}",
                    restrictedToMinimumLevel: minimumLevel);
                actualConsoleSinkPresent = true;
                Console.WriteLine("Console logging configured.");
            }

            // --- 4. Configure File Sink (if Active is true) ---
            bool actualFileSinkPresent = false;
            string finalLogPath = configLogPath; // Start with configured path

            if (configFileActive) // Only attempt file logging if Active: true
            {
                try
                {
                    // Attempt to ensure the configured directory exists
                    if (!Directory.Exists(configLogPath))
                    {
                        Directory.CreateDirectory(configLogPath);
                    }
                    // If we reach here, the configured path is usable (or was just created)
                    finalLogPath = configLogPath;
                }
                catch (Exception ex)
                {
                    // Configured path failed, fall back to current directory
                    Console.Error.WriteLine($"Error creating configured log directory '{configLogPath}': {ex.Message}. Falling back to current directory.");
                    Log.Warning(ex, "Failed to create configured log directory {ConfigPath}, falling back to current directory.", configLogPath);
                    finalLogPath = "."; // Fallback path
                    // We don't need to create ".", it always exists.
                }

                // Now, add the file sink using the determined finalLogPath
                try
                {
                    logConfig = logConfig.WriteTo.File(
                        Path.Combine(finalLogPath, "neo-node-.log"),
                        rollingInterval: RollingInterval.Day,
                        fileSizeLimitBytes: 52428800,
                        retainedFileCountLimit: 7,
                        buffered: false, // Keep unbuffered
                        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] ({SourceContext}) {Message:lj}{NewLine}{Exception}",
                        restrictedToMinimumLevel: minimumLevel);
                    actualFileSinkPresent = true; // Sink was successfully added
                    Console.WriteLine($"File logging configured. Path: {Path.GetFullPath(finalLogPath)}");
                }
                catch (Exception sinkEx)
                {
                     // Catch errors during sink configuration itself (e.g., locking)
                     Console.Error.WriteLine($"Failed to configure file sink at '{finalLogPath}': {sinkEx.Message}. File logging disabled for this session.");
                     Log.Error(sinkEx, "Failed to configure file sink at {Path}.", finalLogPath);
                     actualFileSinkPresent = false; // Ensure state reflects failure
                }
            }

            // --- 5. Assign Logger and Set Internal State --- 
            var oldLogger = Serilog.Log.Logger;
            Serilog.Log.Logger = logConfig.CreateLogger();
            (oldLogger as IDisposable)?.Dispose();

            // Set internal tracking state based on the final configuration outcome
            _currentLogLevel = minimumLevel;
            _isConsoleSinkPresent = actualConsoleSinkPresent;
            _isFileSinkPresent = actualFileSinkPresent; // Reflects if sink was actually added
            _isConsoleLogVisible = actualConsoleSinkPresent;

            Log.Information("Logger configured: Level={level}, Console={console}, File={file}, Path={path}",
                _currentLogLevel, _isConsoleSinkPresent, _isFileSinkPresent, actualFileSinkPresent ? finalLogPath : "(disabled)"); // Log final path only if active
        }

        // ReconfigureLogger needs update to respect initial _isFileSinkPresent
        private void ReconfigureLogger(LogEventLevel? newLevel = null, bool? enableConsole = null, bool? showConsole = null)
        {
            var targetLevel = newLevel ?? _currentLogLevel;
            var targetConsolePresent = enableConsole ?? _isConsoleSinkPresent;
            var targetFilePresent = _isFileSinkPresent; // Keep initial file state
            var targetConsoleVisible = targetConsolePresent && (showConsole ?? _isConsoleLogVisible);

            if (enableConsole == false) targetConsoleVisible = false;
            if (!targetConsolePresent) targetConsoleVisible = false;

            Log.Information("Reconfiguring logger: Target Level={level}, ConsolePresent={consoleP}, FilePresent={fileP} (Initial), ConsoleVisible={consoleV}",
                targetLevel, targetConsolePresent, targetFilePresent, targetConsoleVisible);

            var logConfig = new LoggerConfiguration()
                .MinimumLevel.Is(targetLevel)
                .Enrich.FromLogContext()
                .Enrich.WithThreadId();

            if (targetConsolePresent)
            {
                var consoleSinkLevel = targetConsoleVisible ? targetLevel : (LogEventLevel.Fatal + 1);
                logConfig = logConfig.WriteTo.Console(
                    outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}",
                    restrictedToMinimumLevel: consoleSinkLevel);
            }

            // Configure File Sink (Only if it was initially present)
            if (targetFilePresent)
            {
                string logPath = Settings.Default.Logger.Path; // Use path from settings
                string finalLogPath = logPath;
                try
                {
                    // Ensure directory exists - might have been deleted or become inaccessible since startup?
                    if (!Directory.Exists(logPath))
                    {
                         Log.Warning("Log directory {path} missing during reconfigure. Attempting to create.", logPath);
                        Directory.CreateDirectory(logPath);
                        Log.Information("Recreated log directory: {path}", Path.GetFullPath(logPath));
                    }
                }
                catch (Exception dirEx)
                {
                     Console.Error.WriteLine($"Error ensuring configured log directory '{logPath}' during reconfigure: {dirEx.Message}. Falling back to current directory for this reconfiguration.");
                     Log.Warning(dirEx, "Failed to ensure configured log directory {ConfigPath} during reconfigure, falling back to current directory.", logPath);
                     finalLogPath = "."; // Fallback for this specific reconfiguration
                }
                
                // Try adding file sink with potentially adjusted path
                try 
                {
                     logConfig = logConfig.WriteTo.File(
                        Path.Combine(finalLogPath, "neo-node-.log"),
                        rollingInterval: RollingInterval.Day,
                        fileSizeLimitBytes: 52428800,
                        retainedFileCountLimit: 7,
                        buffered: false, // Keep unbuffered
                        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] ({SourceContext}) {Message:lj}{NewLine}{Exception}",
                        restrictedToMinimumLevel: targetLevel); // Use the potentially updated targetLevel
                }
                catch (Exception sinkEx)
                {
                     Console.Error.WriteLine($"Failed to re-add file sink at '{finalLogPath}' during reconfigure: {sinkEx.Message}.");
                     Log.Error(sinkEx, "Failed to re-add file sink at {Path} during reconfigure.", finalLogPath);
                     // Don't change _isFileSinkPresent state, just log the failure to re-add
                }
            }

            var oldLogger = Serilog.Log.Logger;
            Serilog.Log.Logger = logConfig.CreateLogger();
            (oldLogger as IDisposable)?.Dispose();

            _currentLogLevel = targetLevel;
            _isConsoleSinkPresent = targetConsolePresent;
            // _isFileSinkPresent remains unchanged from initial state
            _isConsoleLogVisible = targetConsoleVisible;

            Log.Information("Logger reconfigured. Current State: Level={level}, ConsolePresent={consoleP}, FilePresent={fileP} (Initial), ConsoleVisible={consoleV}",
                 _currentLogLevel, _isConsoleSinkPresent, _isFileSinkPresent, _isConsoleLogVisible);
        }

        // --- Log Commands ---

        /// <summary>
        /// Sets the minimum logging level at runtime.
        /// </summary>
        /// <param name="level">The desired minimum log level (Verbose, Debug, Information, Warning, Error, Fatal).</param>
        /// <example>
        /// log level set Debug
        /// log level set Warning
        /// </example>
        [ConsoleCommand("log level set", Category = "Log Commands", Description = "Sets the minimum logging level (Verbose, Debug, Information, Warning, Error, Fatal)")]
        private void OnSetLogLevel(LogEventLevel level)
        {
            ReconfigureLogger(newLevel: level);
            ConsoleHelper.Info($"Minimum log level set to: {level}");
        }

        /// <summary>
        /// Ensures the console log sink is active (added to the configuration) and visible (outputting messages).
        /// </summary>
        /// <example>
        /// log console enable
        /// </example>
        [ConsoleCommand("log console enable", Category = "Log Commands", Description = "Ensures the console log sink is active and visible.")]
        private void OnEnableConsoleLogCmd()
        {
            ReconfigureLogger(enableConsole: true, showConsole: true);
            ConsoleHelper.Info("Console logging enabled and visible.");
        }

        /// <summary>
        /// Disables console logging by removing the console sink from the configuration.
        /// </summary>
        /// <example>
        /// log console disable
        /// </example>
        [ConsoleCommand("log console disable", Category = "Log Commands", Description = "Disables the console log sink entirely.")]
        private void OnDisableConsoleLogCmd()
        {
            ReconfigureLogger(enableConsole: false);
            ConsoleHelper.Info("Console logging disabled (sink removed).");
        }

        /// <summary>
        /// Makes console log output visible by adjusting the sink's level restriction.
        /// This only works if the console sink is already enabled.
        /// </summary>
        /// <example>
        /// log console show
        /// </example>
        [ConsoleCommand("log console show", Category = "Log Commands", Description = "Makes console output visible (if console logging is enabled).")]
        private void OnShowConsoleLog()
        {
            if (!_isConsoleSinkPresent)
            {
                ConsoleHelper.Warning("Console logging is not enabled. Use 'log console enable' first.");
                return;
            }
            ReconfigureLogger(showConsole: true);
            ConsoleHelper.Info("Console output is now visible.");
        }

        /// <summary>
        /// Hides console log output by setting the sink's level restriction very high.
        /// The sink remains active but won't output messages.
        /// This only works if the console sink is already enabled.
        /// </summary>
        /// <example>
        /// log console hide
        /// </example>
        [ConsoleCommand("log console hide", Category = "Log Commands", Description = "Hides console output without disabling the sink (if console logging is enabled).")]
        private void OnHideConsoleLog()
        {
            if (!_isConsoleSinkPresent)
            {
                ConsoleHelper.Warning("Console logging is not enabled. Cannot hide.");
                return;
            }
            ReconfigureLogger(showConsole: false);
            ConsoleHelper.Info("Console output is now hidden.");
        }

        /// <summary>
        /// Shows the initial configuration from config.json and the current runtime logging status.
        /// </summary>
        /// <example>
        /// log status
        /// </example>
        [ConsoleCommand("log status", Category = "Log Commands", Description = "Shows the current logging configuration and runtime status.")]
        private void OnLogStatus()
        {
            // Configuration from Settings
            // var configLogLevelStr = Settings.Default.Logger.MinimumLevel; // Removed
            var configConsoleEnabled = Settings.Default.Logger.ConsoleOutput;
            var configFileActive = Settings.Default.Logger.Active;
            var configLogPath = Settings.Default.Logger.Path;

            // Runtime status from internal state
            var runtimeLogLevel = _currentLogLevel;
            var runtimeConsolePresent = _isConsoleSinkPresent;
            var runtimeFilePresent = _isFileSinkPresent;
            var runtimeConsoleVisible = _isConsoleLogVisible;

            ConsoleHelper.Info("--- Logging Status ---");
            ConsoleHelper.Info("[Configuration (config.json)]");
            // ConsoleHelper.Info($"  - Minimum Level: {configLogLevelStr}"); // Removed
            ConsoleHelper.Info($"  - Console Output Enabled: {configConsoleEnabled}");
            ConsoleHelper.Info($"  - File Logging Active: {configFileActive}");
            ConsoleHelper.Info($"  - Log Path: {configLogPath}");
            ConsoleHelper.Info("[Runtime Status]");
            ConsoleHelper.Info($"  - Current Minimum Level: {runtimeLogLevel}");
            ConsoleHelper.Info($"  - Console Sink Present: {runtimeConsolePresent}");
            ConsoleHelper.Info($"  - Console Output Visible: {runtimeConsoleVisible} {(runtimeConsolePresent ? "" : "(Sink not present)")}");
            ConsoleHelper.Info($"  - File Sink Present: {runtimeFilePresent}");
            if (runtimeFilePresent)
            {
                try { ConsoleHelper.Info($"  - Current Log Path: {Path.GetFullPath(configLogPath)}"); }
                catch { ConsoleHelper.Info($"  - Current Log Path: {configLogPath} (Invalid?)"); }
            }
            ConsoleHelper.Info("--- End Status ---");
        }

        // --- End Log Commands ---
    }
}
