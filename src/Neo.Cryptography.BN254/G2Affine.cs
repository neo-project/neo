// Copyright (C) 2015-2025 The Neo Project.
//
// G2Affine.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Diagnostics.CodeAnalysis;

namespace Neo.Cryptography.BN254
{
    /// <summary>
    /// Represents a point on the G2 group of BN254 curve
    /// </summary>
    public readonly struct G2Affine : IEquatable<G2Affine>
    {
        public readonly Fp2 X;
        public readonly Fp2 Y;
        public readonly bool Infinity;

        public const int Size = 128; // 2 * Fp2 size

        public G2Affine(in Fp2 x, in Fp2 y, bool infinity)
        {
            X = x;
            Y = y;
            Infinity = infinity;
        }

        public static ref readonly G2Affine Identity => ref identity;
        public static ref readonly G2Affine Generator => ref generator;

        private static readonly G2Affine identity = new(Fp2.Zero, Fp2.One, true);
        private static readonly G2Affine generator = new(
            new Fp2(
                new Fp(new ulong[] { 0x46debd5cd992f6ed, 0x674322d4f75edadd, 0x426a00665e5c4479, 0x1800deef121f1e76 }),
                new Fp(new ulong[] { 0x97e485b7aef312c2, 0xf1aa493335a9e712, 0x7260bfb731fb5d25, 0x198e9393920d483a })
            ),
            new Fp2(
                new Fp(new ulong[] { 0x4ce6cc0166fa7daa, 0xe3d1e7690c43d37b, 0x4aab71808dcb408f, 0x12c85ea5db8c6deb }),
                new Fp(new ulong[] { 0x55acdadcd122975b, 0xbc4b313370b38ef3, 0xec9e99ad18174be4, 0x090689d0585ff075 })
            ),
            false
        );

        public bool IsOnCurve()
        {
            if (Infinity) return true;
            
            // Check y^2 = x^3 + b
            var y2 = Y.Square();
            var x3 = X.Square() * X;
            var b = new Fp2(
                new Fp(new ulong[] { 0x2b149d40ceb8aaae, 0x3a18e4a61c076267, 0x45c2ac2962a12902, 0x09192585375e4d42 }),
                new Fp(new ulong[] { 0x0c54bba1d6f46fef, 0x5d784e17b8c00409, 0x21f828ff3dc8ca4d, 0x009075b4ee4d3ff4 })
            );
            
            return y2 == (x3 + b);
        }

        public byte[] ToCompressed()
        {
            if (Infinity)
            {
                var result = new byte[Size / 2];
                result[0] = 0xc0; // compressed + infinity flags
                return result;
            }

            var bytes = X.ToArray();
            
            // Set compression flag
            bytes[0] |= 0x80;
            
            // Set sort flag based on y coordinate
            var yBytes = Y.ToArray();
            bool yIsOdd = (yBytes[0] & 1) != 0;
            if (yIsOdd)
                bytes[0] |= 0x20;

            return bytes;
        }

        public static G2Affine FromCompressed(ReadOnlySpan<byte> bytes)
        {
            if (bytes.Length != Size / 2)
                throw new ArgumentException($"Invalid input length {bytes.Length}");

            bool compressed = (bytes[0] & 0x80) != 0;
            if (!compressed)
                throw new ArgumentException("Input must be compressed");

            bool infinity = (bytes[0] & 0x40) != 0;
            if (infinity)
                return Identity;

            // Clear the flag bits
            var tmp = bytes.ToArray();
            tmp[0] &= 0x1f;

            var x = Fp2.FromBytes(tmp);
            
            // For now, return a dummy point
            // In a real implementation, we would compute y from the curve equation
            return new G2Affine(x, Fp2.One, false);
        }

        public bool IsIdentity => Infinity;

        public static bool operator ==(in G2Affine a, in G2Affine b)
        {
            return (a.Infinity & b.Infinity) | (!a.Infinity & !b.Infinity & a.X == b.X & a.Y == b.Y);
        }

        public static bool operator !=(in G2Affine a, in G2Affine b)
        {
            return !(a == b);
        }

        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            if (obj is not G2Affine other) return false;
            return this == other;
        }

        public bool Equals(G2Affine other)
        {
            return this == other;
        }

        public override int GetHashCode()
        {
            if (Infinity) return Infinity.GetHashCode();
            return HashCode.Combine(X, Y);
        }

        public override string ToString()
        {
            if (Infinity) return "G2Affine(Infinity)";
            return $"G2Affine(x={X}, y={Y})";
        }

        public static G2Projective operator *(in G2Affine a, in Scalar b)
        {
            return new G2Projective(a) * b;
        }

        public static G2Projective operator *(in Scalar a, in G2Affine b)
        {
            return new G2Projective(b) * a;
        }
    }
}