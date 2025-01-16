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
using System.Reflection;

namespace Neo.UnitTests.Plugins
{
    [TestClass]
    public class UT_Plugin
    {
        private static readonly object s_locker = new();

        [TestInitialize]
        public void TestInitialize()
        {
            ClearEventHandlers();
        }

        [TestCleanup]
        public void TestCleanup()
        {
            ClearEventHandlers();
        }

        private static void ClearEventHandlers()
        {
            ClearEventHandler("Committing");
            ClearEventHandler("Committed");
        }

        private static void ClearEventHandler(string eventName)
        {
            var eventInfo = typeof(Blockchain).GetEvent(eventName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (eventInfo == null)
            {
                return;
            }

            var fields = typeof(Blockchain).GetFields(BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
            foreach (var field in fields)
            {
                if (field.FieldType == typeof(MulticastDelegate) || field.FieldType.BaseType == typeof(MulticastDelegate))
                {
                    var eventDelegate = (MulticastDelegate)field.GetValue(null);
                    if (eventDelegate != null && field.Name.Contains(eventName))
                    {
                        foreach (var handler in eventDelegate.GetInvocationList())
                        {
                            eventInfo.RemoveEventHandler(null, handler);
                        }
                        break;
                    }
                }
            }
        }

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
            lock (s_locker)
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
        public void TestOnException()
        {
            _ = new TestPlugin();
            // Ensure no exception is thrown
            try
            {
                Blockchain.InvokeCommitting(null, null, null, null);
                Blockchain.InvokeCommitted(null, null);
            }
            catch (Exception ex)
            {
                Assert.Fail($"InvokeCommitting or InvokeCommitted threw an exception: {ex.Message}");
            }

            // Register TestNonPlugin that throws exceptions
            _ = new TestNonPlugin();

            // Ensure exception is thrown
            Assert.ThrowsException<NotImplementedException>(() =>
           {
               Blockchain.InvokeCommitting(null, null, null, null);
           });

            Assert.ThrowsException<NotImplementedException>(() =>
           {
               Blockchain.InvokeCommitted(null, null);
           });
        }

        [TestMethod]
        public void TestOnPluginStopped()
        {
            var pp = new TestPlugin();
            Assert.AreEqual(false, pp.IsStopped);
            // Ensure no exception is thrown
            try
            {
                Blockchain.InvokeCommitting(null, null, null, null);
                Blockchain.InvokeCommitted(null, null);
            }
            catch (Exception ex)
            {
                Assert.Fail($"InvokeCommitting or InvokeCommitted threw an exception: {ex.Message}");
            }

            Assert.AreEqual(true, pp.IsStopped);
        }

        [TestMethod]
        public void TestOnPluginStopOnException()
        {
            // pp will stop on exception.
            var pp = new TestPlugin();
            Assert.AreEqual(false, pp.IsStopped);
            // Ensure no exception is thrown
            try
            {
                Blockchain.InvokeCommitting(null, null, null, null);
                Blockchain.InvokeCommitted(null, null);
            }
            catch (Exception ex)
            {
                Assert.Fail($"InvokeCommitting or InvokeCommitted threw an exception: {ex.Message}");
            }

            Assert.AreEqual(true, pp.IsStopped);

            // pp2 will not stop on exception.
            var pp2 = new TestPlugin(UnhandledExceptionPolicy.Ignore);
            Assert.AreEqual(false, pp2.IsStopped);
            // Ensure no exception is thrown
            try
            {
                Blockchain.InvokeCommitting(null, null, null, null);
                Blockchain.InvokeCommitted(null, null);
            }
            catch (Exception ex)
            {
                Assert.Fail($"InvokeCommitting or InvokeCommitted threw an exception: {ex.Message}");
            }

            Assert.AreEqual(false, pp2.IsStopped);
        }

        [TestMethod]
        public void TestOnNodeStopOnPluginException()
        {
            // node will stop on pp exception.
            var pp = new TestPlugin(UnhandledExceptionPolicy.StopNode);
            Assert.AreEqual(false, pp.IsStopped);
            Assert.ThrowsException<NotImplementedException>(() =>
            {
                Blockchain.InvokeCommitting(null, null, null, null);
            });

            Assert.ThrowsException<NotImplementedException>(() =>
            {
                Blockchain.InvokeCommitted(null, null);
            });

            Assert.AreEqual(false, pp.IsStopped);
        }
    }
}
