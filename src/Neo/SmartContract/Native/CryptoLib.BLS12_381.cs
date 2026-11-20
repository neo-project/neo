// Copyright (C) 2015-2026 The Neo Project.
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
        /// <param name="engine">The application engine.</param>
        /// <param name="data">The point as byte array.</param>
        /// <returns></returns>
        [ContractMethod(CpuFee = 1 << 19)]
        public static InteropInterface Bls12381Deserialize(ApplicationEngine engine, byte[] data)
        {
            return data.Length switch
            {
                48 => new InteropInterface(G1Affine.FromCompressed(data)),
                // Handle the canonical G2 identity from Iara onwards.
                96 => engine.IsHardforkEnabled(Hardfork.HF_Iara) && data[0] == 0xc0 && data.AsSpan(1).IndexOfAnyExcept((byte)0) < 0
                    ? new InteropInterface(G2Affine.Identity)
                    : new InteropInterface(G2Affine.FromCompressed(data)),
                576 => new InteropInterface(DeserializeGt(engine, data)),
                _ => throw new ArgumentException("Invalid BLS12-381 point length"),
            };
        }

        private static Gt DeserializeGt(ApplicationEngine engine, byte[] data)
        {
            Gt gt = Gt.FromBytes(data);
            if (!engine.IsHardforkEnabled(Hardfork.HF_Iara))
                return gt;

            // The subgroup check costs roughly one Gt scalar multiplication.
            engine.AddFee((1 << 21) * engine.ExecFeePicoFactor, false);

            // Gt.FromBytes only validates the Fp12 encoding. Starting with Iara,
            // require membership in the order-r target group as well.
            ReadOnlySpan<byte> order =
            [
                0x73, 0xed, 0xa7, 0x53, 0x29, 0x9d, 0x7d, 0x48,
                0x33, 0x39, 0xd8, 0x08, 0x09, 0xa1, 0xd8, 0x05,
                0x53, 0xbd, 0xa4, 0x02, 0xff, 0xfe, 0x5b, 0xfe,
                0xff, 0xff, 0xff, 0xff, 0x00, 0x00, 0x00, 0x01
            ];
            Fp12 power = Fp12.One;
            foreach (byte value in order)
            {
                for (int bit = 7; bit >= 0; bit--)
                {
                    power = power.Square();
                    if ((value & (1 << bit)) != 0)
                        power *= gt.Value;
                }
            }
            if (power != Fp12.One)
                throw new FormatException("Invalid BLS12-381 Gt element");

            return gt;
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
    }
}
