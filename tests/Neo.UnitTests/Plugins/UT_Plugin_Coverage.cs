// Copyright (C) 2015-2026 The Neo Project.
//
// UT_Plugin_Coverage.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Plugins;
using System;
using System.IO;

namespace Neo.UnitTests.Plugins
{
    [TestClass]
    public class UT_Plugin_Coverage
    {
        private static readonly object s_locker = new();

        private sealed class MessageThrowingPlugin : Plugin
        {
            private readonly UnhandledExceptionPolicy _policy;
            protected internal override UnhandledExceptionPolicy ExceptionPolicy => _policy;

            public MessageThrowingPlugin(UnhandledExceptionPolicy policy) => _policy = policy;

            protected override bool OnMessage(object message) =>
                throw new InvalidOperationException("plugin-message-fault");
        }

        private sealed class MessageHandlingPlugin : Plugin
        {
            public object LastMessage { get; private set; }

            protected override bool OnMessage(object message)
            {
                LastMessage = message;
                return true;
            }
        }

        private sealed class DescribePlugin : Plugin
        {
            public override string Description => "coverage-desc";
            public override string Name => "DescribePlugin";
        }

        [TestMethod]
        public void Description_Path_RootPath_And_Dispose()
        {
            var plugin = new DescribePlugin();
            Assert.AreEqual("coverage-desc", plugin.Description);
            Assert.AreEqual("DescribePlugin", plugin.Name);
            Assert.IsFalse(string.IsNullOrEmpty(plugin.RootPath));
            Assert.IsFalse(string.IsNullOrEmpty(plugin.Path));
            Assert.IsFalse(string.IsNullOrEmpty(plugin.ConfigFile));
            Assert.IsNotNull(plugin.Version);
            plugin.Dispose();
        }

        [TestMethod]
        public void PluginsDirectory_IsAbsoluteOrRelativePath()
        {
            Assert.IsFalse(string.IsNullOrEmpty(Plugin.PluginsDirectory));
            // Directory may or may not exist in unit-test host.
            _ = Directory.Exists(Plugin.PluginsDirectory);
        }

        [TestMethod]
        public void SendMessage_StopPlugin_StopsOnMessageException()
        {
            lock (s_locker)
            {
                Plugin.Plugins.Clear();
                var plugin = new MessageThrowingPlugin(UnhandledExceptionPolicy.StopPlugin);
                Assert.IsFalse(plugin.IsStopped);
                Assert.IsFalse(Plugin.SendMessage("x"));
                Assert.IsTrue(plugin.IsStopped);
            }
        }

        [TestMethod]
        public void SendMessage_Ignore_ContinuesAfterMessageException()
        {
            lock (s_locker)
            {
                Plugin.Plugins.Clear();
                var plugin = new MessageThrowingPlugin(UnhandledExceptionPolicy.Ignore);
                Assert.IsFalse(Plugin.SendMessage("x"));
                Assert.IsFalse(plugin.IsStopped);
            }
        }

        [TestMethod]
        public void SendMessage_StopNode_Rethrows()
        {
            lock (s_locker)
            {
                Plugin.Plugins.Clear();
                _ = new MessageThrowingPlugin(UnhandledExceptionPolicy.StopNode);
                Assert.ThrowsExactly<InvalidOperationException>(() => Plugin.SendMessage("x"));
            }
        }

        [TestMethod]
        public void SendMessage_Handled_StopsPropagation()
        {
            lock (s_locker)
            {
                Plugin.Plugins.Clear();
                var first = new MessageHandlingPlugin();
                var second = new MessageThrowingPlugin(UnhandledExceptionPolicy.StopNode);
                Assert.IsTrue(Plugin.SendMessage("payload"));
                Assert.AreEqual("payload", first.LastMessage);
                // second must not run because first handled the message
                Assert.IsFalse(second.IsStopped);
            }
        }

        [TestMethod]
        public void SendMessage_SkipsStoppedPlugins()
        {
            lock (s_locker)
            {
                Plugin.Plugins.Clear();
                var stopped = new MessageHandlingPlugin { IsStopped = true };
                Assert.IsFalse(Plugin.SendMessage("ignored"));
                Assert.IsNull(stopped.LastMessage);
            }
        }

        [TestMethod]
        public void OnSystemLoaded_Default_DoesNotThrow()
        {
            var plugin = new DescribePlugin();
            plugin.OnSystemLoaded(TestBlockchain.GetSystem());
        }
    }
}
