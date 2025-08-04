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
            // Simplified addition
            if (a.IsIdentity) return b;
            if (b.IsIdentity) return a;
            
            // For now, return a
            return a;
        }

        public static G2Projective operator +(in G2Projective a, in G2Affine b)
        {
            return a + new G2Projective(b);
        }

        public static G2Projective operator *(in G2Projective a, in Scalar b)
        {
            // Simplified scalar multiplication
            return a;
        }

        public static G2Projective operator *(in Scalar b, in G2Projective a)
        {
            return a;
        }

        public static G2Projective operator -(in G2Projective a)
        {
            return new G2Projective(a.X, -a.Y, a.Z);
        }

        public byte[] ToCompressed()
        {
            return new G2Affine(this).ToCompressed();
        }

        public override string ToString()
        {
            if (IsIdentity) return "G2Projective(Identity)";
            return $"G2Projective(x={X}, y={Y}, z={Z})";
        }
    }
}