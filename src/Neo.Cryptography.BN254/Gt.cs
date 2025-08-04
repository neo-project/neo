// Copyright (C) 2015-2025 The Neo Project.
//
// Gt.cs file belongs to the neo project and is free
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
    /// Element of the target group Gt (multiplicative group in Fp12)
    /// Fp12 is represented as a tower: Fp12 = Fp6[w] where Fp6 = Fp2[v]
    /// </summary>
    public readonly struct Gt : IEquatable<Gt>
    {
        // Fp12 = Fp6[w] = (c0 + c1*w) where c0, c1 are Fp6 elements
        // Fp6 = Fp2[v] = (c0 + c1*v + c2*v^2) where c0, c1, c2 are Fp2 elements
        public readonly Fp2 C0, C1, C2, C3, C4, C5; // 6 Fp2 elements = Fp12

        public const int Size = 384; // 12 * 32 bytes

        public Gt(Fp2 c0, Fp2 c1, Fp2 c2, Fp2 c3, Fp2 c4, Fp2 c5)
        {
            C0 = c0;
            C1 = c1;
            C2 = c2;
            C3 = c3;
            C4 = c4;
            C5 = c5;
        }

        public static ref readonly Gt Identity => ref identity;

        private static readonly Gt identity = new(Fp2.One, Fp2.Zero, Fp2.Zero, Fp2.Zero, Fp2.Zero, Fp2.Zero);

        public bool IsIdentity => this == Identity;

        public static Gt FromBytes(ReadOnlySpan<byte> bytes)
        {
            if (bytes.Length != Size)
                throw new ArgumentException($"Invalid data length {bytes.Length}, expected {Size}");

            // Deserialize 6 Fp2 elements
            var span = bytes;
            var c0 = Fp2.FromBytes(span.Slice(0, 64));
            var c1 = Fp2.FromBytes(span.Slice(64, 64));
            var c2 = Fp2.FromBytes(span.Slice(128, 64));
            var c3 = Fp2.FromBytes(span.Slice(192, 64));
            var c4 = Fp2.FromBytes(span.Slice(256, 64));
            var c5 = Fp2.FromBytes(span.Slice(320, 64));

            return new Gt(c0, c1, c2, c3, c4, c5);
        }

        public byte[] ToArray()
        {
            var result = new byte[Size];
            C0.ToArray().CopyTo(result, 0);
            C1.ToArray().CopyTo(result, 64);
            C2.ToArray().CopyTo(result, 128);
            C3.ToArray().CopyTo(result, 192);
            C4.ToArray().CopyTo(result, 256);
            C5.ToArray().CopyTo(result, 320);
            return result;
        }

        public static bool operator ==(in Gt a, in Gt b)
        {
            return a.C0 == b.C0 && a.C1 == b.C1 && a.C2 == b.C2 &&
                   a.C3 == b.C3 && a.C4 == b.C4 && a.C5 == b.C5;
        }

        public static bool operator !=(in Gt a, in Gt b)
        {
            return !(a == b);
        }

        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            if (obj is not Gt other) return false;
            return this == other;
        }

        public bool Equals(Gt other)
        {
            return this == other;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(C0, C1, C2, C3, C4, C5);
        }

        public static Gt operator +(in Gt a, in Gt b)
        {
            // Gt addition (component-wise Fp2 addition)
            return new Gt(a.C0 + b.C0, a.C1 + b.C1, a.C2 + b.C2, a.C3 + b.C3, a.C4 + b.C4, a.C5 + b.C5);
        }

        public static Gt operator -(in Gt a, in Gt b)
        {
            // Gt subtraction (component-wise Fp2 subtraction)
            return new Gt(a.C0 - b.C0, a.C1 - b.C1, a.C2 - b.C2, a.C3 - b.C3, a.C4 - b.C4, a.C5 - b.C5);
        }

        public static Gt operator -(in Gt a)
        {
            // Gt negation
            return new Gt(-a.C0, -a.C1, -a.C2, -a.C3, -a.C4, -a.C5);
        }

        public static Gt operator *(in Gt a, in Gt b)
        {
            // Fp12 multiplication using tower representation
            // Split into two Fp6 elements: a = a0 + a1*w, b = b0 + b1*w
            var a0 = new Fp6(a.C0, a.C1, a.C2);
            var a1 = new Fp6(a.C3, a.C4, a.C5);
            var b0 = new Fp6(b.C0, b.C1, b.C2);
            var b1 = new Fp6(b.C3, b.C4, b.C5);

            // Karatsuba multiplication: (a0 + a1*w) * (b0 + b1*w)
            var c0 = a0 * b0;
            var c1 = a1 * b1;
            var c2 = (a0 + a1) * (b0 + b1) - c0 - c1;

            // Apply Fp12 reduction: w^2 = gamma (non-residue in Fp6)
            var result0 = c0 + c1.MulByNonResidue();
            var result1 = c2;

            return new Gt(result0.C0, result0.C1, result0.C2, result1.C0, result1.C1, result1.C2);
        }

        public Gt Square()
        {
            // Optimized squaring for Fp12
            return this * this;
        }

        public Gt Conjugate()
        {
            // Frobenius conjugate: conjugate the w component
            return new Gt(C0, C1, C2, -C3, -C4, -C5);
        }

        public bool TryInvert(out Gt result)
        {
            // Fp12 inversion using conjugate method
            var conj = Conjugate();
            var norm = this * conj;

            // Extract norm as Fp6 element (should be in base field)
            var normFp6 = new Fp6(norm.C0, norm.C1, norm.C2);
            if (!normFp6.TryInvert(out var invNorm))
            {
                result = Identity;
                return false;
            }

            result = new Gt(
                conj.C0 * invNorm.C0, conj.C1 * invNorm.C1, conj.C2 * invNorm.C2,
                conj.C3 * invNorm.C0, conj.C4 * invNorm.C1, conj.C5 * invNorm.C2
            );
            return true;
        }

        public Gt FrobeniusMap(int power)
        {
            // Apply Frobenius endomorphism
            var c0 = C0.FrobeniusMap(power);
            var c1 = C1.FrobeniusMap(power);
            var c2 = C2.FrobeniusMap(power);
            var c3 = C3.FrobeniusMap(power);
            var c4 = C4.FrobeniusMap(power);
            var c5 = C5.FrobeniusMap(power);

            return new Gt(c0, c1, c2, c3, c4, c5);
        }

        public static Gt operator *(in Gt a, in Scalar b)
        {
            // Scalar exponentiation in Gt (multiplicative group)
            return a.Power(b.ToArray());
        }

        public static Gt operator *(in Scalar a, in Gt b)
        {
            return b * a;
        }

        public Gt Power(ReadOnlySpan<ulong> exponent)
        {
            // Binary exponentiation
            var result = Identity;
            var base_ = this;

            for (int i = 0; i < exponent.Length; i++)
            {
                var limb = exponent[i];
                for (int j = 0; j < 64; j++)
                {
                    if ((limb & (1UL << j)) != 0)
                    {
                        result = result * base_;
                    }
                    base_ = base_.Square();
                }
            }

            return result;
        }

        public Gt Power(ReadOnlySpan<byte> exponent)
        {
            // Convert byte array to ulong array and call main Power method
            var ulongArray = new ulong[4];
            for (int i = 0; i < Math.Min(exponent.Length, 32); i++)
            {
                int ulongIndex = i / 8;
                int byteOffset = i % 8;
                ulongArray[ulongIndex] |= (ulong)exponent[i] << (byteOffset * 8);
            }
            return Power(ulongArray);
        }

        public override string ToString()
        {
            return $"Gt(c0={C0}, c1={C1}, c2={C2}, c3={C3}, c4={C4}, c5={C5})";
        }

        /// <summary>
        /// Helper struct for Fp6 operations in tower construction
        /// </summary>
        private readonly struct Fp6
        {
            public readonly Fp2 C0, C1, C2;

            public Fp6(Fp2 c0, Fp2 c1, Fp2 c2)
            {
                C0 = c0;
                C1 = c1;
                C2 = c2;
            }

            public static Fp6 operator +(in Fp6 a, in Fp6 b)
            {
                return new Fp6(a.C0 + b.C0, a.C1 + b.C1, a.C2 + b.C2);
            }

            public static Fp6 operator -(in Fp6 a, in Fp6 b)
            {
                return new Fp6(a.C0 - b.C0, a.C1 - b.C1, a.C2 - b.C2);
            }

            public static Fp6 operator *(in Fp6 a, in Fp6 b)
            {
                // Fp6 multiplication with v^3 = xi (non-residue)
                var a0b0 = a.C0 * b.C0;
                var a1b1 = a.C1 * b.C1;
                var a2b2 = a.C2 * b.C2;

                var c0 = a0b0 + (a.C1 * b.C2 + a.C2 * b.C1).MulByNonResidue();
                var c1 = (a.C0 * b.C1 + a.C1 * b.C0) + a2b2.MulByNonResidue();
                var c2 = (a.C0 * b.C2 + a.C2 * b.C0) + a1b1;

                return new Fp6(c0, c1, c2);
            }

            public Fp6 MulByNonResidue()
            {
                // Multiply by non-residue xi = (1, 1) in Fp2
                var xi = new Fp2(Fp.One, Fp.One);
                return new Fp6(C2 * xi, C0, C1);
            }

            public bool TryInvert(out Fp6 result)
            {
                // Fp6 inversion using extended Euclidean algorithm
                var a = C0.Square() - (C1 * C2).MulByNonResidue();
                var b = C2.Square().MulByNonResidue() - C0 * C1;
                var c = C1.Square() - C0 * C2;

                var f = ((C1 * c + C2 * b).MulByNonResidue() + C0 * a);

                if (!f.TryInvert(out var f_inv))
                {
                    result = new Fp6(Fp2.Zero, Fp2.Zero, Fp2.Zero);
                    return false;
                }

                result = new Fp6(a * f_inv, b * f_inv, c * f_inv);
                return true;
            }
        }
    }
}
