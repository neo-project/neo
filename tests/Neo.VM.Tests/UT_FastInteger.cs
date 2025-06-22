// Copyright (C) 2015-2025 The Neo Project.
//
// UT_FastInteger.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.VM.Types;
using System;
using System.Numerics;

namespace Neo.VM.Tests
{
    [TestClass]
    public class UT_FastInteger
    {
        [TestMethod]
        public void TestCreation()
        {
            // Test creation with various types
            var fi1 = FastInteger.Create(42L);
            var fi2 = FastInteger.Create(new BigInteger(42));
            var fi3 = FastInteger.Create(-256L);
            var fi4 = FastInteger.Create(256L);

            Assert.AreEqual(42, fi1.GetLong());
            Assert.AreEqual(42, fi2.GetLong());
            Assert.AreEqual(-256, fi3.GetLong());
            Assert.AreEqual(256, fi4.GetLong());
        }

        [TestMethod]
        public void TestCaching()
        {
            // Test that cached values return the same instance
            var fi1 = FastInteger.Create(0);
            var fi2 = FastInteger.Create(0);
            var fi3 = FastInteger.Create(1);
            var fi4 = FastInteger.Create(1);
            var fi5 = FastInteger.Create(-1);
            var fi6 = FastInteger.Create(-1);

            Assert.AreSame(fi1, fi2);  // Same instance for cached value
            Assert.AreSame(fi3, fi4);  // Same instance for cached value
            Assert.AreSame(fi5, fi6);  // Same instance for cached value

            // Test common static instances
            Assert.AreSame(FastInteger.Zero, FastInteger.Create(0));
            Assert.AreSame(FastInteger.One, FastInteger.Create(1));
            Assert.AreSame(FastInteger.MinusOne, FastInteger.Create(-1));
        }

        [TestMethod]
        public void TestArithmetic()
        {
            var a = FastInteger.Create(10);
            var b = FastInteger.Create(5);

            // Test addition
            var sum = FastInteger.Add(a, b);
            Assert.AreEqual(15, sum.GetLong());

            // Test subtraction
            var diff = FastInteger.Subtract(a, b);
            Assert.AreEqual(5, diff.GetLong());

            // Test multiplication
            var product = FastInteger.Multiply(a, b);
            Assert.AreEqual(50, product.GetLong());

            // Test division
            var quotient = FastInteger.Divide(a, b);
            Assert.AreEqual(2, quotient.GetLong());

            // Test modulo
            var remainder = FastInteger.Modulo(a, b);
            Assert.AreEqual(0, remainder.GetLong());
        }

        [TestMethod]
        public void TestComparisons()
        {
            var a = FastInteger.Create(10);
            var b = FastInteger.Create(5);
            var c = FastInteger.Create(10);

            // Test equality
            Assert.IsTrue(a.Equals(c));
            Assert.IsFalse(a.Equals(b));

            // Test comparison operators
            Assert.IsTrue(a > b);
            Assert.IsFalse(a < b);
            Assert.IsTrue(a >= c);
            Assert.IsTrue(a <= c);
        }

        [TestMethod]
        public void TestOverflow()
        {
            // Test long overflow to BigInteger
            var maxLong = FastInteger.Create(long.MaxValue);
            var one = FastInteger.Create(1);

            var overflow = FastInteger.Add(maxLong, one);
            // Should still work but internally use BigInteger
            Assert.AreEqual(new BigInteger(long.MaxValue) + 1, overflow.GetInteger());

            // Test that it handles values outside long range
            var bigValue = FastInteger.Create(new BigInteger(long.MaxValue) + 100);
            Assert.AreEqual(new BigInteger(long.MaxValue) + 100, bigValue.GetInteger());
        }

        [TestMethod]
        public void TestIntegerFactory()
        {
            // Test IntegerFactory creates appropriate types
            var small = IntegerFactory.Create(42);
            var large = IntegerFactory.Create(new BigInteger(long.MaxValue) + 1000);

            Assert.IsInstanceOfType(small, typeof(FastInteger));
            Assert.IsInstanceOfType(large, typeof(Integer));

            // Test optimized arithmetic
            var a = IntegerFactory.Create(10);
            var b = IntegerFactory.Create(5);

            Assert.IsTrue(IntegerFactory.TryAdd(a, b, out var result));
            Assert.AreEqual(15, result.GetInteger());
        }

        [TestMethod]
        public void TestCompatibilityWithExistingInteger()
        {
            // Test that FastInteger and Integer can work together
            var fastInt = FastInteger.Create(42);
            var regularInt = new Integer(42);

            // Should be equal when compared via GetInteger()
            Assert.AreEqual(fastInt.GetInteger(), regularInt.GetInteger());
            Assert.AreEqual(fastInt.GetBoolean(), regularInt.GetBoolean());

            // Test mixed arithmetic using IntegerFactory
            Assert.IsTrue(IntegerFactory.TryAdd(fastInt, regularInt, out var sum));
            Assert.AreEqual(84, sum.GetInteger());
        }

        [TestMethod]
        public void TestSpecialValues()
        {
            // Test zero
            var zero = FastInteger.Zero;
            Assert.AreEqual(0, zero.GetLong());
            Assert.IsFalse(zero.GetBoolean());

            // Test one
            var one = FastInteger.One;
            Assert.AreEqual(1, one.GetLong());
            Assert.IsTrue(one.GetBoolean());

            // Test minus one
            var minusOne = FastInteger.MinusOne;
            Assert.AreEqual(-1, minusOne.GetLong());
            Assert.IsTrue(minusOne.GetBoolean());
        }

        [TestMethod]
        public void TestTypeConversions()
        {
            var fi = FastInteger.Create(42);

            // Test GetInteger()
            Assert.AreEqual(new BigInteger(42), fi.GetInteger());

            // Test GetBoolean()
            Assert.IsTrue(fi.GetBoolean());
            Assert.IsFalse(FastInteger.Zero.GetBoolean());

            // Test GetInt32()
            Assert.AreEqual(42, fi.GetInt32());

            // Test TryGetLong()
            Assert.IsTrue(fi.TryGetLong(out long value));
            Assert.AreEqual(42L, value);
        }

        [TestMethod]
        public void TestStackItemProperties()
        {
            var fi = FastInteger.Create(42);

            // Test StackItem properties
            Assert.AreEqual(StackItemType.Integer, fi.Type);
            Assert.IsFalse(fi.IsNull);

            // Test Size property
            Assert.IsTrue(fi.Size > 0);

            // Test Memory property
            var memory = fi.Memory;
            Assert.IsTrue(memory.Length > 0);
        }
    }
}
