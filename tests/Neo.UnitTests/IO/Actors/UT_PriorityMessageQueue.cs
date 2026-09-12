// Copyright (C) 2015-2026 The Neo Project.
//
// UT_PriorityMessageQueue.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Akka.Actor;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO.Actors;
using System.Collections;

namespace Neo.UnitTests.IO.Actors
{
    [TestClass]
    public class UT_PriorityMessageQueue
    {
        private static Envelope Env(object message) => new(message, ActorRefs.NoSender);

        [TestMethod]
        public void Enqueue_Dequeue_RespectsPriority()
        {
            var queue = new PriorityMessageQueue(
                dropper: static (_, _) => false,
                priorityGenerator: static msg => msg is string s && s == "high");

            queue.Enqueue(ActorRefs.NoSender, Env("low"));
            queue.Enqueue(ActorRefs.NoSender, Env("high"));
            Assert.IsTrue(queue.HasMessages);
            Assert.AreEqual(2, queue.Count);

            Assert.IsTrue(queue.TryDequeue(out var first));
            Assert.AreEqual("high", first.Message);
            Assert.IsTrue(queue.TryDequeue(out var second));
            Assert.AreEqual("low", second.Message);
        }

        [TestMethod]
        public void Dropper_PreventsEnqueue()
        {
            var queue = new PriorityMessageQueue(
                dropper: static (_, _) => true,
                priorityGenerator: static _ => false);

            queue.Enqueue(ActorRefs.NoSender, Env("x"));
            Assert.IsFalse(queue.HasMessages);
            Assert.AreEqual(0, queue.Count);
        }

        [TestMethod]
        public void Idle_Message_IsNotQueued_ButCanBeSyntheticDequeued()
        {
            var queue = new PriorityMessageQueue(
                dropper: static (_, _) => false,
                priorityGenerator: static _ => false);

            queue.Enqueue(ActorRefs.NoSender, Env(Idle.Instance));
            Assert.IsFalse(queue.HasMessages);
            // After an enqueue (even Idle), TryDequeue may synthesize Idle once.
            Assert.IsTrue(queue.TryDequeue(out var idle));
            Assert.AreSame(Idle.Instance, idle.Message);
            Assert.IsFalse(queue.TryDequeue(out _));
        }

        [TestMethod]
        public void CleanUp_IsNoOp()
        {
            var queue = new PriorityMessageQueue(static (_, _) => false, static _ => false);
            queue.CleanUp(ActorRefs.NoSender, null!);
        }
    }
}
