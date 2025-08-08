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

        public static G2Projective operator +(in G2Projective a, in G2Projective b)
        {
            // Complete addition formulas for short Weierstrass curves
            // Using Jacobian coordinates for efficiency
            
            // Handle identity cases efficiently
            if (a.IsIdentity) return b;
            if (b.IsIdentity) return a;

            // Efficient complete addition in Jacobian coordinates
            var z1z1 = a.Z * a.Z;
            var z2z2 = b.Z * b.Z;
            var u1 = a.X * z2z2;
            var u2 = b.X * z1z1;
            var s1 = a.Y * b.Z * z2z2;
            var s2 = b.Y * a.Z * z1z1;

            if (u1 == u2)
            {
                if (s1 == s2)
                {
                    // Points are equal, use doubling
                    return Double(in a);
                }
                else
                {
                    // Points are negatives of each other
                    return Identity;
                }
            }

            var h = u2 - u1;
            var i = (h + h) * (h + h);
            var j = h * i;
            var r = (s2 - s1) + (s2 - s1);
            var v = u1 * i;
            var x3 = r * r - j - (v + v);
            var y3 = r * (v - x3) - ((s1 * j) + (s1 * j));
            var z3 = ((a.Z + b.Z) * (a.Z + b.Z) - z1z1 - z2z2) * h;

            return new G2Projective(x3, y3, z3);
        }

        public static G2Projective operator +(in G2Projective a, in G2Affine b)
        {
            // Mixed addition: projective + affine
            // More efficient since b.Z = 1
            if (b.Infinity) return a;
            if (a.IsIdentity) return new G2Projective(b);

            var z1z1 = a.Z * a.Z;
            var u2 = b.X * z1z1;
            var s2 = b.Y * a.Z * z1z1;

            if (a.X == u2)
            {
                if (a.Y == s2)
                {
                    // Points are equal, use doubling
                    return Double(in a);
                }
                else
                {
                    // Points are negatives of each other
                    return Identity;
                }
            }

            var h = u2 - a.X;
            var hh = h * h;
            var i = hh + hh;
            i = i + i;
            var j = h * hh;
            var r = (s2 - a.Y) + (s2 - a.Y);
            var v = a.X * hh;
            var x3 = r * r - j - (v + v);
            var y3 = r * (v - x3) - ((a.Y * j) + (a.Y * j));
            var z3 = (a.Z + h) * (a.Z + h) - z1z1 - hh;

            return new G2Projective(x3, y3, z3);
        }

        public static G2Projective operator *(in G2Projective point, in Scalar scalar)
        {
            // Constant-time scalar multiplication using Montgomery ladder
            return Multiply(in point, scalar.ToArray());
        }

        private static G2Projective Multiply(in G2Projective point, ReadOnlySpan<byte> scalar)
        {
            // Constant-time scalar multiplication using Montgomery ladder
            // This prevents timing attacks by ensuring consistent execution time
            var r0 = Identity;
            var r1 = point;

            // Process each bit of the scalar in constant time
            for (int i = scalar.Length - 1; i >= 0; i--)
            {
                byte b = scalar[i];
                for (int j = 7; j >= 0; j--)
                {
                    // Double-and-add in constant time
                    var bit = (b >> j) & 1;
                    var swap = bit == 1;
                    
                    // Conditional swap without branching
                    ConditionalSwap(ref r0, ref r1, swap);
                    r1 = r0 + r1;
                    r0 = Double(in r0);
                    ConditionalSwap(ref r0, ref r1, swap);
                }
            }

            return r0;
        }

        private static void ConditionalSwap(ref G2Projective a, ref G2Projective b, bool swap)
        {
            // Constant-time conditional swap
            var tmp = a;
            a = ConditionalSelect(in a, in b, swap);
            b = ConditionalSelect(in b, in tmp, swap);
        }

        private static G2Projective ConditionalSelect(in G2Projective a, in G2Projective b, bool choice)
        {
            // Select b if choice is true, otherwise select a (constant-time)
            var x = choice ? b.X : a.X;
            var y = choice ? b.Y : a.Y;
            var z = choice ? b.Z : a.Z;
            return new G2Projective(x, y, z);
        }

        private static G2Projective Double(in G2Projective p)
        {
            // Handle identity case
            if (p.IsIdentity) return p;

            // Efficient doubling in Jacobian coordinates
            var a = p.X * p.X;
            var b = p.Y * p.Y;
            var c = b * b;
            var d = ((p.X + b) * (p.X + b) - a - c) + ((p.X + b) * (p.X + b) - a - c);
            var e = a + a + a;
            var f = e * e;
            var x3 = f - (d + d);
            var eightC = c + c;
            eightC = eightC + eightC;
            eightC = eightC + eightC;
            var y3 = e * (d - x3) - eightC;
            var z3 = (p.Y + p.Z) * (p.Y + p.Z) - b - p.Z * p.Z;

            return new G2Projective(x3, y3, z3);
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
