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

        private void InitializeLoggingState()
        {
            // Inspect the statically configured logger from Program.cs
            var initialLogger = Serilog.Log.Logger;
            if (initialLogger == null)
            {
                ConsoleHelper.Warning("Logger not initialized yet. Using default log states.");
                _currentLogLevel = LogEventLevel.Information;
                _isConsoleSinkPresent = false;
                _isFileSinkPresent = false;
                _isConsoleLogVisible = false;
                return;
            }

            var sinks = initialLogger.GetSinks();
            _isConsoleSinkPresent = sinks.Any(s => s.GetType().Name.Contains("Console", StringComparison.OrdinalIgnoreCase));
            _isFileSinkPresent = sinks.Any(s => s.GetType().Name.Contains("File", StringComparison.OrdinalIgnoreCase));
            // Initial visibility depends on whether the console sink is present and not effectively disabled by a high level (like Program.cs does)
            // We infer visibility based on presence for simplicity here, as Program.cs manages the initial 'hidden' state.
            _isConsoleLogVisible = _isConsoleSinkPresent;

            Log.Debug("Initial log state captured: Level={level}, ConsolePresent={consoleP}, FilePresent={fileP}, ConsoleVisible={consoleV}",
                _currentLogLevel, _isConsoleSinkPresent, _isFileSinkPresent, _isConsoleLogVisible);
        }

        // Central method to reconfigure Serilog based on desired state
        private void ReconfigureLogger(LogEventLevel? newLevel = null, bool? enableConsole = null, bool? enableFile = null, bool? showConsole = null)
        {
            // Determine target state
            var targetLevel = newLevel ?? _currentLogLevel;
            var targetConsolePresent = enableConsole ?? _isConsoleSinkPresent;
            var targetFilePresent = enableFile ?? _isFileSinkPresent;
            // Visibility change only matters if console *should* be present
            var targetConsoleVisible = targetConsolePresent && (showConsole ?? _isConsoleLogVisible);

            // Prevent hiding if console logging is being explicitly disabled
            if (enableConsole == false) targetConsoleVisible = false;
            // Prevent showing if console logging is not present
            if (!targetConsolePresent) targetConsoleVisible = false;

            Log.Information("Reconfiguring logger: Target Level={level}, ConsolePresent={consoleP}, FilePresent={fileP}, ConsoleVisible={consoleV}",
                targetLevel, targetConsolePresent, targetFilePresent, targetConsoleVisible);

            var logConfig = new LoggerConfiguration()
                .MinimumLevel.Is(targetLevel) // Set the global minimum level
                .Enrich.FromLogContext()
                .Enrich.WithThreadId();

            // Configure Console Sink
            if (targetConsolePresent)
            {
                // Determine the effective minimum level for the console sink
                var consoleSinkLevel = targetConsoleVisible ? targetLevel : (LogEventLevel.Fatal + 1); // Use high level to hide

                logConfig = logConfig.WriteTo.Console(
                    outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}",
                    restrictedToMinimumLevel: consoleSinkLevel); // Control visibility via level restriction
            }

            // Configure File Sink
            if (targetFilePresent)
            {
                var logPath = Settings.Default.Logger.Path;
                try
                {
                    if (!Directory.Exists(logPath))
                    {
                        Directory.CreateDirectory(logPath);
                        Log.Information("Created log directory: {path}", Path.GetFullPath(logPath));
                    }

                    logConfig = logConfig.WriteTo.File(
                       Path.Combine(logPath, "neo-node-.log"),
                       rollingInterval: RollingInterval.Day,
                       retainedFileCountLimit: 7,
                       buffered: true,
                       outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] ({SourceContext}) {Message:lj}{NewLine}{Exception}");
                }
                catch (Exception ex)
                {
                    ConsoleHelper.Error($"Failed to ensure log directory '{logPath}' or add file sink: {ex.Message}. File logging remains disabled for this session.");
                    targetFilePresent = false; // Update state if sink couldn't be added
                }
            }

            // Create and assign the new logger
            var oldLogger = Serilog.Log.Logger;
            Serilog.Log.Logger = logConfig.CreateLogger();
            (oldLogger as IDisposable)?.Dispose(); // Dispose the old logger

            // Update internal state tracking
            _currentLogLevel = targetLevel;
            _isConsoleSinkPresent = targetConsolePresent;
            _isFileSinkPresent = targetFilePresent;
            _isConsoleLogVisible = targetConsoleVisible; // Visibility depends on presence AND show state

            Log.Information("Logger reconfigured. Current State: Level={level}, ConsolePresent={consoleP}, FilePresent={fileP}, ConsoleVisible={consoleV}",
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
        /// Ensures the file log sink is active (added to the configuration).
        /// </summary>
        /// <example>
        /// log file enable
        /// </example>
        [ConsoleCommand("log file enable", Category = "Log Commands", Description = "Ensures the file log sink is active.")]
        private void OnEnableFileLogCmd()
        {
            ReconfigureLogger(enableFile: true);
            if (_isFileSinkPresent) // Check state *after* reconfiguration attempt
                ConsoleHelper.Info($"File logging enabled. Path: {Path.GetFullPath(Settings.Default.Logger.Path)}");
            else
                ConsoleHelper.Error("Failed to enable file logging (check previous errors).");
        }

        /// <summary>
        /// Disables file logging by removing the file sink from the configuration.
        /// </summary>
        /// <example>
        /// log file disable
        /// </example>
        [ConsoleCommand("log file disable", Category = "Log Commands", Description = "Disables the file log sink entirely.")]
        private void OnDisableFileLogCmd()
        {
            ReconfigureLogger(enableFile: false);
            ConsoleHelper.Info("File logging disabled (sink removed).");
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
