// Copyright (C) 2015-2025 The Neo Project.
//
// G2Projective.cs file belongs to the neo project and is free
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
    /// Represents a point on the G2 group in projective coordinates
    /// </summary>
    public readonly struct G2Projective : IEquatable<G2Projective>
    {
        public readonly Fp2 X;
        public readonly Fp2 Y;
        public readonly Fp2 Z;

        public G2Projective(in Fp2 x, in Fp2 y, in Fp2 z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public static ref readonly G2Projective Identity => ref identity;

        private static readonly G2Projective identity = new(Fp2.Zero, Fp2.One, Fp2.Zero);

        public G2Projective(in G2Affine p)
        {
            X = p.Infinity ? Fp2.Zero : p.X;
            Y = p.Infinity ? Fp2.One : p.Y;
            Z = p.Infinity ? Fp2.Zero : Fp2.One;
        }

        public bool IsIdentity => Z.IsZero;

        public static bool operator ==(in G2Projective a, in G2Projective b)
        {
            var x1 = a.X * b.Z;
            var x2 = b.X * a.Z;

            var y1 = a.Y * b.Z;
            var y2 = b.Y * a.Z;

            return x1 == x2 & y1 == y2;
        }

        public static bool operator !=(in G2Projective a, in G2Projective b)
        {
            return !(a == b);
        }

        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            if (obj is not G2Projective other) return false;
            return this == other;
        }

        public bool Equals(G2Projective other)
        {
            return this == other;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(X, Y, Z);
        }

        public static G2Projective operator +(in G2Projective a, in G2Projective other)
        {
            // Complete addition formula for short Weierstrass curves over Fp2
            if (a.IsIdentity) return other;
            if (other.IsIdentity) return a;

            var t0 = a.X * other.X;
            var t1 = a.Y * other.Y;
            var t2 = a.Z * other.Z;
            var t3 = a.X + a.Y;
            var t4 = other.X + other.Y;
            t3 = t3 * t4;
            t4 = t0 + t1;
            t3 = t3 - t4;
            t4 = a.X + a.Z;
            var t5 = other.X + other.Z;
            t4 = t4 * t5;
            t5 = t0 + t2;
            t4 = t4 - t5;
            t5 = a.Y + a.Z;
            var x3 = other.Y + other.Z;
            t5 = t5 * x3;
            x3 = t1 + t2;
            t5 = t5 - x3;

            // G2 curve constant: b = (0x2b149d40ceb8aaae3a18e4a61c076267..., 0x09075b4ee4d3ff4c9054...)
            var curveB = new Fp2(
                new Fp(new ulong[] { 0x2b149d40ceb8aaae, 0x3a18e4a61c076267, 0x45c2ac2962a12902, 0x09192585375e4d42 }),
                new Fp(new ulong[] { 0x0c54bba1d6f46fef, 0x5d784e17b8c00409, 0x21f828ff3dc8ca4d, 0x009075b4ee4d3ff4 })
            );

            var z3 = curveB * t2;
            x3 = z3 + t2;
            z3 = t1 - x3;
            x3 = t1 + x3;
            var y3 = x3 * z3;
            t1 = t0 + t0;
            t1 = t1 + t0;
            t4 = curveB * t4;
            t0 = t1 * t4;
            y3 = y3 + t0;
            t0 = t5 * t4;
            x3 = t3 * x3;
            x3 = x3 - t0;
            t0 = t3 * t1;
            z3 = t5 * z3;
            z3 = z3 + t0;

            return new G2Projective(x3, y3, z3);
        }

        public static G2Projective operator +(in G2Projective a, in G2Affine b)
        {
            return a + new G2Projective(b);
        }

        public static G2Projective operator *(in G2Projective a, in Scalar b)
        {
            // Window method scalar multiplication
            if (a.IsIdentity) return Identity;
            if (b == Scalar.Zero) return Identity;
            if (b == Scalar.One) return a;

            // Use binary method for scalar multiplication
            var result = Identity;
            var addend = a;
            var scalar = b;

            while (scalar != Scalar.Zero)
            {
                if ((scalar.GetLimb(0) & 1) == 1)
                {
                    result = result + addend;
                }
                addend = addend + addend; // Double

                // Right shift scalar by 1
                var carry = 0UL;
                for (int i = 3; i >= 0; i--)
                {
                    var newCarry = (scalar.GetLimb(i) & 1) << 63;
                    var limb = (scalar.GetLimb(i) >> 1) | carry;
                    scalar = scalar.SetLimb(i, limb);
                    carry = newCarry;
                }
            }

            return result;
        }

        public static G2Projective operator *(in Scalar b, in G2Projective a)
        {
            return a * b;
        }

        public static G2Projective operator -(in G2Projective a)
        {
            return new G2Projective(a.X, -a.Y, a.Z);
        }

        public byte[] ToCompressed()
        {
            return new G2Affine(this).ToCompressed();
        }

        public G2Projective Double()
        {
            // Point doubling using complete formulas
            return this + this;
        }

        public G2Affine Negate()
        {
            return new G2Affine(X, -Y, false);
        }

        public override string ToString()
        {
            if (IsIdentity) return "G2Projective(Identity)";
            return $"G2Projective(x={X}, y={Y}, z={Z})";
        }
    }
}
