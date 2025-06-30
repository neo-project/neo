// Copyright (C) 2015-2025 The Neo Project.
//
// UT_MessageReceiver.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Neo.UnitTests.IO
{
    [TestClass]
    public class UT_MessageReceiver
    {
        private class TestMessageReceiver(int workerCount) :
            MessageReceiver<int>(new MessageRelayer(workerCount))
        {
            public readonly List<int> ProcessedMessages = [];
            public readonly ManualResetEventSlim Signal = new();

            public override void OnReceive(object message)
            {
                if (message is not int imsg)
                    throw new InvalidCastException("Expected message of type int.");

                lock (ProcessedMessages)
                {
                    ProcessedMessages.Add(imsg);
                    if (ProcessedMessages.Count >= 5)
                        Signal.Set();
                }

                // return Task.Delay(10);
            }
        }

        [TestMethod]
        public void TestTellAndDispose()
        {
            var receiver = new TestMessageReceiver(2);
            for (var i = 0; i < 5; i++)
                receiver.Tell(i);

            Assert.IsTrue(receiver.Signal.Wait(1000));
            Assert.AreEqual(5, receiver.ProcessedMessages.Count);
            receiver.Relayer.Dispose();
        }

        [TestMethod]
        public void TestTellAll()
        {
            var receiver = new TestMessageReceiver(2);
            receiver.Tell(1, 2, 3, 4, 5);

            Assert.IsTrue(receiver.Signal.Wait(1000));
            CollectionAssert.AreEquivalent(new List<int> { 1, 2, 3, 4, 5 }, receiver.ProcessedMessages);
            receiver.Relayer.Dispose();
        }
    }
}
