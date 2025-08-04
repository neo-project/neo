// Copyright (C) 2015-2025 The Neo Project.
//
// UT_Fp.cs file belongs to the neo project and is free
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
    public class UT_Fp
    {
        [TestMethod]
        public void TestConstants()
        {
            // Test that constants are properly initialized
            Fp.Zero.Should().Be(new Fp(0, 0, 0, 0));
            Fp.One.Should().NotBe(Fp.Zero);
            Fp.Modulus.Should().NotBe(Fp.Zero);
        }

        [TestMethod]
        public void TestEquality()
        {
            var a = new Fp(1, 2, 3, 4);
            var b = new Fp(1, 2, 3, 4);
            var c = new Fp(5, 6, 7, 8);

            a.Should().Be(b);
            a.Should().NotBe(c);
            (a == b).Should().BeTrue();
            (a != c).Should().BeTrue();
        }

        [TestMethod]
        public void TestAddition()
        {
            var a = Fp.One;
            var b = Fp.One;
            var c = a + b;

            c.Should().NotBe(Fp.Zero);
            c.Should().NotBe(Fp.One);

            // Test identity
            (Fp.Zero + Fp.One).Should().Be(Fp.One);
            (Fp.One + Fp.Zero).Should().Be(Fp.One);
        }

        [TestMethod]
        public void TestSubtraction()
        {
            var a = Fp.One + Fp.One;
            var b = Fp.One;
            var c = a - b;

            c.Should().Be(Fp.One);

            // Test identity
            (Fp.One - Fp.Zero).Should().Be(Fp.One);
            (Fp.One - Fp.One).Should().Be(Fp.Zero);
        }

        [TestMethod]
        public void TestMultiplication()
        {
            var a = Fp.One;
            var b = Fp.One;
            var c = a * b;

            c.Should().Be(Fp.One);

            // Test identity
            (Fp.Zero * Fp.One).Should().Be(Fp.Zero);
            (Fp.One * Fp.One).Should().Be(Fp.One);
        }

        [TestMethod]
        public void TestNegation()
        {
            var a = Fp.One;
            var neg_a = -a;

            (a + neg_a).Should().Be(Fp.Zero);
            (-Fp.Zero).Should().Be(Fp.Zero);
        }

        [TestMethod]
        public void TestSquare()
        {
            var a = Fp.One;
            var a_squared = a.Square();

            a_squared.Should().Be(Fp.One);

            // Test that square is same as self multiplication
            var b = new Fp(2, 3, 4, 5);
            b.Square().Should().Be(b * b);
        }

        [TestMethod]
        public void TestInversion()
        {
            var a = Fp.One;
            a.TryInvert(out var inv).Should().BeTrue();
            (a * inv).Should().Be(Fp.One);

            // Test zero has no inverse
            Fp.Zero.TryInvert(out _).Should().BeFalse();
        }

        [TestMethod]
        public void TestSerialization()
        {
            var a = new Fp(1, 2, 3, 4);
            var bytes = a.ToArray();
            bytes.Length.Should().Be(32);

            var b = Fp.FromBytes(bytes);
            b.Should().Be(a);
        }

        [TestMethod]
        public void TestFromBytesInvalidLength()
        {
            var invalidBytes = new byte[31]; // Wrong length
            Action act = () => Fp.FromBytes(invalidBytes);
            act.Should().Throw<ArgumentException>()
                .WithMessage("Invalid data length 31, expected 32");
        }

        [TestMethod]
        public void TestIsZeroAndIsOne()
        {
            Fp.Zero.IsZero.Should().BeTrue();
            Fp.Zero.IsOne.Should().BeFalse();
            
            Fp.One.IsZero.Should().BeFalse();
            Fp.One.IsOne.Should().BeTrue();
            
            var other = new Fp(1, 2, 3, 4);
            other.IsZero.Should().BeFalse();
            other.IsOne.Should().BeFalse();
        }

        [TestMethod]
        public void TestToString()
        {
            var a = new Fp(0x0123456789abcdef, 0xfedcba9876543210, 
                          0x1111111111111111, 0x2222222222222222);
            var str = a.ToString();
            str.Should().Contain("2222222222222222");
            str.Should().Contain("1111111111111111");
            str.Should().Contain("fedcba9876543210");
            str.Should().Contain("0123456789abcdef");
        }
    }
}