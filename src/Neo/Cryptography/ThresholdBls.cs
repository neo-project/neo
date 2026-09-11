// Copyright (C) 2015-2026 The Neo Project.
//
// ThresholdBls.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Cryptography.BLS12_381;
using Neo.Extensions;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace Neo.Cryptography
{
    /// <summary>
    /// Threshold BLS (G1 signatures, G2 public keys) for DRB partials.
    /// Hash-to-G1 is a try-and-increment map (replace with RFC 9380 before mainnet).
    /// Shamir x-coordinates are validatorIndex + 1 (x = 0 is the secret).
    /// </summary>
    public static class ThresholdBls
    {
        private static readonly byte[] s_hashToG1Dst = "NEO-RNP-BLS-HASH-TO-G1-V1"u8.ToArray();
        private static readonly Fp s_curveB = ComputeCurveB();

        public static G1Affine G1Generator => G1Affine.Generator;

        public static G2Affine G2Generator => G2Affine.Generator;

        /// <summary>
        /// Splits <paramref name="secret"/> into <paramref name="n"/> shares with threshold <paramref name="k"/>.
        /// Share i is evaluated at x = i + 1.
        /// </summary>
        public static Scalar[] SplitSecret(Scalar secret, int n, int k, RandomNumberGenerator rng)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(n, 1);
            ArgumentOutOfRangeException.ThrowIfLessThan(k, 1);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(k, n);

            var coefficients = new Scalar[k];
            coefficients[0] = secret;
            Span<byte> wide = stackalloc byte[64];
            for (var i = 1; i < k; i++)
            {
                rng.GetBytes(wide);
                coefficients[i] = Scalar.FromBytesWide(wide);
            }

            var shares = new Scalar[n];
            for (var i = 0; i < n; i++)
            {
                var x = FromInt(i + 1);
                var y = coefficients[k - 1];
                for (var d = k - 2; d >= 0; d--)
                    y = y * x + coefficients[d];
                shares[i] = y;
            }
            return shares;
        }

        public static G2Affine PublicKey(Scalar secret) => new(G2Generator.ToCurve() * secret);

        public static G1Affine Sign(Scalar secret, ReadOnlySpan<byte> message)
        {
            var h = HashToG1(message);
            return new G1Affine(h.ToCurve() * secret);
        }

        public static bool Verify(G2Affine publicKey, ReadOnlySpan<byte> message, G1Affine signature)
        {
            if (signature.IsIdentity || publicKey.IsIdentity)
                return false;
            var h = HashToG1(message);
            var g2 = G2Generator;
            var lhs = Bls12.Pairing(in signature, in g2);
            var rhs = Bls12.Pairing(in h, in publicKey);
            return lhs.Equals(rhs);
        }

        /// <summary>
        /// Combines G1 partials. Each tuple uses a 0-based dBFT validator index.
        /// </summary>
        public static G1Affine Combine(IReadOnlyList<(byte ValidatorIndex, G1Affine Partial)> partials, int threshold)
        {
            ArgumentNullException.ThrowIfNull(partials);
            ArgumentOutOfRangeException.ThrowIfLessThan(threshold, 1);
            if (partials.Count < threshold)
                throw new ArgumentException($"Need at least {threshold} partials.", nameof(partials));

            var taken = new (int X, G1Affine Sig)[threshold];
            var seen = new HashSet<int>();
            var n = 0;
            foreach (var (index, sig) in partials)
            {
                var x = index + 1;
                if (!seen.Add(x))
                    throw new ArgumentException("Duplicate validator index.", nameof(partials));
                taken[n++] = (x, sig);
                if (n == threshold)
                    break;
            }

            var xs = new int[threshold];
            for (var i = 0; i < threshold; i++)
                xs[i] = taken[i].X;

            G1Projective acc = default;
            var started = false;
            for (var i = 0; i < threshold; i++)
            {
                var lambda = LagrangeAtZero(taken[i].X, xs);
                var term = taken[i].Sig.ToCurve() * lambda;
                acc = started ? acc + term : term;
                started = true;
            }
            return new G1Affine(acc);
        }

        public static G1Affine HashToG1(ReadOnlySpan<byte> message)
        {
            var dst = s_hashToG1Dst;
            for (var ctr = 0; ctr < 256; ctr++)
            {
                var payload = new byte[dst.Length + message.Length + 1];
                dst.CopyTo(payload, 0);
                message.CopyTo(payload.AsSpan(dst.Length));
                payload[^1] = (byte)ctr;
                var h0 = payload.Sha256();
                payload[^1] = (byte)(ctr ^ 0x5a);
                var h1 = payload.Sha256();
                var xBytes = new byte[48];
                Buffer.BlockCopy(h0, 0, xBytes, 0, 32);
                Buffer.BlockCopy(h1, 0, xBytes, 32, 16);

                // 48-byte big-endian integers often exceed p; keep the value in range.
                xBytes[0] &= 0x1f;
                Fp x;
                try
                {
                    x = Fp.FromBytes(xBytes);
                }
                catch (Exception)
                {
                    continue;
                }

                var rhs = x.Square() * x + s_curveB;
                Fp y;
                try
                {
                    y = rhs.Sqrt();
                }
                catch (Exception)
                {
                    continue;
                }

                var affine = new G1Affine(in x, in y);
                if (affine.IsIdentity || !affine.IsOnCurve)
                    continue;
                var cleared = new G1Affine(affine.ToCurve().ClearCofactor());
                if (cleared.IsIdentity)
                    continue;
                return cleared;
            }

            throw new InvalidOperationException("Failed to hash message to G1.");
        }

        public static Scalar FromInt(int value)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(value);
            var bytes = new byte[32];
            BinaryPrimitives.WriteInt32LittleEndian(bytes, value);
            return Scalar.FromBytes(bytes);
        }

        private static Scalar LagrangeAtZero(int xi, ReadOnlySpan<int> xs)
        {
            var num = Scalar.One;
            var den = Scalar.One;
            var sxi = FromInt(xi);
            foreach (var xj in xs)
            {
                if (xj == xi)
                    continue;
                var sxj = FromInt(xj);
                num *= -sxj;
                den *= sxi - sxj;
            }
            return num * den.Invert();
        }

        private static Fp ComputeCurveB()
        {
            var g = G1Affine.Generator;
            return g.Y.Square() - g.X.Square() * g.X;
        }
    }
}
