// Copyright (C) 2015-2025 The Neo Project.
//
// UT_StateRootPlugin.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.Extensions.Configuration;
using Neo.Network.P2P.Payloads;
using Neo.Plugins.StateRootPlugin.Storage;
using Neo.UnitTests;
using System.Reflection;

namespace Neo.Plugins.StateRootPlugin.Tests
{
    [TestClass]
    public class UT_StateRootPlugin
    {
        private const uint TestNetwork = 5195086u;

        private static readonly ProtocolSettings s_protocol = TestProtocolSettings.Default with { Network = TestNetwork };

        private StateRootPlugin? _stateRootPlugin;
        private TestBlockchain.TestNeoSystem? _system;

        [TestInitialize]
        public void Setup()
        {
            _system = new TestBlockchain.TestNeoSystem(s_protocol);
            _stateRootPlugin = new StateRootPlugin();

            // Use reflection to call the protected OnSystemLoaded method
            var bindingFlags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
            var onSystemLoaded = typeof(StateRootPlugin).GetMethod("OnSystemLoaded", bindingFlags);
            Assert.IsNotNull(onSystemLoaded, "OnSystemLoaded method not found via reflection.");

            onSystemLoaded.Invoke(_stateRootPlugin, [_system]);

            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["PluginConfiguration:FullState"] = "true",
                    ["PluginConfiguration:Network"] = TestNetwork.ToString(),
                })
                .Build()
                .GetSection("PluginConfiguration");
            StateRootSettings.Load(config);
        }

        [TestCleanup]
        public void Cleanup()
        {
            _stateRootPlugin?.Dispose();
        }

        [TestMethod]
        public void TestStateRootPlugin_Initialization()
        {
            Assert.IsNotNull(_stateRootPlugin);
            Assert.AreEqual("StateRootPlugin", _stateRootPlugin.Name);
            Assert.IsTrue(_stateRootPlugin!.Description.Contains("state root"));
        }

        [TestMethod]
        public void TestStateRootPlugin_StateStoreAccess()
        {
            // Test that StateStore is accessible
            Assert.IsNotNull(StateStore.Singleton);
        }

        [TestMethod]
        public void TestStateRootPlugin_BasicFunctionality()
        {
            // Test basic plugin functionality
            Assert.IsNotNull(_stateRootPlugin!.Name);
            Assert.IsNotNull(_stateRootPlugin.Description);
            Assert.IsNotNull(_stateRootPlugin.ConfigFile);
        }
    }
}
