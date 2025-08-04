// Copyright (C) 2015-2025 The Neo Project.
//
// UT_Scalar.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using FluentAssertions;
using Neo.Cryptography.BN254;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Neo.UnitTests.Cryptography.BN254
{
    [TestClass]
    public class UT_Scalar
    {
        [TestMethod]
        public void TestConstants()
        {
            Scalar.Zero.Should().Be(new Scalar(0, 0, 0, 0));
            Scalar.One.Should().NotBe(Scalar.Zero);
        }

        [TestMethod]
        public void TestEquality()
        {
            var a = new Scalar(1, 2, 3, 4);
            var b = new Scalar(1, 2, 3, 4);
            var c = new Scalar(5, 6, 7, 8);

            a.Should().Be(b);
            a.Should().NotBe(c);
            (a == b).Should().BeTrue();
            (a != c).Should().BeTrue();
        }

        [TestMethod]
        public void TestArithmetic()
        {
            var a = Scalar.One;
            var b = Scalar.One;
            
            // Addition
            var sum = a + b;
            sum.Should().NotBe(Scalar.Zero);
            sum.Should().NotBe(Scalar.One);
            (Scalar.Zero + Scalar.One).Should().Be(Scalar.One);
            
            // Subtraction
            (sum - b).Should().Be(Scalar.One);
            (Scalar.One - Scalar.One).Should().Be(Scalar.Zero);
            
            // Multiplication
            (Scalar.One * Scalar.One).Should().Be(Scalar.One);
            (Scalar.Zero * Scalar.One).Should().Be(Scalar.Zero);
            
            // Negation
            var neg_a = -a;
            (a + neg_a).Should().Be(Scalar.Zero);
            (-Scalar.Zero).Should().Be(Scalar.Zero);
        }

        [TestMethod]
        public void TestSquare()
        {
            var a = Scalar.One;
            a.Square().Should().Be(Scalar.One);

            var b = new Scalar(2, 3, 4, 5);
            b.Square().Should().Be(b * b);
        }

        [TestMethod]
        public void TestInversion()
        {
            var a = Scalar.One;
            a.TryInvert(out var inv).Should().BeTrue();
            (a * inv).Should().Be(Scalar.One);

            Scalar.Zero.TryInvert(out _).Should().BeFalse();
        }

        [TestMethod]
        public void TestSerialization()
        {
            var a = new Scalar(1, 2, 3, 4);
            var bytes = a.ToArray();
            bytes.Length.Should().Be(32);

            var b = Scalar.FromBytes(bytes);
            b.Should().Be(a);
        }

        [TestMethod]
        public void TestFromBytesInvalidLength()
        {
            var invalidBytes = new byte[31];
            Action act = () => Scalar.FromBytes(invalidBytes);
            act.Should().Throw<ArgumentException>()
                .WithMessage("Invalid data length 31, expected 32");
        }

        [TestMethod]
        public void TestIsZeroAndIsOne()
        {
            Scalar.Zero.IsZero.Should().BeTrue();
            Scalar.Zero.IsOne.Should().BeFalse();
            
            Scalar.One.IsZero.Should().BeFalse();
            Scalar.One.IsOne.Should().BeTrue();
            
            var other = new Scalar(1, 2, 3, 4);
            other.IsZero.Should().BeFalse();
            other.IsOne.Should().BeFalse();
        }

        [TestMethod]
        public void TestFromRawUnchecked()
        {
            var s = Scalar.FromRawUnchecked(1, 2, 3, 4);
            s.Should().Be(new Scalar(1, 2, 3, 4));
        }

        [TestMethod]
        public void TestHashCode()
        {
            var a = new Scalar(1, 2, 3, 4);
            var b = new Scalar(1, 2, 3, 4);
            var c = new Scalar(5, 6, 7, 8);

            a.GetHashCode().Should().Be(b.GetHashCode());
            a.GetHashCode().Should().NotBe(c.GetHashCode());
        }
    }
}