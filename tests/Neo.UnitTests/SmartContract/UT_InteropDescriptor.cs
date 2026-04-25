// Copyright (C) 2015-2026 The Neo Project.
//
// UT_InteropDescriptor.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Persistence;
using Neo.SmartContract;
using System;
using System.Reflection;

namespace Neo.UnitTests.SmartContract
{
    [TestClass]
    public class UT_InteropDescriptor
    {
        [TestMethod]
        public void TestInvokeReturnsMethodResult()
        {
            using var snapshot = TestBlockchain.GetTestSnapshotCache();
            using var engine = new TestEngine(snapshot);
            var descriptor = CreateDescriptor(nameof(TestEngine.AddTwoNumbers));

            var result = descriptor.Invoke(engine, [2, 3]);

            Assert.AreEqual(5, result);
        }

        [TestMethod]
        public void TestInvokeReturnsNullForVoidMethod()
        {
            using var snapshot = TestBlockchain.GetTestSnapshotCache();
            using var engine = new TestEngine(snapshot);
            var descriptor = CreateDescriptor(nameof(TestEngine.MarkFlag));

            var result = descriptor.Invoke(engine, [true]);

            Assert.IsNull(result);
            Assert.IsTrue(engine.FlagWasMarked);
        }

        [TestMethod]
        public void TestInvokeWrapsTargetException()
        {
            using var snapshot = TestBlockchain.GetTestSnapshotCache();
            using var engine = new TestEngine(snapshot);
            var descriptor = CreateDescriptor(nameof(TestEngine.ThrowInvalidOperation));

            var exception = Assert.ThrowsExactly<TargetInvocationException>(() => descriptor.Invoke(engine, []));

            Assert.IsInstanceOfType<InvalidOperationException>(exception.InnerException);
        }

        private static InteropDescriptor CreateDescriptor(string methodName)
        {
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var method = typeof(TestEngine).GetMethod(methodName, flags) ?? throw new AssertFailedException();

            return new InteropDescriptor
            {
                Name = $"Test.{methodName}",
                Handler = method,
                FixedPrice = 0,
                RequiredCallFlags = CallFlags.None
            };
        }

        private sealed class TestEngine(DataCache snapshot) : ApplicationEngine(TriggerType.Application, null, snapshot, null, TestProtocolSettings.Default, 0)
        {
            public bool FlagWasMarked { get; private set; }

            internal int AddTwoNumbers(int left, int right) => left + right;

            internal void MarkFlag(bool value)
            {
                FlagWasMarked = value;
            }

            internal void ThrowInvalidOperation()
            {
                throw new InvalidOperationException();
            }
        }
    }
}
