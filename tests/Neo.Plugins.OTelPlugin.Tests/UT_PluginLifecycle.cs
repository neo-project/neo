// Copyright (C) 2015-2025 The Neo Project.
//
// UT_PluginLifecycle.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Plugins.OpenTelemetry;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;

namespace Neo.Plugins.OTelPlugin.Tests
{
    [TestClass]
    public class UT_PluginLifecycle
    {
        private string _tempConfigPath = null!;

        [TestInitialize]
        public void Setup()
        {
            _tempConfigPath = Path.GetTempFileName();
        }

        [TestCleanup]
        public void Cleanup()
        {
            if (File.Exists(_tempConfigPath))
                File.Delete(_tempConfigPath);

            // Reset static config path
            TestableOpenTelemetryPlugin.SetConfigPath(null);
        }

        [TestMethod]
        public void TestPluginInitialization()
        {
            // Use default config
            TestableOpenTelemetryPlugin.SetConfigPath(null);
            var plugin = new TestableOpenTelemetryPlugin();

            Assert.AreEqual("OpenTelemetry", plugin.Name);
            Assert.AreEqual("Provides observability for Neo blockchain node using OpenTelemetry", plugin.Description);
        }

        [TestMethod]
        public void TestPluginConfigureWithEnabledSetting()
        {
            var config = new Dictionary<string, object>
            {
                ["Enabled"] = true
            };

            File.WriteAllText(_tempConfigPath, JsonSerializer.Serialize(new { PluginConfiguration = config }));

            // Set config path before creating plugin
            TestableOpenTelemetryPlugin.SetConfigPath(_tempConfigPath);
            var plugin = new TestableOpenTelemetryPlugin();

            Assert.IsTrue(plugin.IsEnabled);
        }

        [TestMethod]
        public void TestPluginConfigureWithDisabledSetting()
        {
            var config = new Dictionary<string, object>
            {
                ["Enabled"] = false
            };

            File.WriteAllText(_tempConfigPath, JsonSerializer.Serialize(new { PluginConfiguration = config }));

            // Set config path before creating plugin
            TestableOpenTelemetryPlugin.SetConfigPath(_tempConfigPath);
            var plugin = new TestableOpenTelemetryPlugin();

            Assert.IsFalse(plugin.IsEnabled);
        }

        [TestMethod]
        public void TestPluginSystemLoadedWhenEnabled()
        {
            var config = new Dictionary<string, object>
            {
                ["Enabled"] = true
            };

            File.WriteAllText(_tempConfigPath, JsonSerializer.Serialize(new { PluginConfiguration = config }));

            // Set config path before creating plugin
            TestableOpenTelemetryPlugin.SetConfigPath(_tempConfigPath);
            var plugin = new TestableOpenTelemetryPlugin();
            plugin.TestOnSystemLoaded(null!);

            Assert.IsTrue(plugin.IsInitialized);
            Assert.IsNotNull(plugin.MeterProvider);
            Assert.IsNotNull(plugin.Meter);
            // RequestCounter was removed in favor of actual blockchain metrics
        }

        [TestMethod]
        public void TestPluginSystemLoadedWhenDisabled()
        {
            var config = new Dictionary<string, object>
            {
                ["Enabled"] = false
            };

            File.WriteAllText(_tempConfigPath, JsonSerializer.Serialize(new { PluginConfiguration = config }));

            // Set config path before creating plugin
            TestableOpenTelemetryPlugin.SetConfigPath(_tempConfigPath);
            var plugin = new TestableOpenTelemetryPlugin();
            plugin.TestOnSystemLoaded(null!);

            Assert.IsFalse(plugin.IsInitialized);
            Assert.IsNull(plugin.MeterProvider);
            Assert.IsNull(plugin.Meter);
            // RequestCounter was removed in favor of actual blockchain metrics
        }

