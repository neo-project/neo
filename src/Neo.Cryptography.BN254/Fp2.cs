// Copyright (C) 2015-2025 The Neo Project.
//
// Fp2.cs file belongs to the neo project and is free
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
    /// Element of the quadratic extension field Fp2 = Fp[i] / (i^2 + 1)
    /// </summary>
    public readonly struct Fp2 : INumber<Fp2>
    {
        public readonly Fp C0;
        public readonly Fp C1;

        public const int Size = 64; // 2 * Fp size

        public Fp2(in Fp c0, in Fp c1)
        {
            C0 = c0;
            C1 = c1;
        }

        public static ref readonly Fp2 Zero => ref zero;
        public static ref readonly Fp2 One => ref one;

        private static readonly Fp2 zero = new(Fp.Zero, Fp.Zero);
        private static readonly Fp2 one = new(Fp.One, Fp.Zero);

        public static Fp2 FromBytes(ReadOnlySpan<byte> data)
        {
            if (data.Length != Size)
                throw new ArgumentException($"Invalid data length {data.Length}, expected {Size}");
            
            var c0 = Fp.FromBytes(data.Slice(0, 32));
            var c1 = Fp.FromBytes(data.Slice(32, 32));
            
            return new Fp2(c0, c1);
        }

        public byte[] ToArray()
        {
            var result = new byte[Size];
            C0.ToArray().CopyTo(result, 0);
            C1.ToArray().CopyTo(result, 32);
            return result;
        }

        public static bool operator ==(in Fp2 a, in Fp2 b)
        {
            return a.C0 == b.C0 & a.C1 == b.C1;
        }

        public static bool operator !=(in Fp2 a, in Fp2 b)
        {
            return !(a == b);
        }

        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            if (obj is not Fp2 other) return false;
            return this == other;
        }

        public bool Equals(Fp2 other)
        {
            return this == other;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(C0, C1);
        }

        public static Fp2 operator +(in Fp2 a, in Fp2 b)
        {
            return new Fp2(a.C0 + b.C0, a.C1 + b.C1);
        }

        public static Fp2 operator -(in Fp2 a, in Fp2 b)
        {
            return new Fp2(a.C0 - b.C0, a.C1 - b.C1);
        }

        public static Fp2 operator *(in Fp2 a, in Fp2 b)
        {
            // (a0 + a1*i) * (b0 + b1*i) = (a0*b0 - a1*b1) + (a0*b1 + a1*b0)*i
            var c0 = a.C0 * b.C0 - a.C1 * b.C1;
            var c1 = a.C0 * b.C1 + a.C1 * b.C0;
            return new Fp2(c0, c1);
        }

        public static Fp2 operator -(in Fp2 a)
        {
            return new Fp2(-a.C0, -a.C1);
        }

        public Fp2 Square()
        {
            // (a0 + a1*i)^2 = (a0^2 - a1^2) + 2*a0*a1*i
            var c0 = C0.Square() - C1.Square();
            var c1 = C0 * C1;
            c1 = c1 + c1;
            return new Fp2(c0, c1);
        }

        public bool TryInvert(out Fp2 result)
        {
            // 1/(a0 + a1*i) = (a0 - a1*i) / (a0^2 + a1^2)
            var norm = C0.Square() + C1.Square();
            if (!norm.TryInvert(out var inv))
            {
                result = Zero;
                return false;
            }
            
            result = new Fp2(C0 * inv, -C1 * inv);
            return true;
        }

        public bool IsZero => C0.IsZero & C1.IsZero;
        public bool IsOne => C0.IsOne & C1.IsZero;

        public override string ToString()
        {
            return $"Fp2({C0}, {C1})";
        }
    }
}