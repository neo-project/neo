using System;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Plugins;

namespace Neo.UnitTests.Plugins
{
    [TestClass]
    public class UT_Plugin
    {
        private static readonly object locker = new();

        [TestMethod]
        public void TestGetConfigFile()
        {
            var pp = new TestPlugin();
            var file = pp.ConfigFile;
            file.EndsWith("config.json").Should().BeTrue();
        }

        [TestMethod]
        public void TestGetName()
        {
            var pp = new TestPlugin();
            pp.Name.Should().Be("TestPlugin");
        }

        [TestMethod]
        public void TestGetVersion()
        {
            var pp = new TestPlugin();
            Action action = () => pp.Version.ToString();
            action.Should().NotThrow();
        }

        [TestMethod]
        public void TestSendMessage()
        {
            lock (locker)
            {
                Plugin.Plugins.Clear();
                Plugin.SendMessage("hey1").Should().BeFalse();

                var lp = new TestPlugin();
                Plugin.SendMessage("hey2").Should().BeTrue();
            }
        }

        [TestMethod]
        public void TestGetConfiguration()
        {
            var pp = new TestPlugin();
            pp.TestGetConfiguration().Key.Should().Be("PluginConfiguration");
        }
    }
}