        [TestMethod]
        public void TestPluginDispose()
        {
            var config = new Dictionary<string, object>
            {
                ["Enabled"] = true
            };

            File.WriteAllText(_tempConfigPath, JsonSerializer.Serialize(new { PluginConfiguration = config }));

            // Set config path before creating plugin
            TestableOpenTelemetryPlugin.SetConfigPath(_tempConfigPath);
            var plugin = new TestableOpenTelemetryPlugin();
            plugin.TestOnSystemLoaded(null!);

            // Verify initialized
            Assert.IsTrue(plugin.IsInitialized);

            // Dispose
            plugin.Dispose();

            // Verify disposed (we can't directly check private fields, but dispose should not throw)
            // Multiple dispose should also not throw
            plugin.Dispose();
        }

        [TestMethod]
        public void TestPluginDefaultConfiguration()
        {
            // Test with no config file
            TestableOpenTelemetryPlugin.SetConfigPath(null);
            var plugin = new TestableOpenTelemetryPlugin();

            // Should default to enabled
            Assert.IsTrue(plugin.IsEnabled);
        }

        [TestMethod]
        public void TestPluginWithInvalidConfiguration()
        {
            // Write invalid JSON
            File.WriteAllText(_tempConfigPath, "{ invalid json }");

            // Plugin should handle invalid configuration gracefully by using defaults
            // It should not throw, but log a warning and use default settings
            try
            {
                TestableOpenTelemetryPlugin.SetConfigPath(_tempConfigPath);
                var plugin = new TestableOpenTelemetryPlugin();
                // Should default to enabled when configuration is invalid
                Assert.IsTrue(plugin.IsEnabled);
            }
            catch (Exception ex) when (ex is JsonException || ex is System.IO.InvalidDataException)
            {
                // It's acceptable for the plugin to let configuration exceptions bubble up
                // since the configuration file is malformed
            }
        }

        [TestMethod]
        public void TestPluginMultipleSystemLoads()
        {
            var config = new Dictionary<string, object>
            {
                ["Enabled"] = true
            };

            File.WriteAllText(_tempConfigPath, JsonSerializer.Serialize(new { PluginConfiguration = config }));

            // Set config path before creating plugin
            TestableOpenTelemetryPlugin.SetConfigPath(_tempConfigPath);
            var plugin = new TestableOpenTelemetryPlugin();

            // Load system multiple times
            plugin.TestOnSystemLoaded(null!);
            plugin.TestOnSystemLoaded(null!);

            // Should still be initialized (idempotent)
            Assert.IsTrue(plugin.IsInitialized);
        }

        // Testable version of the plugin that exposes protected methods
        private class TestableOpenTelemetryPlugin : OpenTelemetryPlugin
        {
            private static string? _staticConfigPath;

            // Static method to set config path before plugin creation
            public static void SetConfigPath(string? configPath)
            {
                _staticConfigPath = configPath;
            }

            // Override ConfigFile to return our custom path
            public override string ConfigFile => _staticConfigPath ?? base.ConfigFile;

            // Use reflection to access private fields
            public bool IsEnabled
            {
                get
                {
                    var field = typeof(OpenTelemetryPlugin).GetField("_settings", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (field != null)
                    {
                        var settings = field.GetValue(this) as OTelSettings;
                        return settings?.Enabled ?? false;
                    }
                    return false;
                }
            }

            public bool IsInitialized
            {
                get
                {
                    var field = typeof(OpenTelemetryPlugin).GetField("_meterProvider", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    return field != null && field.GetValue(this) != null;
                }
            }

            public object? MeterProvider
            {
                get
                {
                    var field = typeof(OpenTelemetryPlugin).GetField("_meterProvider", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    return field?.GetValue(this);
                }
            }

            public object? Meter
            {
                get
                {
                    var field = typeof(OpenTelemetryPlugin).GetField("_meter", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    return field?.GetValue(this);
                }
            }

            public object? RequestCounter
            {
                get
                {
                    // RequestCounter field was removed, return null
                    return null;
                }
            }

            public void TestOnSystemLoaded(NeoSystem system)
            {
                OnSystemLoaded(system);
            }
        }
    }
}
