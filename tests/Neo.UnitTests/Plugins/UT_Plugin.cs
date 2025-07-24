// Copyright (C) 2015-2025 The Neo Project.
//
// UT_Plugin.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

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
            try
            {
                var file = pp.ConfigFile;
                Assert.IsTrue(file.EndsWith("config.json"));
            }
            finally
            {
                pp.Dispose();
            }
        }

        [TestMethod]
        public void TestGetName()
        {
            var pp = new TestPlugin();
            try
            {
                Assert.AreEqual("TestPlugin", pp.Name);
            }
            finally
            {
                pp.Dispose();
            }
        }

        [TestMethod]
        public void TestGetVersion()
        {
            var pp = new TestPlugin();
            try
            {
                _ = pp.Version.ToString();
            }
            catch (Exception ex)
            {
                Assert.Fail($"Should not throw but threw {ex}");
            }
            finally
            {
                pp.Dispose();
            }
        }

        [TestMethod]
        public void TestSendMessage()
        {
            lock (s_locker)
            {
                Plugin.Plugins.Clear();
                Assert.IsFalse(Plugin.SendMessage("hey1"));

                var lp = new TestPlugin();
                try
                {
                    Assert.IsTrue(Plugin.SendMessage("hey2"));
                }
                finally
                {
                    lp.Dispose();
                    Plugin.Plugins.Clear();
                }
            }
        }

        [TestMethod]
        public void TestGetConfiguration()
        {
            var pp = new TestPlugin();
            try
            {
                Assert.AreEqual("PluginConfiguration", pp.TestGetConfiguration().Key);
            }
            finally
            {
                pp.Dispose();
            }
        }

        [TestMethod]
        public void TestOnException()
        {
            var tp = new TestPlugin();
            TestNonPlugin tnp = null;
            try
            {
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
                tnp = new TestNonPlugin();

                // Ensure exception is thrown
                Assert.ThrowsExactly<NotImplementedException>(() =>
               {
                   Blockchain.InvokeCommitting(null, null, null, null);
               });

                Assert.ThrowsExactly<NotImplementedException>(() =>
               {
                   Blockchain.InvokeCommitted(null, null);
               });
            }
            finally
            {
                tp?.Dispose();
                tnp?.Dispose();
            }
        }

        [TestMethod]
        public void TestOnPluginStopped()
        {
            var pp = new TestPlugin();
            try
            {
                Assert.IsFalse(pp.IsStopped);
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

                Assert.IsTrue(pp.IsStopped);
            }
            finally
            {
                pp.Dispose();
            }
        }

        [TestMethod]
        public void TestOnPluginStopOnException()
        {
            // pp will stop on exception.
            var pp = new TestPlugin();
            var pp2 = (TestPlugin)null;
            try
            {
                Assert.IsFalse(pp.IsStopped);
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

                Assert.IsTrue(pp.IsStopped);

                // pp2 will not stop on exception.
                pp2 = new TestPlugin(UnhandledExceptionPolicy.Ignore);
                Assert.IsFalse(pp2.IsStopped);
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

                Assert.IsFalse(pp2.IsStopped);
            }
            finally
            {
                pp?.Dispose();
                pp2?.Dispose();
            }
        }

        [TestMethod]
        public void TestOnNodeStopOnPluginException()
        {
            // node will stop on pp exception.
            var pp = new TestPlugin(UnhandledExceptionPolicy.StopNode);
            try
            {
                Assert.IsFalse(pp.IsStopped);
                Assert.ThrowsExactly<NotImplementedException>(() =>
                {
                    Blockchain.InvokeCommitting(null, null, null, null);
                });

                Assert.ThrowsExactly<NotImplementedException>(() =>
                {
                    Blockchain.InvokeCommitted(null, null);
                });

                Assert.IsFalse(pp.IsStopped);
            }
            finally
            {
                pp.Dispose();
            }
        }
    }
}
