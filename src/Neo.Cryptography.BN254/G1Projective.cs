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
            // Handle identity cases
            if (a.IsIdentity) return b;
            if (b.IsIdentity) return a;

            // Temporary implementation: convert to affine, add, convert back
            // This is slower but correct until we fix the projective formulas
            var aAffine = new G1Affine(a);
            var bAffine = new G1Affine(b);

            // Use affine addition (which works correctly)
            var result = aAffine + bAffine;

            // Convert back to projective
            return new G1Projective(result);
        }

        private static G1Projective AddMixed(in G1Projective a, in G1Affine b)
        {
            // Handle identity cases
            if (b.IsIdentity) return a;
            if (a.IsIdentity) return new G1Projective(b);

            // Temporary implementation: convert to affine, add, convert back
            // This is slower but correct until we fix the mixed addition formulas
            var aAffine = new G1Affine(a);

            // Use affine addition (which works correctly)
            var result = aAffine + b;

            // Convert back to projective
            return new G1Projective(result);
        }

        public G1Projective Double()
        {
            // Handle identity case
            if (IsIdentity) return this;

            // Temporary implementation: convert to affine, double, convert back
            // This is slower but correct until we fix the projective formulas
            var affine = new G1Affine(this);
            var doubled = affine.Double();

            return new G1Projective(doubled);
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
