// Copyright (C) 2015-2025 The Neo Project.
//
// MainServiceLoggerTests.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.Extensions.Configuration; // Required for ConfigurationBuilder
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Serilog;
using Serilog.Events;
using System.Reflection;

namespace Neo.CLI.Tests
{
    [TestClass]
    public class MainServiceLoggerTests
    {
        private static ILogger? _originalLogger;
        private static Settings? _originalCustomSettings;
        private StringWriter? _consoleOutput;
        private TextWriter? _originalConsoleOut;
        private const string TestLogDir = "TestLogs_MainService"; // Unique test log dir

        // Helper to invoke private methods
        private static T? InvokePrivateMethod<T>(object instance, string methodName, params object[] parameters)
        {
            var methodInfo = instance.GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
            if (methodInfo == null)
            {
                throw new ArgumentException($"Method '{methodName}' not found on type '{instance.GetType().FullName}'.");
            }
            return (T?)methodInfo.Invoke(instance, parameters);
        }

        // Helper to get private field value
        private static T? GetPrivateField<T>(object instance, string fieldName)
        {
            var fieldInfo = instance.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static); // Include Static for shared fields if any
            if (fieldInfo == null)
            {
                // Search in base types if necessary
                var baseType = instance.GetType().BaseType;
                while (baseType != null && fieldInfo == null)
                {
                    fieldInfo = baseType.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                    baseType = baseType.BaseType;
                }
            }
            if (fieldInfo == null)
            {
                throw new ArgumentException($"Field '{fieldName}' not found on type '{instance.GetType().FullName}' or its base types.");
            }
            return (T?)fieldInfo.GetValue(instance);
        }

        // Helper to set private field value
        private static void SetPrivateField(object instance, string fieldName, object value)
        {
            var fieldInfo = instance.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            if (fieldInfo == null)
            {
                var baseType = instance.GetType().BaseType;
                while (baseType != null && fieldInfo == null)
                {
                    fieldInfo = baseType.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                    baseType = baseType.BaseType;
                }
            }
            if (fieldInfo == null)
            {
                throw new ArgumentException($"Field '{fieldName}' not found on type '{instance.GetType().FullName}' or its base types.");
            }
            fieldInfo.SetValue(instance, value);
        }

        [TestInitialize]
        public void TestInitialize()
        {
            Console.WriteLine("--- TestInitialize Start ---");
            // Store original static state
            _originalLogger = Serilog.Log.Logger;
            _originalCustomSettings = Settings.Custom;
            _originalConsoleOut = Console.Out;

            // Redirect console output
            _consoleOutput = new StringWriter();
            Console.SetOut(_consoleOutput);

            // Clean up previous test log directory if it exists
            if (Directory.Exists(TestLogDir))
            {
                Console.WriteLine($"Deleting existing TestLogDir: {TestLogDir}");
                try { Directory.Delete(TestLogDir, true); } catch (Exception ex) { Console.WriteLine($"Error deleting TestLogDir: {ex.Message}"); }
            }
            Directory.CreateDirectory(TestLogDir); // Ensure base dir exists

            // Set base Settings.Custom for tests (can be overridden)
            var config = new ConfigurationBuilder()
                .AddJsonFile("config.json", optional: false)
                .Build();
            try { Settings.Initialize(config); } catch { /* Ignore */ }
            var appConfigSection = config.GetSection("ApplicationConfiguration");
            Settings.Custom = new Settings()
            {
                Logger = new LoggerSettings(appConfigSection.GetSection("Logger")) { Path = TestLogDir },
                Storage = new StorageSettings(appConfigSection.GetSection("Storage")) { Engine = "MemoryStore" },
                P2P = new P2PSettings(appConfigSection.GetSection("P2P")),
                UnlockWallet = new UnlockWalletSettings(appConfigSection.GetSection("UnlockWallet")),
                Contracts = new ContractsSettings(appConfigSection.GetSection("Contracts")),
                Plugins = new PluginsSettings(appConfigSection.GetSection("Plugins"))
            };
            Console.WriteLine($"TestInitialize: Base Settings.Custom configured. Log path: {Settings.Default.Logger.Path}");

            // DO NOT configure a default logger here. Each test will set its own.
            Serilog.Log.Logger = Serilog.Core.Logger.None; // Start with a null logger
            Console.WriteLine("TestInitialize: Serilog.Log.Logger set to None.");

            Console.WriteLine("--- TestInitialize End ---");
        }

