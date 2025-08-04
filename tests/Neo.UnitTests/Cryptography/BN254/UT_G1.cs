// Copyright (C) 2015-2025 The Neo Project.
//
// UT_G1.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Cryptography.BN254;
using System;

namespace Neo.UnitTests.Cryptography.BN254
{
    [TestClass]
    public class UT_G1
    {
        [TestMethod]
        public void TestG1AffineIdentity()
        {
            var identity = G1Affine.Identity;
            identity.IsIdentity.Should().BeTrue();
            identity.IsOnCurve().Should().BeTrue();
        }

        [TestMethod]
        public void TestG1AffineGenerator()
        {
            var generator = G1Affine.Generator;
            generator.IsIdentity.Should().BeFalse();
            generator.IsOnCurve().Should().BeTrue();
        }

        [TestMethod]
        public void TestG1AffineEquality()
        {
            var a = G1Affine.Generator;
            var b = G1Affine.Generator;
            var c = G1Affine.Identity;

            a.Should().Be(b);
            a.Should().NotBe(c);
            (a == b).Should().BeTrue();
            (a != c).Should().BeTrue();
        }

        [TestMethod]
        public void TestG1AffineSerialization()
        {
            var point = G1Affine.Generator;
            var compressed = point.ToCompressed();
            compressed.Length.Should().Be(32);

            var decompressed = G1Affine.FromCompressed(compressed);
            decompressed.Should().Be(point);
        }

        [TestMethod]
        public void TestG1AffineIdentitySerialization()
        {
            var identity = G1Affine.Identity;
            var compressed = identity.ToCompressed();
            compressed.Length.Should().Be(32);
            // All zeros for infinity in Neo format

            var decompressed = G1Affine.FromCompressed(compressed);
            decompressed.Should().Be(identity);
            decompressed.IsIdentity.Should().BeTrue();
        }

        [TestMethod]
        public void TestG1AffineInvalidSerialization()
        {
            // Wrong length
            Action act1 = () => G1Affine.FromCompressed(new byte[47]);
            act1.Should().Throw<ArgumentException>()
                .WithMessage("Invalid input length 47");

            // Not correct length
            var wrongLength = new byte[48];
            Action act2 = () => G1Affine.FromCompressed(wrongLength);
            act2.Should().Throw<ArgumentException>()
                .WithMessage("Invalid input length 48");
        }

        [TestMethod]
        public void TestG1ProjectiveIdentity()
        {
            var identity = G1Projective.Identity;
            identity.IsIdentity.Should().BeTrue();
            identity.IsOnCurve().Should().BeTrue();
        }

        [TestMethod]
        public void TestG1ProjectiveFromAffine()
        {
            var affine = G1Affine.Generator;
            var projective = new G1Projective(affine);
            projective.IsIdentity.Should().BeFalse();
            projective.IsOnCurve().Should().BeTrue();

            // Converting back should give the same point
            var affine2 = new G1Affine(projective);
            affine2.Should().Be(affine);
        }

        [TestMethod]
        public void TestG1ProjectiveAddition()
        {
            var a = G1Projective.Generator;
            var b = G1Projective.Generator;
            var c = a + b;

            c.IsIdentity.Should().BeFalse();
            c.IsOnCurve().Should().BeTrue();

            // Test identity
            (G1Projective.Identity + a).Should().Be(a);
            (a + G1Projective.Identity).Should().Be(a);
        }

        [TestMethod]
        public void TestG1ProjectiveDoubling()
        {
            var a = G1Projective.Generator;
            var doubled = a.Double();

            doubled.IsIdentity.Should().BeFalse();
            doubled.IsOnCurve().Should().BeTrue();
            doubled.Should().Be(a + a);
        }

        [TestMethod]
        public void TestG1ProjectiveScalarMultiplication()
        {
            var point = G1Projective.Generator;
            var scalar = Scalar.One;

            var result = point * scalar;
            result.Should().Be(point);

            var zeroResult = point * Scalar.Zero;
            zeroResult.IsIdentity.Should().BeTrue();
        }

        [TestMethod]
        public void TestG1ProjectiveNegation()
        {
            var a = G1Projective.Generator;
            var neg_a = -a;

            (a + neg_a).IsIdentity.Should().BeTrue();
            (-G1Projective.Identity).Should().Be(G1Projective.Identity);
        }

        [TestMethod]
        public void TestG1MixedAddition()
        {
            var affine = G1Affine.Generator;
            var projective = G1Projective.Generator;

            var result1 = projective + affine;
            var result2 = affine + projective;

            result1.Should().Be(result2);
            result1.IsOnCurve().Should().BeTrue();
        }

        [TestMethod]
        public void TestG1HashCode()
        {
            var a = G1Affine.Generator;
            var b = G1Affine.Generator;
            var c = G1Affine.Identity;

            a.GetHashCode().Should().Be(b.GetHashCode());
            a.GetHashCode().Should().NotBe(c.GetHashCode());
        }

        [TestMethod]
        public void TestG1ToString()
        {
            G1Affine.Identity.ToString().Should().Be("G1Affine(Infinity)");
            G1Projective.Identity.ToString().Should().Be("G1Projective(Identity)");

            var affine = G1Affine.Generator;
            affine.ToString().Should().Contain("G1Affine");
            affine.ToString().Should().Contain("x=");
            affine.ToString().Should().Contain("y=");
        }
    }
}
