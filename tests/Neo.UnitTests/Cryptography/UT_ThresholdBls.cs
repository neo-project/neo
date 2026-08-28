// Copyright (C) 2015-2026 The Neo Project.
//
// UT_ThresholdBls.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Cryptography;
using Neo.Cryptography.BLS12_381;
using System;
using System.Linq;
using System.Security.Cryptography;

namespace Neo.UnitTests.Cryptography
{
    [TestClass]
    public class UT_ThresholdBls
    {
        [TestMethod]
        public void SignVerify_RoundTrip()
        {
            var secret = RandomSecret();
            var pk = ThresholdBls.PublicKey(secret);
            var message = RandomBeacon.ComputeRoundId(1, 100, 0);
            var sig = ThresholdBls.Sign(secret, message);
            Assert.IsTrue(ThresholdBls.Verify(pk, message, sig));
            Assert.IsFalse(ThresholdBls.Verify(pk, RandomBeacon.ComputeRoundId(1, 101, 0), sig));
        }

        [TestMethod]
        public void Combine_ShuffledPartials_MatchesHonestSignature()
        {
            const int n = 7;
            const int k = 5;
            var secret = RandomSecret();
            var shares = ThresholdBls.SplitSecret(secret, n, k, RandomNumberGenerator.Create());
            var message = RandomBeacon.ComputeRoundId(0x4e454f33, 12_000_000, 0);
            var honest = ThresholdBls.Sign(secret, message);

            var partials = shares
                .Select((share, i) => ((byte)i, ThresholdBls.Sign(share, message)))
                .OrderBy(_ => RandomNumberGenerator.GetInt32(int.MaxValue))
                .ToArray();

            var combined = ThresholdBls.Combine(partials, k);
            Assert.IsTrue(combined.Equals(honest));
            Assert.IsTrue(ThresholdBls.Verify(ThresholdBls.PublicKey(secret), message, combined));
        }

        [TestMethod]
        public void Combine_RejectsTooFewPartials()
        {
            var secret = RandomSecret();
            var shares = ThresholdBls.SplitSecret(secret, 4, 3, RandomNumberGenerator.Create());
            var message = "rn"u8.ToArray();
            var partials = shares.Take(2)
                .Select((share, i) => ((byte)i, ThresholdBls.Sign(share, message)))
                .ToArray();
            Assert.ThrowsExactly<ArgumentException>(() => ThresholdBls.Combine(partials, 3));
        }

        [TestMethod]
        public void HashToG1_IsDeterministicAndNonIdentity()
        {
            var a = ThresholdBls.HashToG1("hello"u8);
            var b = ThresholdBls.HashToG1("hello"u8);
            var c = ThresholdBls.HashToG1("world"u8);
            Assert.IsTrue(a.Equals(b));
            Assert.IsFalse(a.Equals(c));
            Assert.IsFalse(a.IsIdentity);
            Assert.IsTrue(a.IsOnCurve);
            Assert.IsTrue(a.IsTorsionFree);
        }

        [TestMethod]
        public void SplitSecret_RejectsInvalidThreshold()
        {
            var secret = RandomSecret();
            var rng = RandomNumberGenerator.Create();
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => ThresholdBls.SplitSecret(secret, 0, 1, rng));
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => ThresholdBls.SplitSecret(secret, 3, 0, rng));
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => ThresholdBls.SplitSecret(secret, 3, 4, rng));
        }

        [TestMethod]
        public void Combine_RejectsNullAndDuplicates()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() => ThresholdBls.Combine(null, 1));
            var secret = RandomSecret();
            var message = "rn"u8.ToArray();
            var sig = ThresholdBls.Sign(secret, message);
            var dup = new (byte, G1Affine)[] { (0, sig), (0, sig) };
            Assert.ThrowsExactly<ArgumentException>(() => ThresholdBls.Combine(dup, 2));
        }

        [TestMethod]
        public void Verify_RejectsIdentity()
        {
            var secret = RandomSecret();
            var message = "rn"u8.ToArray();
            var sig = ThresholdBls.Sign(secret, message);
            Assert.IsFalse(ThresholdBls.Verify(G2Affine.Identity, message, sig));
            Assert.IsFalse(ThresholdBls.Verify(ThresholdBls.PublicKey(secret), message, G1Affine.Identity));
        }

        private static Scalar RandomSecret()
        {
            Span<byte> wide = stackalloc byte[64];
            RandomNumberGenerator.Fill(wide);
            return Scalar.FromBytesWide(wide);
        }
    }
}