        [TestCleanup]
        public void TestCleanup()
        {
            Console.WriteLine("--- TestCleanup Start ---");
            // Flush logger before disposing
            (Serilog.Log.Logger as IDisposable)?.Dispose(); // Dispose current test logger
            Console.WriteLine("TestCleanup: Disposed current Serilog.Log.Logger.");

            // Restore original static state
            Serilog.Log.Logger = _originalLogger!; // Restore original logger
            Settings.Custom = _originalCustomSettings; // Restore original custom settings
            Console.SetOut(_originalConsoleOut!); // Restore console output
            Console.WriteLine("TestCleanup: Restored original logger, settings, and console output.");

            _consoleOutput?.Dispose();

            // Clean up test log directory
            if (Directory.Exists(TestLogDir))
            {
                Console.WriteLine($"Deleting TestLogDir: {TestLogDir}");
                try { Directory.Delete(TestLogDir, true); }
                catch (Exception ex) { Console.WriteLine($"TestCleanup: Error deleting TestLogDir: {ex.Message}"); }
            }
            Console.WriteLine("--- TestCleanup End ---");
        }

        // --- Test Cases ---

        [TestMethod]
        public void InitializeLoggingState_IsNoLongerUsedDirectlyInTests() // Renamed original test
        {
            Console.WriteLine("*** Starting Test: SetPrivateFieldsManually ***");
            // Arrange
            // No specific logger needed for this test, TestInitialize sets Logger.None
            var mainService = new MainService();

            // Act: Manually set the state
            SetPrivateField(mainService, "_currentLogLevel", LogEventLevel.Debug);
            SetPrivateField(mainService, "_isConsoleSinkPresent", false);
            SetPrivateField(mainService, "_isFileSinkPresent", true);
            SetPrivateField(mainService, "_isConsoleLogVisible", false); // Consistent with console not present

            // Assert
            Assert.AreEqual(LogEventLevel.Debug, GetPrivateField<LogEventLevel>(mainService, "_currentLogLevel"));
            Assert.IsFalse(GetPrivateField<bool>(mainService, "_isConsoleSinkPresent"));
            Assert.IsTrue(GetPrivateField<bool>(mainService, "_isFileSinkPresent"));
            Assert.IsFalse(GetPrivateField<bool>(mainService, "_isConsoleLogVisible"));
            Console.WriteLine("*** Finished Test: SetPrivateFieldsManually ***");
        }

        [TestMethod]
        public void ReconfigureLogger_SetLevel_UpdatesLevelAndState()
        {
            // Arrange
            Log.Logger = new LoggerConfiguration().MinimumLevel.Information().WriteTo.Console().WriteTo.File(Path.Combine(TestLogDir, "test.log")).CreateLogger();
            var mainService = new MainService();
            // Set initial state manually to match logger
            SetPrivateField(mainService, "_currentLogLevel", LogEventLevel.Information);
            SetPrivateField(mainService, "_isConsoleSinkPresent", true);
            SetPrivateField(mainService, "_isFileSinkPresent", true);
            SetPrivateField(mainService, "_isConsoleLogVisible", true);

            // Act
            InvokePrivateMethod<object>(mainService, "ReconfigureLogger", (object[])[LogEventLevel.Debug, true, true, true]);

            // Assert
            Assert.AreEqual(LogEventLevel.Debug, GetPrivateField<LogEventLevel>(mainService, "_currentLogLevel"));
            Assert.IsTrue(GetPrivateField<bool>(mainService, "_isConsoleSinkPresent"));
            Assert.IsTrue(GetPrivateField<bool>(mainService, "_isFileSinkPresent"));
            Assert.IsTrue(GetPrivateField<bool>(mainService, "_isConsoleLogVisible"));
        }

