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
            var a = new Array
            {
                true,
                1,
                new byte[] { 1 },
                StackItem.Null,
                new Buffer([1]),
                new Map { [0] = 1, [2] = 3 },
                new Struct { 1, 2, 3 }
            };
            a.Add(a);
            var aa = (Array)a.DeepCopy();
            Assert.AreNotEqual(a, aa);
            Assert.AreSame(aa, aa[^1]);
            Assert.IsTrue(a[^2].Equals(aa[^2], ExecutionEngineLimits.Default));
            Assert.AreNotSame(a[^2], aa[^2]);
        }

        [TestMethod]
        public void TestMinIntegerAbs()
        {
            const string minLiteral = "-57896044618658097711785492504343953926634992332820282019728792003956564819968";
            var minInt256 = BigInteger.Parse(minLiteral);

            // Throw exception because of the size of the integer is too large(33 bytes > 32 bytes)
            Assert.ThrowsExactly<System.ArgumentException>(() => _ = new Integer(BigInteger.Abs(minInt256)));
        }

        [TestMethod]
        public void TestIEquatable()
        {
            StackItem expectedBoolean = true;
            StackItem expectedInteger = 1;
            StackItem expectedByteString = new byte[] { 1 };

            var expectedBuffer = new Buffer([1]);
            var expectedMap = new Map { [0] = 1, [2] = 3 };
            var expectedStruct = new Struct { 1, 2, 3 };
            var expectedArray = new Array
            {
                null,
                true,
                1,
                new byte[] { 1 },
                StackItem.Null,
                new Buffer([1]),
                new Map { [0] = 1, [2] = 3 },
                new Struct { 1, 2, 3 }
            };

            Boolean actualBooleanOne = true;
            Boolean actualBooleanTwo = false;

            Integer actualIntegerOne = 1;
            Integer actualIntegerTwo = 2;

            ByteString actualByteStringOne = new byte[] { 1, };
            ByteString actualByteStringTwo = new byte[] { 2, };

            var actualBufferOne = expectedBuffer;
            var actualBufferTwo = new Buffer([2,]);

            var actualMapOne = expectedMap;
            var actualMapTwo = new Map { [4] = 5, [6] = 7, };

            var actualStructOne = new Struct { 1, 2, 3, };
            var actualStructTwo = new Struct { 4, 5, 6, };

            var actualArrayOne = expectedArray;

            var actualArrayTwo = new Array
            {
                new Struct { 1, 2, 3 },
                new Map { [0] = 1, [2] = 3 },
                new Buffer([1]),
                StackItem.Null,
                new byte[] { 1 },
                1,
                true,
                null,
            };

            Assert.AreEqual(true, actualBooleanOne);
            Assert.AreEqual(1, actualIntegerOne);
            Assert.AreEqual(new byte[] { 1 }, actualByteStringOne);

            Assert.AreEqual(expectedBoolean, actualBooleanOne);
            Assert.AreEqual(expectedInteger, actualIntegerOne);
            Assert.AreEqual(expectedByteString, actualByteStringOne);

            Assert.AreEqual(expectedBuffer, actualBufferOne);
            Assert.AreEqual(expectedMap, actualMapOne);
            Assert.AreEqual(expectedStruct, actualStructOne);
            Assert.AreEqual(expectedArray, actualArrayOne);

            Assert.AreNotEqual(true, actualBooleanTwo);
            Assert.AreNotEqual(1, actualIntegerTwo);
            Assert.AreNotEqual(new byte[] { 1 }, actualByteStringTwo);

            Assert.AreNotEqual(expectedBoolean, actualBooleanTwo);
            Assert.AreNotEqual(expectedInteger, actualIntegerTwo);
            Assert.AreNotEqual(expectedByteString, actualByteStringTwo);

            Assert.AreNotEqual(expectedBuffer, actualBufferTwo);
            Assert.AreNotEqual(expectedMap, actualMapTwo);
            Assert.AreNotEqual(expectedStruct, actualStructTwo);
            Assert.AreNotEqual(expectedArray, actualArrayTwo);
        }
    }
}
