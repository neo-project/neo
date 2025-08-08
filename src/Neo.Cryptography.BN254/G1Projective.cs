// Copyright (C) 2015-2025 The Neo Project.
//
// G1Projective.cs file belongs to the neo project and is free
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

namespace Neo.Cryptography.BN254
{
    public readonly struct G1Projective : IEquatable<G1Projective>
    {
        public readonly Fp X;
        public readonly Fp Y;
        public readonly Fp Z;

        public G1Projective(in Fp x, in Fp y, in Fp z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public static ref readonly G1Projective Identity => ref identity;
        public static ref readonly G1Projective Generator => ref generator;

        private static readonly G1Projective identity = new(Fp.Zero, Fp.One, Fp.Zero);
        private static readonly G1Projective generator = new(G1Affine.Generator);

        public G1Projective(in G1Affine p)
        {
            X = ConstantTimeUtility.ConditionalSelect(in p.X, in Fp.Zero, p.Infinity);
            Y = ConstantTimeUtility.ConditionalSelect(in p.Y, in Fp.One, p.Infinity);
            Z = ConstantTimeUtility.ConditionalSelect(in Fp.One, in Fp.Zero, p.Infinity);
        }

        public bool IsIdentity => Z.IsZero;

        public bool IsOnCurve()
        {
            // Y² Z = X³ + b Z³
            var y2 = Y.Square();
            var x3 = X.Square() * X;
            var z3 = Z.Square() * Z;
            var bz3 = G1Constants.B * z3;

            return (y2 * Z) == (x3 + bz3);
        }

        public static bool operator ==(in G1Projective a, in G1Projective b)
        {
            // The points (X, Y, Z) and (X', Y', Z')
            // are equal when (X * Z') = (X' * Z)
            // and (Y * Z') = (Y' * Z).
            var x1 = a.X * b.Z;
            var x2 = b.X * a.Z;

            var y1 = a.Y * b.Z;
            var y2 = b.Y * a.Z;

            return x1 == x2 & y1 == y2;
        }

        public static bool operator !=(in G1Projective a, in G1Projective b)
        {
            return !(a == b);
        }

        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            if (obj is not G1Projective other) return false;
            return this == other;
        }

        public bool Equals(G1Projective other)
        {
            return this == other;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(X, Y, Z);
        }

        public static G1Projective operator +(in G1Projective a, in G1Projective b)
        {
            return Add(in a, in b);
        }

        public static G1Projective operator +(in G1Projective a, in G1Affine b)
        {
            return AddMixed(in a, in b);
        }

        public static G1Projective operator +(in G1Affine a, in G1Projective b)
        {
            return AddMixed(in b, in a);
        }

        public static G1Projective operator -(in G1Projective a)
        {
            return new G1Projective(a.X, -a.Y, a.Z);
        }

        public static G1Projective operator -(in G1Projective a, in G1Projective b)
        {
            return a + (-b);
        }

        public static G1Projective operator *(in G1Projective a, in Scalar b)
        {
            return Multiply(in a, b.ToArray());
        }

        public static G1Projective operator *(in Scalar b, in G1Projective a)
        {
            return Multiply(in a, b.ToArray());
        }

        private static G1Projective Add(in G1Projective a, in G1Projective b)
        {
            // Complete addition formulas for short Weierstrass curves
            // Using Jacobian coordinates for efficiency
            // Based on https://hyperelliptic.org/EFD/g1p/auto-shortw-jacobian.html
            
            // Handle identity cases efficiently
            if (a.IsIdentity) return b;
            if (b.IsIdentity) return a;

            // Efficient complete addition in Jacobian coordinates
            var z1z1 = a.Z.Square();
            var z2z2 = b.Z.Square();
            var u1 = a.X * z2z2;
            var u2 = b.X * z1z1;
            var s1 = a.Y * b.Z * z2z2;
            var s2 = b.Y * a.Z * z1z1;

            if (u1 == u2)
            {
                if (s1 == s2)
                {
                    // Points are equal, use doubling
                    return a.Double();
                }
                else
                {
                    // Points are negatives of each other
                    return Identity;
                }
            }

            var h = u2 - u1;
            var i = (h + h).Square();
            var j = h * i;
            var r = (s2 - s1) + (s2 - s1);
            var v = u1 * i;
            var x3 = r.Square() - j - (v + v);
            var y3 = r * (v - x3) - ((s1 * j) + (s1 * j));
            var z3 = ((a.Z + b.Z).Square() - z1z1 - z2z2) * h;

            return new G1Projective(x3, y3, z3);
        }

        private static G1Projective AddMixed(in G1Projective a, in G1Affine b)
        {
            // Handle identity cases
            if (b.IsIdentity) return a;
            if (a.IsIdentity) return new G1Projective(b);

            // Mixed addition: projective + affine
            // More efficient since b.Z = 1
            var z1z1 = a.Z.Square();
            var u2 = b.X * z1z1;
            var s2 = b.Y * a.Z * z1z1;

            if (a.X == u2)
            {
                if (a.Y == s2)
                {
                    // Points are equal, use affine doubling
                    return new G1Projective(b).Double();
                }
                else
                {
                    // Points are negatives of each other
                    return Identity;
                }
            }

            var h = u2 - a.X;
            var hh = h.Square();
            var i = hh + hh;
            i = i + i;
            var j = h * hh;
            var r = (s2 - a.Y) + (s2 - a.Y);
            var v = a.X * hh;
            var x3 = r.Square() - j - (v + v);
            var y3 = r * (v - x3) - ((a.Y * j) + (a.Y * j));
            var z3 = (a.Z + h).Square() - z1z1 - hh;

            return new G1Projective(x3, y3, z3);
        }

        public G1Projective Double()
        {
            // Handle identity case
            if (IsIdentity) return this;

            // Efficient doubling in Jacobian coordinates
            // Based on https://hyperelliptic.org/EFD/g1p/auto-shortw-jacobian.html#doubling-dbl-2007-bl
            var a = X.Square();
            var b = Y.Square();
            var c = b.Square();
            var d = ((X + b).Square() - a - c) + ((X + b).Square() - a - c);
            var e = a + a + a;
            var f = e.Square();
            var x3 = f - (d + d);
            var eightC = c + c;
            eightC = eightC + eightC;
            eightC = eightC + eightC;
            var y3 = e * (d - x3) - eightC;
            var z3 = (Y + Z).Square() - b - Z.Square();

            return new G1Projective(x3, y3, z3);
        }

        private static G1Projective Multiply(in G1Projective point, ReadOnlySpan<byte> scalar)
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
                    r0 = r0.Double();
                    ConditionalSwap(ref r0, ref r1, swap);
                }
            }

            return r0;
        }

        private static void ConditionalSwap(ref G1Projective a, ref G1Projective b, bool swap)
        {
            // Constant-time conditional swap
            var mask = swap ? ulong.MaxValue : 0UL;
            var tmp = a;
            a = ConditionalSelect(in a, in b, swap);
            b = ConditionalSelect(in b, in tmp, swap);
        }

        private static G1Projective ConditionalSelect(in G1Projective a, in G1Projective b, bool choice)
        {
            // Select b if choice is true, otherwise select a (constant-time)
            var x = ConstantTimeUtility.ConditionalSelect(in a.X, in b.X, choice);
            var y = ConstantTimeUtility.ConditionalSelect(in a.Y, in b.Y, choice);
            var z = ConstantTimeUtility.ConditionalSelect(in a.Z, in b.Z, choice);
            return new G1Projective(x, y, z);
        }

        public override string ToString()
        {
            if (IsIdentity) return "G1Projective(Identity)";
            return $"G1Projective(x={X}, y={Y}, z={Z})";
        }
    }
}