        [TestMethod]
        public void ReconfigureLogger_DisableConsole_UpdatesStateAndRemovesSink()
        {
            // Arrange
            Log.Logger = new LoggerConfiguration().MinimumLevel.Information().WriteTo.Console().WriteTo.File(Path.Combine(TestLogDir, "test.log")).CreateLogger();
            var mainService = new MainService();
            SetPrivateField(mainService, "_currentLogLevel", LogEventLevel.Information);
            SetPrivateField(mainService, "_isConsoleSinkPresent", true);
            SetPrivateField(mainService, "_isFileSinkPresent", true);
            SetPrivateField(mainService, "_isConsoleLogVisible", true);

            // Act
            InvokePrivateMethod<object>(mainService, "ReconfigureLogger", (object[])[LogEventLevel.Information, false, true, null!]); // Disable console

            // Assert State
            Assert.AreEqual(LogEventLevel.Information, GetPrivateField<LogEventLevel>(mainService, "_currentLogLevel"));
            Assert.IsFalse(GetPrivateField<bool>(mainService, "_isConsoleSinkPresent"));
            Assert.IsTrue(GetPrivateField<bool>(mainService, "_isFileSinkPresent"));
            Assert.IsFalse(GetPrivateField<bool>(mainService, "_isConsoleLogVisible"));
        }

        [TestMethod]
        public void ReconfigureLogger_HideConsole_UpdatesStateAndRestrictsSinkLevel()
        {
            // Arrange
            Log.Logger = new LoggerConfiguration().MinimumLevel.Information().WriteTo.Console().WriteTo.File(Path.Combine(TestLogDir, "test.log")).CreateLogger();
            var mainService = new MainService();
            SetPrivateField(mainService, "_currentLogLevel", LogEventLevel.Information);
            SetPrivateField(mainService, "_isConsoleSinkPresent", true);
            SetPrivateField(mainService, "_isFileSinkPresent", true);
            SetPrivateField(mainService, "_isConsoleLogVisible", true);

            // Act
            InvokePrivateMethod<object>(mainService, "ReconfigureLogger", (object[])[LogEventLevel.Information, true, true, false]); // Hide console

            // Assert State
            Assert.AreEqual(LogEventLevel.Information, GetPrivateField<LogEventLevel>(mainService, "_currentLogLevel"));
            Assert.IsTrue(GetPrivateField<bool>(mainService, "_isConsoleSinkPresent"));
            Assert.IsTrue(GetPrivateField<bool>(mainService, "_isFileSinkPresent"));
            Assert.IsFalse(GetPrivateField<bool>(mainService, "_isConsoleLogVisible"));
        }

        [TestMethod]
        public void ReconfigureLogger_ShowConsole_WhenHidden_UpdatesState()
        {
            // Arrange
            Log.Logger = new LoggerConfiguration().MinimumLevel.Information().WriteTo.Console(restrictedToMinimumLevel: LogEventLevel.Fatal + 1).WriteTo.File(Path.Combine(TestLogDir, "test.log")).CreateLogger();
            var mainService = new MainService();
            SetPrivateField(mainService, "_currentLogLevel", LogEventLevel.Information);
            SetPrivateField(mainService, "_isConsoleSinkPresent", true);
            SetPrivateField(mainService, "_isFileSinkPresent", true);
            SetPrivateField(mainService, "_isConsoleLogVisible", false); // Start hidden
            _consoleOutput!.GetStringBuilder().Clear();

            // Act
            InvokePrivateMethod<object>(mainService, "ReconfigureLogger", (object[])[LogEventLevel.Information, true, true, true]); // Show console

            // Assert State
            Assert.IsTrue(GetPrivateField<bool>(mainService, "_isConsoleSinkPresent"));
            Assert.IsTrue(GetPrivateField<bool>(mainService, "_isConsoleLogVisible"));
        }

