// Copyright (C) 2015-2026 The Neo Project.
//
// UT_PriorityMailbox.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Akka.Actor;
using Akka.Configuration;
using Akka.Dispatch;
using Akka.TestKit.MsTest;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO.Actors;
using System.Collections;
using System.Linq;

namespace Neo.UnitTests.IO.Actors
{
    [TestClass]
    public class UT_PriorityMailbox : TestKit
    {
        private sealed class TestMailbox(Settings settings, Config config) : PriorityMailbox(settings, config)
        {
            // expose defaults via base virtuals
            public bool CallIsHighPriority(object message) => IsHighPriority(message);
            public bool CallShallDrop(object message, IEnumerable queue) => ShallDrop(message, queue);
        }

        [TestCleanup]
        public void Cleanup() => Shutdown();

        [TestMethod]
        public void Defaults_IsHighPriority_And_ShallDrop_AreFalse()
        {
            var akkaSettings = new Settings(Sys, DefaultConfig);
            var mailbox = new TestMailbox(akkaSettings, DefaultConfig);

            Assert.IsFalse(mailbox.CallIsHighPriority("anything"));
            Assert.IsFalse(mailbox.CallShallDrop("anything", Enumerable.Empty<object>()));
        }

        [TestMethod]
        public void Create_ReturnsPriorityMessageQueue()
        {
            var akkaSettings = new Settings(Sys, DefaultConfig);
            var mailbox = new TestMailbox(akkaSettings, DefaultConfig);
            var queue = mailbox.Create(ActorRefs.NoSender, Sys);
            Assert.IsInstanceOfType<PriorityMessageQueue>(queue);
        }
    }
}
