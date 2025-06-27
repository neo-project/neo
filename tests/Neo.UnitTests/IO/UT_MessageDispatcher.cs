// Copyright (C) 2015-2025 The Neo Project.
//
// UT_MessageDispatcher.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO;
using Org.BouncyCastle.Asn1.X509;
using System.Threading;

namespace Neo.UnitTests.IO
{
    [TestClass]
    public class UT_MessageDispatcher
    {
        private class TestMessage
        {
            public int Value { get; set; }
        }

        private class TestHandler : IMessageHandler<TestMessage>, IMessageHandler
        {
            public int _receivedCount = 0;

            private readonly ManualResetEventSlim _event;

            public TestHandler(ManualResetEventSlim evt)
            {
                _event = evt;
            }

            public void OnMessage(TestMessage message)
            {
                Interlocked.Increment(ref _receivedCount);
                _event.Set();
            }

            public void OnMessage(object message)
            {
                Interlocked.Increment(ref _receivedCount);
                _event.Set();
            }
        }

        [TestMethod]
        public void Dispatches_Message_To_Registered_Handler()
        {
            using var dispatcher = new MessageDispatcher(workerCount: 1);
            using var evt = new ManualResetEventSlim();

            var handler = new TestHandler(evt);
            dispatcher.RegisterHandler<TestMessage>(handler);

            dispatcher.Dispatch(new TestMessage { Value = 42 });

            Assert.IsTrue(evt.Wait(1000), "Handler did not receive the message");
            Assert.AreEqual(1, handler._receivedCount);
        }

        [TestMethod]
        public void Dispatches_Multiple_Messages()
        {
            using var dispatcher = new MessageDispatcher(workerCount: 2);
            using var evt = new CountdownEvent(10);

            var handler = new TestHandlerWrapper(evt);
            dispatcher.RegisterHandler<TestMessage>(handler);

            for (var i = 0; i < 10; i++)
                dispatcher.Dispatch(new TestMessage { Value = i });

            Assert.IsTrue(evt.Wait(1000), "Not all messages were processed");
            Assert.AreEqual(10, handler._count);
        }

        private class TestHandlerWrapper(CountdownEvent evt) : IMessageHandler<TestMessage>, IMessageHandler
        {
            public int _count;
            private readonly CountdownEvent _evt = evt;

            public void OnMessage(TestMessage message)
            {
                Interlocked.Increment(ref _count);
                _evt.Signal();
            }

            public void OnMessage(object message)
            {
                Interlocked.Increment(ref _count);
                _evt.Signal();
            }
        }
    }
}
