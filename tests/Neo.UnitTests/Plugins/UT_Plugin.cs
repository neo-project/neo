// Copyright (C) 2015-2024 The Neo Project.
//
// UT_Plugin.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Ledger;
using Neo.Plugins;
using System;
using System.Threading.Tasks;

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

        [TestMethod]
        public async Task TestOnException()
        {
            // Ensure no exception is thrown
            try
            {
                await Blockchain.InvokeCommittingAsync(null, null, null, null);
                await Blockchain.InvokeCommittedAsync(null, null);
            }
            catch (Exception ex)
            {
                Assert.Fail($"InvokeCommitting or InvokeCommitted threw an exception: {ex.Message}");
            }

            // Register TestNonPlugin that throws exceptions
            _ = new TestNonPlugin();

            // Ensure exception is thrown
            await Assert.ThrowsExceptionAsync<NotImplementedException>(async () =>
            {
                await Blockchain.InvokeCommittingAsync(null, null, null, null);
            });

            await Assert.ThrowsExceptionAsync<NotImplementedException>(async () =>
            {
                await Blockchain.InvokeCommittedAsync(null, null);
            });
        }
    }
}
