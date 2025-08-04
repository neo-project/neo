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
            X = ConditionalSelect(p.X, Fp.Zero, p.Infinity);
            Y = ConditionalSelect(p.Y, Fp.One, p.Infinity);
            Z = ConditionalSelect(Fp.One, Fp.Zero, p.Infinity);
        }

        public bool IsIdentity => Z.IsZero;

        public bool IsOnCurve()
        {
            // Y² Z = X³ + b Z³
            var y2 = Y.Square();
            var x3 = X.Square() * X;
            var z3 = Z.Square() * Z;
            var bz3 = new Fp(3, 0, 0, 0) * z3;

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
            // Complete addition formula for short Weierstrass curves
            // From "Complete addition formulas for prime order elliptic curves" by Renes-Costello-Batina
            var t0 = a.X * b.X;
            var t1 = a.Y * b.Y;
            var t2 = a.Z * b.Z;
            var t3 = a.X + a.Y;
            var t4 = b.X + b.Y;
            t3 = t3 * t4;
            t4 = t0 + t1;
            t3 = t3 - t4;
            t4 = a.X + a.Z;
            var t5 = b.X + b.Z;
            t4 = t4 * t5;
            t5 = t0 + t2;
            t4 = t4 - t5;
            t5 = a.Y + a.Z;
            var x3 = b.Y + b.Z;
            t5 = t5 * x3;
            x3 = t1 + t2;
            t5 = t5 - x3;
            var z3 = new Fp(3, 0, 0, 0) * t2;
            x3 = z3 + t2;
            z3 = t1 - x3;
            x3 = t1 + x3;
            var y3 = x3 * z3;
            t1 = t0 + t0;
            t1 = t1 + t0;
            t4 = new Fp(3, 0, 0, 0) * t4;
            t0 = t1 * t4;
            y3 = y3 + t0;
            t0 = t5 * t4;
            x3 = t3 * x3;
            x3 = x3 - t0;
            t0 = t3 * t1;
            z3 = t5 * z3;
            z3 = z3 + t0;

            return new G1Projective(x3, y3, z3);
        }

        private static G1Projective AddMixed(in G1Projective a, in G1Affine b)
        {
            // Mixed addition formula
            if (b.IsIdentity) return a;
            if (a.IsIdentity) return new G1Projective(b);

            var t0 = a.X * b.X;
            var t1 = a.Y * b.Y;
            var t3 = b.X + b.Y;
            var t4 = a.X + a.Y;
            t3 = t3 * t4;
            t4 = t0 + t1;
            t3 = t3 - t4;
            t4 = b.X * a.Z;
            t4 = t4 + a.X;
            var t5 = b.Y * a.Z;
            t5 = t5 + a.Y;
            var z3 = new Fp(3, 0, 0, 0) * a.Z;
            var x3 = z3 + a.Z;
            z3 = t1 - x3;
            x3 = t1 + x3;
            var y3 = x3 * z3;
            t1 = t0 + t0;
            t1 = t1 + t0;
            t4 = new Fp(3, 0, 0, 0) * t4;
            t0 = t1 * t4;
            y3 = y3 + t0;
            t0 = t5 * t4;
            x3 = t3 * x3;
            x3 = x3 - t0;
            t0 = t3 * t1;
            z3 = t5 * z3;
            z3 = z3 + t0;

            return new G1Projective(x3, y3, z3);
        }

        public G1Projective Double()
        {
            // Point doubling formula
            var t0 = Y.Square();
            var z3 = t0 + t0;
            z3 = z3 + z3;
            z3 = z3 + z3;
            var t1 = Y * Z;
            var t2 = Z.Square();
            t2 = new Fp(3, 0, 0, 0) * t2;
            var x3 = t2 * z3;
            var y3 = t0 + t2;
            z3 = t1 * z3;
            t1 = t2 + t2;
            t2 = t1 + t2;
            t0 = t0 - t2;
            y3 = t0 * y3;
            y3 = x3 + y3;
            t1 = X * Y;
            x3 = t0 * t1;
            x3 = x3 + x3;

            return new G1Projective(x3, y3, z3);
        }

        private static G1Projective Multiply(in G1Projective point, ReadOnlySpan<byte> scalar)
        {
            var acc = Identity;
            var base_ = point;

            // Binary scalar multiplication algorithm
            foreach (byte b in scalar)
            {
                for (int i = 0; i < 8; i++)
                {
                    if ((b & (1 << i)) != 0)
                    {
                        acc = acc + base_;
                    }
                    base_ = base_.Double();
                }
            }

            return acc;
        }

        public override string ToString()
        {
            if (IsIdentity) return "G1Projective(Identity)";
            return $"G1Projective(x={X}, y={Y}, z={Z})";
        }
    }
}
