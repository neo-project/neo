// Copyright (C) 2015-2025 The Neo Project.
//
// UT_StackItem.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.VM;
using Neo.VM.Types;
using System.Numerics;

namespace Neo.Test
{
    [TestClass]
    public class UT_StackItem
    {
        [TestMethod]
        public void TestCircularReference()
        {
            var itemA = new Struct { true, false };
            var itemB = new Struct { true, false };
            var itemC = new Struct { false, false };

            itemA[1] = itemA;
            itemB[1] = itemB;
            itemC[1] = itemC;

            Assert.AreEqual(itemA.GetHashCode(), itemB.GetHashCode());
            Assert.AreNotEqual(itemA.GetHashCode(), itemC.GetHashCode());
        }

        [TestMethod]
        public void TestHashCode()
        {
            StackItem itemA = "NEO";
            StackItem itemB = "NEO";
            StackItem itemC = "SmartEconomy";

            Assert.AreEqual(itemB.GetHashCode(), itemA.GetHashCode());
            Assert.AreNotEqual(itemC.GetHashCode(), itemA.GetHashCode());

            itemA = new Buffer(1);
            itemB = new Buffer(1);
            itemC = new Buffer(2);

            Assert.AreEqual(itemB.GetHashCode(), itemA.GetHashCode());
            Assert.AreNotEqual(itemC.GetHashCode(), itemA.GetHashCode());

            itemA = new byte[] { 1, 2, 3 };
            itemB = new byte[] { 1, 2, 3 };
            itemC = new byte[] { 5, 6 };

            Assert.AreEqual(itemB.GetHashCode(), itemA.GetHashCode());
            Assert.AreNotEqual(itemC.GetHashCode(), itemA.GetHashCode());

            itemA = true;
            itemB = true;
            itemC = false;

            Assert.AreEqual(itemB.GetHashCode(), itemA.GetHashCode());
            Assert.AreNotEqual(itemC.GetHashCode(), itemA.GetHashCode());

            itemA = 1;
            itemB = 1;
            itemC = 123;

            Assert.AreEqual(itemB.GetHashCode(), itemA.GetHashCode());
            Assert.AreNotEqual(itemC.GetHashCode(), itemA.GetHashCode());

            itemA = new Null();
            itemB = new Null();

            Assert.AreEqual(itemB.GetHashCode(), itemA.GetHashCode());

            itemA = new Array { true, false, 0 };
            itemB = new Array { true, false, 0 };
            itemC = new Array { true, false, 1 };

            Assert.AreEqual(itemB.GetHashCode(), itemA.GetHashCode());
            Assert.AreNotEqual(itemC.GetHashCode(), itemA.GetHashCode());

            itemA = new Struct { true, false, 0 };
            itemB = new Struct { true, false, 0 };
            itemC = new Struct { true, false, 1 };

            Assert.AreEqual(itemB.GetHashCode(), itemA.GetHashCode());
            Assert.AreNotEqual(itemC.GetHashCode(), itemA.GetHashCode());

            itemA = new Map { [true] = false, [0] = 1 };
            itemB = new Map { [true] = false, [0] = 1 };
            itemC = new Map { [true] = false, [0] = 2 };

            Assert.AreEqual(itemB.GetHashCode(), itemA.GetHashCode());
            Assert.AreNotEqual(itemC.GetHashCode(), itemA.GetHashCode());

            // Test CompoundType GetHashCode for subitems
            var junk = new Array { true, false, 0 };
            itemA = new Map { [true] = junk, [0] = junk };
            itemB = new Map { [true] = junk, [0] = junk };
            itemC = new Map { [true] = junk, [0] = 2 };

            Assert.AreEqual(itemB.GetHashCode(), itemA.GetHashCode());
            Assert.AreNotEqual(itemC.GetHashCode(), itemA.GetHashCode());

            itemA = new InteropInterface(123);
            itemB = new InteropInterface(123);
            itemC = new InteropInterface(124);

            Assert.AreEqual(itemB.GetHashCode(), itemA.GetHashCode());
            Assert.AreNotEqual(itemC.GetHashCode(), itemA.GetHashCode());

            var script = new Script(System.Array.Empty<byte>());
            itemA = new Pointer(script, 123);
            itemB = new Pointer(script, 123);
            itemC = new Pointer(script, 1234);

            Assert.AreEqual(itemB.GetHashCode(), itemA.GetHashCode());
            Assert.AreNotEqual(itemC.GetHashCode(), itemA.GetHashCode());
        }

