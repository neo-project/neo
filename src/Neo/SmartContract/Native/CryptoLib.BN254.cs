// Copyright (C) 2015-2025 The Neo Project.
//
// CryptoLib.BN254.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Cryptography.BN254;
using Neo.VM.Types;
using System;

namespace Neo.SmartContract.Native
{
    partial class CryptoLib
    {
        /// <summary>
        /// Serialize a BN254 point.
        /// </summary>
        /// <param name="g">The point to be serialized.</param>
        /// <returns>The serialized point.</returns>
        [ContractMethod(CpuFee = 1 << 19)]
        public static byte[] Bn254Serialize(InteropInterface g)
        {
            return g.GetInterface<object>() switch
            {
                G1Affine p => p.ToCompressed(),
                G1Projective p => new G1Affine(p).ToCompressed(),
                G2Affine p => p.ToCompressed(),
                G2Projective p => new G2Affine(p).ToCompressed(),
                Gt p => p.ToArray(),
                _ => throw new ArgumentException("BN254 type mismatch")
            };
        }

        /// <summary>
        /// Deserialize a BN254 point.
        /// </summary>
        /// <param name="data">The point as byte array.</param>
        /// <returns>The deserialized point.</returns>
        [ContractMethod(CpuFee = 1 << 19)]
        public static InteropInterface Bn254Deserialize(byte[] data)
        {
            return data.Length switch
            {
                48 => new InteropInterface(G1Affine.FromCompressed(data)),
                64 => new InteropInterface(G2Affine.FromCompressed(data)),
                384 => new InteropInterface(Gt.FromBytes(data)),
                _ => throw new ArgumentException("Invalid BN254 point length"),
            };
        }

        /// <summary>
        /// Determines whether the specified BN254 points are equal.
        /// </summary>
        /// <param name="x">The first point.</param>
        /// <param name="y">The second point.</param>
        /// <returns><c>true</c> if the specified points are equal; otherwise, <c>false</c>.</returns>
        [ContractMethod(CpuFee = 1 << 5)]
        public static bool Bn254Equal(InteropInterface x, InteropInterface y)
        {
            return (x.GetInterface<object>(), y.GetInterface<object>()) switch
            {
                (G1Affine p1, G1Affine p2) => p1.Equals(p2),
                (G1Projective p1, G1Projective p2) => p1.Equals(p2),
                (G2Affine p1, G2Affine p2) => p1.Equals(p2),
                (G2Projective p1, G2Projective p2) => p1.Equals(p2),
                (Gt p1, Gt p2) => p1.Equals(p2),
                _ => throw new ArgumentException("BN254 type mismatch")
            };
        }

        /// <summary>
        /// Add operation of two BN254 points.
        /// </summary>
        /// <param name="x">The first point.</param>
        /// <param name="y">The second point.</param>
        /// <returns>The sum of the two points.</returns>
        [ContractMethod(CpuFee = 1 << 19)]
        public static InteropInterface Bn254Add(InteropInterface x, InteropInterface y)
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
                _ => throw new ArgumentException("BN254 type mismatch")
            };
        }

        /// <summary>
        /// Mul operation of BN254 point and scalar.
        /// </summary>
        /// <param name="x">The point.</param>
        /// <param name="mul">Multiplier, 32 bytes, little-endian.</param>
        /// <param name="neg">Negative number flag.</param>
        /// <returns>The result of scalar multiplication.</returns>
        [ContractMethod(CpuFee = 1 << 21)]
        public static InteropInterface Bn254Mul(InteropInterface x, byte[] mul, bool neg)
        {
            Scalar X = neg ? -Scalar.FromBytes(mul) : Scalar.FromBytes(mul);
            return x.GetInterface<object>() switch
            {
                G1Affine p => new(p * X),
                G1Projective p => new(p * X),
                G2Affine p => new(p * X),
                G2Projective p => new(p * X),
                Gt p => new(p * X),
                _ => throw new ArgumentException("BN254 type mismatch")
            };
        }

        /// <summary>
        /// Pairing operation of BN254 g1 and g2 points.
        /// </summary>
        /// <param name="g1">The g1 point.</param>
        /// <param name="g2">The g2 point.</param>
        /// <returns>The pairing result in Gt.</returns>
        [ContractMethod(CpuFee = 1 << 23)]
        public static InteropInterface Bn254Pairing(InteropInterface g1, InteropInterface g2)
        {
            G1Affine g1a = g1.GetInterface<object>() switch
            {
                G1Affine g => g,
                G1Projective g => new(g),
                _ => throw new ArgumentException("BN254 type mismatch")
            };
            G2Affine g2a = g2.GetInterface<object>() switch
            {
                G2Affine g => g,
                G2Projective g => new(g),
                _ => throw new ArgumentException("BN254 type mismatch")
            };
            return new(Bn254.Pairing(in g1a, in g2a));
        }

        /// <summary>
        /// Multi-pairing check for BN254.
        /// Checks if e(g1[0], g2[0]) * ... * e(g1[n], g2[n]) = 1
        /// </summary>
        /// <param name="g1">Array of G1 points.</param>
        /// <param name="g2">Array of G2 points.</param>
        /// <returns><c>true</c> if the pairing check passes; otherwise, <c>false</c>.</returns>
        [ContractMethod(CpuFee = 1 << 23)]
        public static bool Bn254PairingCheck(InteropInterface[] g1, InteropInterface[] g2)
        {
            if (g1.Length != g2.Length)
                throw new ArgumentException("Input arrays must have the same length");
            
            if (g1.Length == 0)
                return true;

            var pairs = new (G1Affine, G2Affine)[g1.Length];
            
            for (int i = 0; i < g1.Length; i++)
            {
                G1Affine g1a = g1[i].GetInterface<object>() switch
                {
                    G1Affine g => g,
                    G1Projective g => new(g),
                    _ => throw new ArgumentException("BN254 G1 type mismatch")
                };
                
                G2Affine g2a = g2[i].GetInterface<object>() switch
                {
                    G2Affine g => g,
                    G2Projective g => new(g),
                    _ => throw new ArgumentException("BN254 G2 type mismatch")
                };
                
                pairs[i] = (g1a, g2a);
            }
            
            return Bn254.PairingCheck(pairs);
        }
    }
}