        [TestMethod]
        public void ReconfigureLogger_EnableFile_WhenDisabled_UpdatesStateAndAddsSink()
        {
            // Arrange
            Log.Logger = new LoggerConfiguration().MinimumLevel.Information().WriteTo.Console().CreateLogger(); // No File Sink
            var mainService = new MainService();
            SetPrivateField(mainService, "_currentLogLevel", LogEventLevel.Information);
            SetPrivateField(mainService, "_isConsoleSinkPresent", true);
            SetPrivateField(mainService, "_isFileSinkPresent", false);
            SetPrivateField(mainService, "_isConsoleLogVisible", true);

            // Act
            InvokePrivateMethod<object>(mainService, "ReconfigureLogger", (object[])[LogEventLevel.Information, true, true, true]); // Enable file

            // Assert State
            Assert.IsTrue(GetPrivateField<bool>(mainService, "_isConsoleSinkPresent"));
            Assert.IsTrue(GetPrivateField<bool>(mainService, "_isFileSinkPresent")); // File sink added
            Assert.IsTrue(GetPrivateField<bool>(mainService, "_isConsoleLogVisible"));
        }

        [TestMethod]
        public void ReconfigureLogger_EnableFile_InvalidPath_DisablesFileLogging()
        {
            // Arrange
            string invalidPath = Path.Combine("Inv:", "InvalidChars");
            var appConfigSection = new ConfigurationBuilder().AddJsonFile("config.json").Build().GetSection("ApplicationConfiguration");
            Settings.Custom = new Settings() { Logger = new LoggerSettings(appConfigSection.GetSection("Logger")) { Path = invalidPath } /* Other settings */ };
            Log.Logger = new LoggerConfiguration().MinimumLevel.Information().WriteTo.Console().CreateLogger(); // No File Sink
            var mainService = new MainService();
            SetPrivateField(mainService, "_currentLogLevel", LogEventLevel.Information);
            SetPrivateField(mainService, "_isConsoleSinkPresent", true);
            SetPrivateField(mainService, "_isFileSinkPresent", false);
            SetPrivateField(mainService, "_isConsoleLogVisible", true);

            // Act
            InvokePrivateMethod<object>(mainService, "ReconfigureLogger", (object[])[LogEventLevel.Information, true, true, true]); // Attempt to enable file

            // Assert State
            Assert.IsFalse(GetPrivateField<bool>(mainService, "_isFileSinkPresent"), "File sink should remain disabled due to error");
        }


        // --- Command Tests ---

        [TestMethod]
        public void OnSetLogLevel_CallsReconfigureAndPrintsMessage()
        {
            // Arrange
            Log.Logger = new LoggerConfiguration().MinimumLevel.Information().WriteTo.Console().CreateLogger();
            var mainService = new MainService();
            SetPrivateField(mainService, "_currentLogLevel", LogEventLevel.Information);
            SetPrivateField(mainService, "_isConsoleSinkPresent", true);
            SetPrivateField(mainService, "_isFileSinkPresent", false);
            SetPrivateField(mainService, "_isConsoleLogVisible", true);

            // Act
            InvokePrivateMethod<object>(mainService, "OnSetLogLevel", LogEventLevel.Warning);

            // Assert
            Assert.AreEqual(LogEventLevel.Warning, GetPrivateField<LogEventLevel>(mainService, "_currentLogLevel"), "Internal level state not updated.");
            StringAssert.Contains(_consoleOutput!.ToString(), "Minimum log level set to: Warning", "Console output message mismatch.");
        }