        [TestMethod]
        public void TestNull()
        {
            StackItem nullItem = System.Array.Empty<byte>();
            Assert.AreNotEqual(StackItem.Null, nullItem);

            nullItem = new Null();
            Assert.AreEqual(StackItem.Null, nullItem);
        }

        [TestMethod]
        public void TestEqual()
        {
            StackItem itemA = "NEO";
            StackItem itemB = "NEO";
            StackItem itemC = "SmartEconomy";
            StackItem itemD = "Smarteconomy";
            StackItem itemE = "smarteconomy";

            Assert.IsTrue(itemA.Equals(itemB));
            Assert.IsFalse(itemA.Equals(itemC));
            Assert.IsFalse(itemC.Equals(itemD));
            Assert.IsFalse(itemD.Equals(itemE));
            Assert.IsFalse(itemA.Equals(new object()));
        }

        [TestMethod]
        public void TestCast()
        {
            // Signed byte

            StackItem item = sbyte.MaxValue;

            Assert.IsInstanceOfType(item, typeof(Integer));
            Assert.AreEqual(new BigInteger(sbyte.MaxValue), ((Integer)item).GetInteger());

            // Unsigned byte

            item = byte.MaxValue;

            Assert.IsInstanceOfType(item, typeof(Integer));
            Assert.AreEqual(new BigInteger(byte.MaxValue), ((Integer)item).GetInteger());

            // Signed short

            item = short.MaxValue;

            Assert.IsInstanceOfType(item, typeof(Integer));
            Assert.AreEqual(new BigInteger(short.MaxValue), ((Integer)item).GetInteger());

            // Unsigned short

            item = ushort.MaxValue;

            Assert.IsInstanceOfType(item, typeof(Integer));
            Assert.AreEqual(new BigInteger(ushort.MaxValue), ((Integer)item).GetInteger());

            // Signed integer

            item = int.MaxValue;

            Assert.IsInstanceOfType(item, typeof(Integer));
            Assert.AreEqual(new BigInteger(int.MaxValue), ((Integer)item).GetInteger());

            // Unsigned integer

            item = uint.MaxValue;

            Assert.IsInstanceOfType(item, typeof(Integer));
            Assert.AreEqual(new BigInteger(uint.MaxValue), ((Integer)item).GetInteger());

            // Signed long

            item = long.MaxValue;

            Assert.IsInstanceOfType(item, typeof(Integer));
            Assert.AreEqual(new BigInteger(long.MaxValue), ((Integer)item).GetInteger());

            // Unsigned long

            item = ulong.MaxValue;

            Assert.IsInstanceOfType(item, typeof(Integer));
            Assert.AreEqual(new BigInteger(ulong.MaxValue), ((Integer)item).GetInteger());

            // BigInteger

            item = BigInteger.MinusOne;

            Assert.IsInstanceOfType(item, typeof(Integer));
            Assert.AreEqual(new BigInteger(-1), ((Integer)item).GetInteger());

            // Boolean

            item = true;

            Assert.IsInstanceOfType(item, typeof(Boolean));
            Assert.IsTrue(item.GetBoolean());

            // ByteString

            item = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09 };

            Assert.IsInstanceOfType(item, typeof(ByteString));
            CollectionAssert.AreEqual(new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09 }, item.GetSpan().ToArray());
        }

        [TestMethod]
        public void TestDeepCopy()
        {
            Array a = new()
            {
                true,
                1,
                new byte[] { 1 },
                StackItem.Null,
                new Buffer(new byte[] { 1 }),
                new Map { [0] = 1, [2] = 3 },
                new Struct { 1, 2, 3 }
            };
            a.Add(a);
            Array aa = (Array)a.DeepCopy();
            Assert.AreNotEqual(a, aa);
            Assert.AreSame(aa, aa[^1]);
            Assert.IsTrue(a[^2].Equals(aa[^2], ExecutionEngineLimits.Default));
            Assert.AreNotSame(a[^2], aa[^2]);
        }
    }
}
