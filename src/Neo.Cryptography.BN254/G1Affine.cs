// Copyright (C) 2015-2025 The Neo Project.
//
// G1Affine.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Diagnostics.CodeAnalysis;
using static Neo.Cryptography.BN254.ConstantTimeUtility;
using static Neo.Cryptography.BN254.G1Constants;

namespace Neo.Cryptography.BN254
{
    public readonly struct G1Affine : IEquatable<G1Affine>
    {
        public readonly Fp X;
        public readonly Fp Y;
        public readonly bool Infinity;

        public G1Affine(in Fp x, in Fp y, bool infinity)
        {
            X = x;
            Y = y;
            Infinity = infinity;
        }

        public static ref readonly G1Affine Identity => ref identity;
        public static ref readonly G1Affine Generator => ref generator;

        private static readonly G1Affine identity = new(Fp.Zero, Fp.One, true);
        private static readonly G1Affine generator = new(GENERATOR_X, GENERATOR_Y, false);

        public G1Affine(in G1Projective p)
        {
            bool nonzero = p.Z.TryInvert(out Fp zinv);
            zinv = ConditionalSelect(in Fp.Zero, in zinv, nonzero);
            Fp x = p.X * zinv;
            Fp y = p.Y * zinv;

            var tmp = new G1Affine(in x, in y, false);
            this = ConditionalSelect(in identity, in tmp, nonzero);
        }

        public static G1Affine FromCompressed(ReadOnlySpan<byte> bytes)
        {
            if (bytes.Length != 32)
                throw new ArgumentException($"Invalid input length {bytes.Length}");

            // Check compression flag
            bool compressed = (bytes[0] & 0x80) != 0;
            if (!compressed)
                throw new ArgumentException("Input must be compressed");

            bool infinity = (bytes[0] & 0x40) != 0;
            bool sort = (bytes[0] & 0x20) != 0;

            if (infinity)
            {
                return Identity;
            }

            // Clear the flag bits
            var tmp = bytes.ToArray();
            tmp[0] &= 0x1f;

            Fp x = Fp.FromBytes(tmp);
            
            // Compute y from curve equation: y^2 = x^3 + b
            Fp y2 = x.Square() * x + new Fp(3, 0, 0, 0);
            
            // Compute square root
            if (!Sqrt(in y2, out Fp y))
                throw new ArgumentException("Invalid point - not on curve");

            // Select correct y based on sort flag
            bool yIsOdd = (y.ToArray()[0] & 1) != 0;
            if (yIsOdd != sort)
                y = -y;

            var result = new G1Affine(in x, in y, false);
            if (!result.IsOnCurve())
                throw new ArgumentException("Invalid point - not on curve");

            return result;
        }

        public byte[] ToCompressed()
        {
            if (Infinity)
            {
                var result = new byte[32];
                result[0] = 0xc0; // compressed + infinity flags
                return result;
            }

            var bytes = X.ToArray();
            
            // Set compression flag
            bytes[0] |= 0x80;
            
            // Set sort flag based on y coordinate
            bool yIsOdd = (Y.ToArray()[0] & 1) != 0;
            if (yIsOdd)
                bytes[0] |= 0x20;

            return bytes;
        }

        public bool IsOnCurve()
        {
            if (Infinity) return true;
            
            // Check y^2 = x^3 + b
            Fp y2 = Y.Square();
            Fp x3b = X.Square() * X + new Fp(3, 0, 0, 0);
            
            return y2 == x3b;
        }

        public bool IsIdentity => Infinity;

        public static bool operator ==(in G1Affine a, in G1Affine b)
        {
            return (a.Infinity & b.Infinity) | (!a.Infinity & !b.Infinity & a.X == b.X & a.Y == b.Y);
        }

        public static bool operator !=(in G1Affine a, in G1Affine b)
        {
            return !(a == b);
        }

        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            if (obj is not G1Affine other) return false;
            return this == other;
        }

        public bool Equals(G1Affine other)
        {
            return this == other;
        }

        public override int GetHashCode()
        {
            if (Infinity) return Infinity.GetHashCode();
            return HashCode.Combine(X, Y);
        }

        public G1Projective ToProjective()
        {
            return new G1Projective(this);
        }

        public static G1Projective operator +(in G1Affine a, in G1Affine b)
        {
            return a.ToProjective() + b;
        }

        public static G1Projective operator -(in G1Affine a)
        {
            return new G1Projective(new G1Affine(a.X, -a.Y, a.Infinity));
        }

        public static G1Projective operator -(in G1Affine a, in G1Affine b)
        {
            return a + (-b);
        }

        public static G1Projective operator *(in G1Affine a, in Scalar b)
        {
            return a.ToProjective() * b;
        }

        public static G1Projective operator *(in Scalar a, in G1Affine b)
        {
            return b.ToProjective() * a;
        }

        private static bool Sqrt(in Fp a, out Fp result)
        {
            // Tonelli-Shanks algorithm for BN254
            // This is a simplified implementation
            result = a.PowVartime(new ulong[] {
                0x0f40231095ee3347,
                0x25e05a5a347a3c4b,
                0x2e14116b0a04d617,
                0x0c19139cb84c680a
            });
            
            return result.Square() == a;
        }

        public override string ToString()
        {
            if (Infinity) return "G1Affine(Infinity)";
            return $"G1Affine(x={X}, y={Y})";
        }
    }
}