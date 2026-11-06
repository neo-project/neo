// Copyright (C) 2015-2026 The Neo Project.
//
// UT_DynamicPriceTable.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.SmartContract;
using Neo.VM;
using System;
using System.Threading.Tasks;

namespace Neo.UnitTests.SmartContract
{
    [TestClass]
    public class UT_DynamicPriceTable
    {
        [TestMethod]
        public void CloneIsolatesWritesInBothDirections()
        {
            var original = new DynamicPriceTable();
            DynamicPriceTable.PriceFunc first = _ => 10;
            DynamicPriceTable.PriceFunc second = _ => 20;
            original[OpCode.PUSH1] = first;
            var clone = original.Clone();
            var sibling = original.Clone();
            original[OpCode.PUSH1] = second;
            Assert.AreSame(first, clone[OpCode.PUSH1]);
            Assert.AreSame(first, sibling[OpCode.PUSH1]);
            clone[OpCode.PUSH1] = null;
            Assert.IsNull(clone[OpCode.PUSH1]);
            Assert.AreSame(second, original[OpCode.PUSH1]);
            Assert.AreSame(first, sibling[OpCode.PUSH1]);
            var descendant = clone.Clone();
            clone[OpCode.PUSH1] = first;
            Assert.IsNull(descendant[OpCode.PUSH1]);
            descendant[OpCode.PUSH2] = second;
            Assert.IsNull(clone[OpCode.PUSH2]);
        }

        [TestMethod]
        public void ClonePreservesEverySlot()
        {
            var original = new DynamicPriceTable();
            for (int i = 0; i <= byte.MaxValue; i++)
            {
                int price = i;
                original[(OpCode)i] = _ => price;
            }
            var clone = original.Clone();
            for (int i = 0; i <= byte.MaxValue; i++)
            {
                Assert.AreEqual((long)i, clone[(OpCode)i](new RunStats()));
                clone[(OpCode)i] = null;
                Assert.IsNotNull(original[(OpCode)i]);
            }
        }

        [TestMethod]
        public void ConcurrentUpdatesPreserveOtherSlots()
        {
            var table = new DynamicPriceTable();
            var snapshot = table.Clone();
            Parallel.For(0, 256, i =>
            {
                table[(OpCode)i] = _ => i;
            });
            for (int i = 0; i < 256; i++)
            {
                Assert.AreEqual((long)i, table[(OpCode)i](new RunStats()));
                Assert.IsNull(snapshot[(OpCode)i]);
            }
        }

        [TestMethod]
        public void ConcurrentClonesRetainTheirCapturedPrice()
        {
            var table = new DynamicPriceTable();
            table[OpCode.PUSH1] = _ => 0;
            Parallel.For(0, 1000, i =>
            {
                var clone = table.Clone();
                var captured = clone[OpCode.PUSH1];
                table[OpCode.PUSH1] = _ => i;
                Assert.AreSame(captured, clone[OpCode.PUSH1]);
                clone[OpCode.PUSH2] = _ => i;
                Assert.IsNull(table[OpCode.PUSH2]);
            });
        }

        [TestMethod]
        public void CloneDoesNotAllocateAnOpcodeArray()
        {
            var original = new DynamicPriceTable();
            for (int i = 0; i < 100; i++) GC.KeepAlive(original.Clone());
            const int iterations = 1000;
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < iterations; i++) GC.KeepAlive(original.Clone());
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.IsTrue(allocated < iterations * 256L,
                $"Clone allocated {allocated / iterations} bytes per operation; expected a wrapper, not a 256-slot array.");
        }
    }
}
