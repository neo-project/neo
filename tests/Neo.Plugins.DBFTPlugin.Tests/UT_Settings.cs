// Copyright (C) 2015-2025 The Neo Project.
//
// UT_Settings.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Plugins.DBFTPlugin.Consensus;
using System.Collections.Generic;

namespace Neo.Plugins.DBFTPlugin.Tests
{
    [TestClass]
    public class UT_Settings
    {
        private IConfigurationSection CreateMockConfig(bool ignoreRecoveryLogs = true, uint network = 0x334F454Eu)
        {
            return new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string> {
                    { "PluginConfiguration:IgnoreRecoveryLogs", ignoreRecoveryLogs.ToString().ToLowerInvariant() },
                    { "PluginConfiguration:Network", $"0x{network:X8}" },
                    { "PluginConfiguration:AutoStart", "false" },
                    { "PluginConfiguration:MaxBlockSize", "2000000" },
                    { "PluginConfiguration:MaxBlockSystemFee", "150000000000" },
                    { "PluginConfiguration:ExceptionPolicy", "2" }
                })
                .Build()
                .GetSection("PluginConfiguration");
        }

        [TestMethod]
        public void TestSettingsInitialization()
        {
            // Test default values
            var config = CreateMockConfig();
            var settings = new Settings(config);

            Assert.IsTrue(settings.IgnoreRecoveryLogs);
            Assert.AreEqual(0x334F454Eu, settings.Network);
            Assert.IsFalse(settings.AutoStart);
            Assert.AreEqual(2000000u, settings.MaxBlockSize);
            Assert.AreEqual(150000000000, settings.MaxBlockSystemFee);
            Assert.AreEqual(UnhandledExceptionPolicy.Ignore, settings.ExceptionPolicy);
        }

        [TestMethod]
        public void TestCustomSettings()
        {
            // Test custom values
            var configCustom = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string> {
                    { "PluginConfiguration:IgnoreRecoveryLogs", "false" },
                    { "PluginConfiguration:Network", "0x12345678" },
                    { "PluginConfiguration:AutoStart", "true" },
                    { "PluginConfiguration:MaxBlockSize", "500000" },
                    { "PluginConfiguration:MaxBlockSystemFee", "50000000000" },
                    { "PluginConfiguration:ExceptionPolicy", "1" }
                })
                .Build()
                .GetSection("PluginConfiguration");

            var settings = new Settings(configCustom);

            Assert.IsFalse(settings.IgnoreRecoveryLogs);
            Assert.AreEqual(0x12345678u, settings.Network);
            Assert.IsTrue(settings.AutoStart);
            Assert.AreEqual(500000u, settings.MaxBlockSize);
            Assert.AreEqual(50000000000, settings.MaxBlockSystemFee);
            Assert.AreEqual(UnhandledExceptionPolicy.Ignore, settings.ExceptionPolicy);
        }
    }
}
