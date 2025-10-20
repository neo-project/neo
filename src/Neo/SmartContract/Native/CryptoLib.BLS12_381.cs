// Copyright (C) 2015-2025 The Neo Project.
//
// CryptoLib.BLS12_381.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Cryptography.BLS12_381;
using Neo.VM.Types;
using System;
using Array = Neo.VM.Types.Array;
using VMBuffer = Neo.VM.Types.Buffer;

namespace Neo.SmartContract.Native
{
    partial class CryptoLib
    {
        /// <summary>
        /// Serialize a bls12381 point.
        /// </summary>
        /// <param name="g">The point to be serialized.</param>
        /// <returns></returns>
        [ContractMethod(CpuFee = 1 << 19)]
        public static byte[] Bls12381Serialize(InteropInterface g)
        {
            return g.GetInterface<object>() switch
            {
                G1Affine p => p.ToCompressed(),
                G1Projective p => new G1Affine(p).ToCompressed(),
                G2Affine p => p.ToCompressed(),
                G2Projective p => new G2Affine(p).ToCompressed(),
                Gt p => p.ToArray(),
                _ => throw new ArgumentException("BLS12-381 type mismatch")
            };
        }

        /// <summary>
        /// Deserialize a bls12381 point.
        /// </summary>
        /// <param name="data">The point as byte array.</param>
        /// <returns></returns>
        [ContractMethod(CpuFee = 1 << 19)]
        public static InteropInterface Bls12381Deserialize(byte[] data)
        {
            return data.Length switch
            {
                48 => new InteropInterface(G1Affine.FromCompressed(data)),
                96 => new InteropInterface(G2Affine.FromCompressed(data)),
                576 => new InteropInterface(Gt.FromBytes(data)),
                _ => throw new ArgumentException("Invalid BLS12-381 point length"),
            };
        }

        /// <summary>
        /// Determines whether the specified points are equal.
        /// </summary>
        /// <param name="x">The first point.</param>
        /// <param name="y">Teh second point.</param>
        /// <returns><c>true</c> if the specified points are equal; otherwise, <c>false</c>.</returns>
        [ContractMethod(CpuFee = 1 << 5)]
        public static bool Bls12381Equal(InteropInterface x, InteropInterface y)
        {
            return (x.GetInterface<object>(), y.GetInterface<object>()) switch
            {
                (G1Affine p1, G1Affine p2) => p1.Equals(p2),
                (G1Projective p1, G1Projective p2) => p1.Equals(p2),
                (G2Affine p1, G2Affine p2) => p1.Equals(p2),
                (G2Projective p1, G2Projective p2) => p1.Equals(p2),
                (Gt p1, Gt p2) => p1.Equals(p2),
                _ => throw new ArgumentException("BLS12-381 type mismatch")
            };
        }

        /// <summary>
        /// Add operation of two points.
        /// </summary>
        /// <param name="x">The first point.</param>
        /// <param name="y">The second point.</param>
        /// <returns></returns>
        [ContractMethod(CpuFee = 1 << 19)]
        public static InteropInterface Bls12381Add(InteropInterface x, InteropInterface y)
        {
            return (x.GetInterface<object>(), y.GetInterface<object>()) switch
            {
                (G1Affine p1, G1Affine p2) => new(new G1Projective(p1) + p2),
                (G1Affine p1, G1Projective p2) => new(p1 + p2),
                (G1Projective p1, G1Affine p2) => new(p1 + p2),
                (G1Projective p1, G1Projective p2) => new(p1 + p2),
                (G2Affine p1, G2Affine p2) => new(new G2Projective(p1) + p2),
                (G2Affine p1, G2Projective p2) => new(p1 + p2),
                (G2Projective p1, G2Affine p2) => new(p1 + p2),
                (G2Projective p1, G2Projective p2) => new(p1 + p2),
                (Gt p1, Gt p2) => new(p1 + p2),
                _ => throw new ArgumentException("BLS12-381 type mismatch")
            };
        }

        /// <summary>
        /// Mul operation of gt point and multiplier
        /// </summary>
        /// <param name="x">The point</param>
        /// <param name="mul">Multiplier,32 bytes,little-endian</param>
        /// <param name="neg">negative number</param>
        /// <returns></returns>
        [ContractMethod(CpuFee = 1 << 21)]
        public static InteropInterface Bls12381Mul(InteropInterface x, byte[] mul, bool neg)
        {
            Scalar X = neg ? -Scalar.FromBytes(mul) : Scalar.FromBytes(mul);
            return x.GetInterface<object>() switch
            {
                G1Affine p => new(p * X),
                G1Projective p => new(p * X),
                G2Affine p => new(p * X),
                G2Projective p => new(p * X),
                Gt p => new(p * X),
                _ => throw new ArgumentException("BLS12-381 type mismatch")
            };
        }