        [TestMethod]
        public void OnEnableConsoleLogCmd_CallsReconfigureAndPrintsMessage()
        {
            // Arrange
            Log.Logger = new LoggerConfiguration().MinimumLevel.Information().WriteTo.File(Path.Combine(TestLogDir, "test.log")).CreateLogger(); // No Console
            var mainService = new MainService();
            SetPrivateField(mainService, "_currentLogLevel", LogEventLevel.Information);
            SetPrivateField(mainService, "_isConsoleSinkPresent", false);
            SetPrivateField(mainService, "_isFileSinkPresent", true);
            SetPrivateField(mainService, "_isConsoleLogVisible", false);

            // Act
            InvokePrivateMethod<object>(mainService, "OnEnableConsoleLogCmd");

            // Assert
            Assert.IsTrue(GetPrivateField<bool>(mainService, "_isConsoleSinkPresent"), "Internal console state not enabled.");
            Assert.IsTrue(GetPrivateField<bool>(mainService, "_isConsoleLogVisible"), "Internal console state not visible.");
            StringAssert.Contains(_consoleOutput!.ToString(), "Console logging enabled and visible.", "Console output message mismatch.");
        }

        [TestMethod]
        public void OnHideConsoleLog_WhenEnabled_CallsReconfigureAndPrintsMessage()
        {
            // Arrange
            Log.Logger = new LoggerConfiguration().MinimumLevel.Information().WriteTo.Console().WriteTo.File(Path.Combine(TestLogDir, "test.log")).CreateLogger();
            var mainService = new MainService();
            SetPrivateField(mainService, "_currentLogLevel", LogEventLevel.Information);
            SetPrivateField(mainService, "_isConsoleSinkPresent", true);
            SetPrivateField(mainService, "_isFileSinkPresent", true);
            SetPrivateField(mainService, "_isConsoleLogVisible", true);

            // Act
            InvokePrivateMethod<object>(mainService, "OnHideConsoleLog");

            // Assert
            Assert.IsTrue(GetPrivateField<bool>(mainService, "_isConsoleSinkPresent"), "Console sink should still be present.");
            Assert.IsFalse(GetPrivateField<bool>(mainService, "_isConsoleLogVisible"), "Internal console state not hidden.");
            StringAssert.Contains(_consoleOutput!.ToString(), "Console output is now hidden.", "Console output message mismatch.");
        }

        [TestMethod]
        public void OnHideConsoleLog_WhenDisabled_PrintsWarning()
        {
            // Arrange
            Log.Logger = new LoggerConfiguration().MinimumLevel.Information().WriteTo.File(Path.Combine(TestLogDir, "test.log")).CreateLogger(); // No Console
            var mainService = new MainService();
            SetPrivateField(mainService, "_currentLogLevel", LogEventLevel.Information);
            SetPrivateField(mainService, "_isConsoleSinkPresent", false);
            SetPrivateField(mainService, "_isFileSinkPresent", true);
            SetPrivateField(mainService, "_isConsoleLogVisible", false);

            // Act
            InvokePrivateMethod<object>(mainService, "OnHideConsoleLog");

            // Assert
            Assert.IsFalse(GetPrivateField<bool>(mainService, "_isConsoleLogVisible"), "Console visibility should remain false.");
            StringAssert.Contains(_consoleOutput!.ToString(), "Console logging is not enabled. Cannot hide.", "Warning message mismatch.");
        }

