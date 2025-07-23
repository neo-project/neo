using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Plugins.OpenTelemetry;
using System;
using System.IO;

namespace Neo.Plugins.OTelPlugin.Tests
{
    [TestClass]
    public class UT_OTelPlugin
    {
        private OpenTelemetryPlugin? _plugin;

        [TestInitialize]
        public void Setup()
        {
            _plugin = new OpenTelemetryPlugin();
        }

        [TestCleanup]
        public void Cleanup()
        {
            _plugin?.Dispose();
        }

        [TestMethod]
        public void TestPluginName()
        {
            Assert.AreEqual("OpenTelemetry", _plugin!.Name);
        }

        [TestMethod]
        public void TestPluginDescription()
        {
            Assert.AreEqual("Provides observability for Neo blockchain node using OpenTelemetry", _plugin!.Description);
        }

        [TestMethod]
        public void TestPluginCanBeCreated()
        {
            Assert.IsNotNull(_plugin);
            Assert.IsInstanceOfType(_plugin, typeof(Plugin));
        }

        [TestMethod]
        public void TestPluginDispose()
        {
            var plugin = new OpenTelemetryPlugin();
            
            // Should not throw
            plugin.Dispose();
            
            // Multiple dispose should also not throw
            plugin.Dispose();
        }
    }
}