        /// <summary>
        /// Multi exponentiation operation for bls12381 points.
        /// </summary>
        /// <param name="pairs">Array of [point, scalar] pairs.</param>
        /// <returns>The accumulated point.</returns>
        [ContractMethod(Hardfork.HF_Faun, CpuFee = 1 << 23)]
        public static InteropInterface Bls12381MultiExp(Array pairs)
        {
            if (pairs is null || pairs.Count == 0)
                throw new ArgumentException("BLS12-381 multi exponent requires at least one pair");

            bool? useG2 = null;
            G1Projective g1Accumulator = G1Projective.Identity;
            G2Projective g2Accumulator = G2Projective.Identity;

            foreach (StackItem item in pairs)
            {
                if (item is not Array pair || pair.Count != 2)
                    throw new ArgumentException("BLS12-381 multi exponent pair must contain point and scalar");

                if (pair[0] is not InteropInterface pointInterface)
                    throw new ArgumentException("BLS12-381 multi exponent requires interop points");

                var point = pointInterface.GetInterface<object>();
                switch (point)
                {
                    case G1Affine g1Affine:
                        EnsureGroupType(ref useG2, false);
                        {
                            var scalar = ParseScalar(pair[1]);
                            if (!scalar.IsZero)
                                g1Accumulator += new G1Projective(g1Affine) * scalar;
                        }
                        break;
                    case G1Projective g1Projective:
                        EnsureGroupType(ref useG2, false);
                        {
                            var scalar = ParseScalar(pair[1]);
                            if (!scalar.IsZero)
                                g1Accumulator += g1Projective * scalar;
                        }
                        break;
                    case G2Affine g2Affine:
                        EnsureGroupType(ref useG2, true);
                        {
                            var scalar = ParseScalar(pair[1]);
                            if (!scalar.IsZero)
                                g2Accumulator += new G2Projective(g2Affine) * scalar;
                        }
                        break;
                    case G2Projective g2Projective:
                        EnsureGroupType(ref useG2, true);
                        {
                            var scalar = ParseScalar(pair[1]);
                            if (!scalar.IsZero)
                                g2Accumulator += g2Projective * scalar;
                        }
                        break;
                    default:
                        throw new ArgumentException("BLS12-381 type mismatch");
                }
            }

            if (useG2 is null)
                throw new ArgumentException("BLS12-381 multi exponent requires at least one valid pair");

            return useG2.Value
                ? new InteropInterface(g2Accumulator)
                : new InteropInterface(g1Accumulator);
        }

        /// <summary>
        /// Pairing operation of g1 and g2
        /// </summary>
        /// <param name="g1">The g1 point.</param>
        /// <param name="g2">The g2 point.</param>
        /// <returns></returns>
        [ContractMethod(CpuFee = 1 << 23)]
        public static InteropInterface Bls12381Pairing(InteropInterface g1, InteropInterface g2)
        {
            G1Affine g1a = g1.GetInterface<object>() switch
            {
                G1Affine g => g,
                G1Projective g => new(g),
                _ => throw new ArgumentException("BLS12-381 type mismatch")
            };
            G2Affine g2a = g2.GetInterface<object>() switch
            {
                G2Affine g => g,
                G2Projective g => new(g),
                _ => throw new ArgumentException("BLS12-381 type mismatch")
            };
            return new(Bls12.Pairing(in g1a, in g2a));
        }

        private static void EnsureGroupType(ref bool? current, bool isG2)
        {
            if (current is null)
            {
                current = isG2;
            }
            else if (current.Value != isG2)
            {
                throw new ArgumentException("BLS12-381 multi exponent cannot mix groups");
            }
        }

        private static Scalar ParseScalar(StackItem scalarItem)
        {
            ReadOnlySpan<byte> data = scalarItem switch
            {
                ByteString bs when bs.GetSpan().Length == Scalar.Size => bs.GetSpan(),
                VMBuffer buffer when buffer.Size == Scalar.Size => buffer.InnerBuffer.Span,
                _ => throw new ArgumentException("BLS12-381 scalar must be 32 bytes"),
            };

            Span<byte> littleEndian = stackalloc byte[Scalar.Size];
            data.CopyTo(littleEndian);

            try
            {
                return Scalar.FromBytes(littleEndian);
            }
            catch (FormatException)
            {
                var wide = new byte[Scalar.Size * 2];
                littleEndian.CopyTo(wide);
                return Scalar.FromBytesWide(wide);
            }
        }
    }
}