        [TestMethod]
        public void OnLogStatus_OutputsCorrectInformation_NoConfigLevel()
        {
            // Arrange
            // Set specific config
            var appConfigSection = new ConfigurationBuilder().AddJsonFile("config.json").Build().GetSection("ApplicationConfiguration");
            var testSettingsConfig = new Settings()
            {
                Logger = new LoggerSettings(appConfigSection.GetSection("Logger"))
                {
                    ConsoleOutput = true,
                    Active = false,
                    Path = "ConfigPathForStatus"
                },
                Storage = Settings.Default.Storage,
                P2P = Settings.Default.P2P,
                UnlockWallet = Settings.Default.UnlockWallet,
                Contracts = Settings.Default.Contracts,
                Plugins = Settings.Default.Plugins
            };
            Settings.Custom = testSettingsConfig;

            // Set specific runtime logger state
            (Serilog.Log.Logger as IDisposable)?.Dispose();
            Log.Logger = new LoggerConfiguration().MinimumLevel.Verbose().WriteTo.Console(restrictedToMinimumLevel: LogEventLevel.Fatal + 1).WriteTo.File(Path.Combine(TestLogDir, "rt.log")).CreateLogger();
            Directory.CreateDirectory(TestLogDir);

            var mainService = new MainService();
            // Manually set internal state to match the logger above
            SetPrivateField(mainService, "_currentLogLevel", LogEventLevel.Verbose);
            SetPrivateField(mainService, "_isConsoleSinkPresent", true);
            SetPrivateField(mainService, "_isFileSinkPresent", true);
            SetPrivateField(mainService, "_isConsoleLogVisible", false);

            // Act
            InvokePrivateMethod<object>(mainService, "OnLogStatus");

            // Assert Console Output
            string output = _consoleOutput!.ToString();
            Console.SetOut(_originalConsoleOut!); Console.WriteLine("\n--- OnLogStatus Output ---"); Console.WriteLine(output); Console.WriteLine("--- End OnLogStatus Output ---\n"); Console.SetOut(_consoleOutput);

            StringAssert.Contains(output, "[Configuration (config.json)]");
            Assert.IsFalse(output.Contains("Minimum Level:", StringComparison.OrdinalIgnoreCase) && output.Contains("Debug"), "Config minimum level should not be displayed");
            StringAssert.Contains(output, "- Console Output Enabled: True");
            StringAssert.Contains(output, "- File Logging Active: False");
            StringAssert.Contains(output, "- Log Path: ConfigPathForStatus");
            StringAssert.Contains(output, "[Runtime Status]");
            StringAssert.Contains(output, "- Current Minimum Level: Verbose");
            StringAssert.Contains(output, "- Console Sink Present: True");
            StringAssert.Contains(output, "- Console Output Visible: False", "Visibility output should indicate False when hidden.");
            StringAssert.Contains(output, "- File Sink Present: True");
        }

        [TestMethod]
        public void OnDisableConsoleLogCmd_WhenEnabled_RemovesSinkAndUpdatesState()
        {
            // Arrange
            Log.Logger = new LoggerConfiguration().MinimumLevel.Information().WriteTo.Console().WriteTo.File(Path.Combine(TestLogDir, "test.log")).CreateLogger();
            var mainService = new MainService();
            SetPrivateField(mainService, "_currentLogLevel", LogEventLevel.Information);
            SetPrivateField(mainService, "_isConsoleSinkPresent", true);
            SetPrivateField(mainService, "_isFileSinkPresent", true);
            SetPrivateField(mainService, "_isConsoleLogVisible", true);

            // Act
            InvokePrivateMethod<object>(mainService, "OnDisableConsoleLogCmd");

            // Assert State
            Assert.IsFalse(GetPrivateField<bool>(mainService, "_isConsoleSinkPresent"), "Console sink should be marked as not present");
            Assert.IsFalse(GetPrivateField<bool>(mainService, "_isConsoleLogVisible"), "Console should be marked as not visible");
            StringAssert.Contains(_consoleOutput!.ToString(), "Console logging disabled (sink removed).", "Console output message mismatch.");
        }

        [TestMethod]
        public void OnShowConsoleLog_WhenHidden_MakesVisible()
        {
            // Arrange
            Log.Logger = new LoggerConfiguration().MinimumLevel.Information().WriteTo.Console(restrictedToMinimumLevel: LogEventLevel.Fatal + 1).WriteTo.File(Path.Combine(TestLogDir, "test.log")).CreateLogger();
            var mainService = new MainService();
            SetPrivateField(mainService, "_currentLogLevel", LogEventLevel.Information);
            SetPrivateField(mainService, "_isConsoleSinkPresent", true); // It is present
            SetPrivateField(mainService, "_isFileSinkPresent", true);
            SetPrivateField(mainService, "_isConsoleLogVisible", false); // But hidden
            _consoleOutput!.GetStringBuilder().Clear();

            // Act
            InvokePrivateMethod<object>(mainService, "OnShowConsoleLog");

            // Assert State
            Assert.IsTrue(GetPrivateField<bool>(mainService, "_isConsoleSinkPresent"), "Console sink should still be present");
            Assert.IsTrue(GetPrivateField<bool>(mainService, "_isConsoleLogVisible"), "Console should now be visible");
            StringAssert.Contains(_consoleOutput!.ToString(), "Console output is now visible.", "Console output message mismatch.");
        }

        [TestMethod]
        public void OnShowConsoleLog_WhenDisabled_PrintsWarning()
        {
            // Arrange
            Log.Logger = new LoggerConfiguration().MinimumLevel.Information().WriteTo.File(Path.Combine(TestLogDir, "test.log")).CreateLogger(); // No console
            var mainService = new MainService();
            SetPrivateField(mainService, "_currentLogLevel", LogEventLevel.Information);
            SetPrivateField(mainService, "_isConsoleSinkPresent", false);
            SetPrivateField(mainService, "_isFileSinkPresent", true);
            SetPrivateField(mainService, "_isConsoleLogVisible", false);
            _consoleOutput!.GetStringBuilder().Clear();

            // Act
            InvokePrivateMethod<object>(mainService, "OnShowConsoleLog");

            // Assert State (should not change)
            Assert.IsFalse(GetPrivateField<bool>(mainService, "_isConsoleSinkPresent"), "Console sink should remain disabled");
            Assert.IsFalse(GetPrivateField<bool>(mainService, "_isConsoleLogVisible"), "Console visibility should remain false");
            StringAssert.Contains(_consoleOutput!.ToString(), "Console logging is not enabled. Use 'log console enable' first.", "Warning message mismatch.");
        }

        [TestMethod]
        public void OnEnableFileLogCmd_WhenAlreadyEnabled_IsIdempotent()
        {
            // Arrange
            Log.Logger = new LoggerConfiguration().MinimumLevel.Information().WriteTo.Console().WriteTo.File(Path.Combine(TestLogDir, "test.log")).CreateLogger();
            var mainService = new MainService();
            SetPrivateField(mainService, "_currentLogLevel", LogEventLevel.Information);
            SetPrivateField(mainService, "_isConsoleSinkPresent", true);
            SetPrivateField(mainService, "_isFileSinkPresent", true);
            SetPrivateField(mainService, "_isConsoleLogVisible", true);
            _consoleOutput!.GetStringBuilder().Clear();

            // Act
            InvokePrivateMethod<object>(mainService, "OnEnableFileLogCmd");

            // Assert State
            Assert.IsTrue(GetPrivateField<bool>(mainService, "_isFileSinkPresent"), "File sink should remain present");
            StringAssert.Contains(_consoleOutput!.ToString(), "File logging enabled.", "Console output message mismatch.");
        }

        [TestMethod]
        public void OnDisableFileLogCmd_WhenEnabled_RemovesSinkAndUpdatesState()
        {
            // Arrange
            Log.Logger = new LoggerConfiguration().MinimumLevel.Information().WriteTo.Console().WriteTo.File(Path.Combine(TestLogDir, "test.log")).CreateLogger();
            var mainService = new MainService();
            SetPrivateField(mainService, "_currentLogLevel", LogEventLevel.Information);
            SetPrivateField(mainService, "_isConsoleSinkPresent", true);
            SetPrivateField(mainService, "_isFileSinkPresent", true);
            SetPrivateField(mainService, "_isConsoleLogVisible", true);

            // Act
            InvokePrivateMethod<object>(mainService, "OnDisableFileLogCmd");

            // Assert State
            Assert.IsFalse(GetPrivateField<bool>(mainService, "_isFileSinkPresent"), "File sink should be marked as not present");
            StringAssert.Contains(_consoleOutput!.ToString(), "File logging disabled (sink removed).", "Console output message mismatch.");
        }
    }
}
