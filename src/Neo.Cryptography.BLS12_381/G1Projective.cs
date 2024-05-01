// Copyright (C) 2015-2024 The Neo Project.
//
// G1Projective.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using static Neo.Cryptography.BLS12_381.Constants;
using static Neo.Cryptography.BLS12_381.ConstantTimeUtility;
using static Neo.Cryptography.BLS12_381.G1Constants;

namespace Neo.Cryptography.BLS12_381;

[StructLayout(LayoutKind.Explicit, Size = Fp.Size * 3)]
public readonly struct G1Projective : IEquatable<G1Projective>
{
    [FieldOffset(0)]
    public readonly Fp X;
    [FieldOffset(Fp.Size)]
    public readonly Fp Y;
    [FieldOffset(Fp.Size * 2)]
    public readonly Fp Z;

    public static readonly G1Projective Identity = new(in Fp.Zero, in Fp.One, in Fp.Zero);
    public static readonly G1Projective Generator = new(in GeneratorX, in GeneratorY, in Fp.One);

    public bool IsIdentity => Z.IsZero;
    public bool IsOnCurve => ((Y.Square() * Z) == (X.Square() * X + Z.Square() * Z * B)) | Z.IsZero;

    public G1Projective(in Fp x, in Fp y, in Fp z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public G1Projective(in G1Affine p)
        : this(in p.X, in p.Y, ConditionalSelect(in Fp.One, in Fp.Zero, p.Infinity))
    {
    }

    public static bool operator ==(in G1Projective a, in G1Projective b)
    {
        // Is (xz, yz, z) equal to (x'z', y'z', z') when converted to affine?

        var x1 = a.X * b.Z;
        var x2 = b.X * a.Z;

        var y1 = a.Y * b.Z;
        var y2 = b.Y * a.Z;

        var self_is_zero = a.Z.IsZero;
        var other_is_zero = b.Z.IsZero;

        // Both point at infinity. Or neither point at infinity, coordinates are the same.
        return (self_is_zero & other_is_zero) | ((!self_is_zero) & (!other_is_zero) & x1 == x2 & y1 == y2);
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
        return X.GetHashCode() ^ Y.GetHashCode() ^ Z.GetHashCode();
    }

    public static G1Projective operator -(in G1Projective p)
    {
        return new G1Projective(in p.X, -p.Y, in p.Z);
    }

    private static Fp MulBy3B(in Fp a)
    {
        var b = a + a;
        b += b;
        return b + b + b;
    }

    public G1Projective Double()
    {
        var t0 = Y.Square();
        var z3 = t0 + t0;
        z3 += z3;
        z3 += z3;
        var t1 = Y * Z;
        var t2 = Z.Square();
        t2 = MulBy3B(in t2);
        var x3 = t2 * z3;
        var y3 = t0 + t2;
        z3 = t1 * z3;
        t1 = t2 + t2;
        t2 = t1 + t2;
        t0 -= t2;
        y3 = t0 * y3;
        y3 = x3 + y3;
        t1 = X * Y;
        x3 = t0 * t1;
        x3 += x3;

        G1Projective tmp = new(in x3, in y3, in z3);
        return ConditionalSelect(in tmp, in Identity, IsIdentity);
    }

    public static G1Projective operator +(in G1Projective a, in G1Projective b)
    {
        var t0 = a.X * b.X;
        var t1 = a.Y * b.Y;
        var t2 = a.Z * b.Z;
        var t3 = a.X + a.Y;
        var t4 = b.X + b.Y;
        t3 *= t4;
        t4 = t0 + t1;
        t3 -= t4;
        t4 = a.Y + a.Z;
        var x3 = b.Y + b.Z;
        t4 *= x3;
        x3 = t1 + t2;
        t4 -= x3;
        x3 = a.X + a.Z;
        var y3 = b.X + b.Z;
        x3 *= y3;
        y3 = t0 + t2;
        y3 = x3 - y3;
        x3 = t0 + t0;
        t0 = x3 + t0;
        t2 = MulBy3B(in t2);
        var z3 = t1 + t2;
        t1 -= t2;
        y3 = MulBy3B(in y3);
        x3 = t4 * y3;
        t2 = t3 * t1;
        x3 = t2 - x3;
        y3 *= t0;
        t1 *= z3;
        y3 = t1 + y3;
        t0 *= t3;
        z3 *= t4;
        z3 += t0;

        return new G1Projective(in x3, in y3, in z3);
    }

    public static G1Projective operator +(in G1Projective a, in G1Affine b)
    {
        var t0 = a.X * b.X;
        var t1 = a.Y * b.Y;
        var t3 = b.X + b.Y;
        var t4 = a.X + a.Y;
        t3 *= t4;
        t4 = t0 + t1;
        t3 -= t4;
        t4 = b.Y * a.Z;
        t4 += a.Y;
        var y3 = b.X * a.Z;
        y3 += a.X;
        var x3 = t0 + t0;
        t0 = x3 + t0;
        var t2 = MulBy3B(in a.Z);
        var z3 = t1 + t2;
        t1 -= t2;
        y3 = MulBy3B(in y3);
        x3 = t4 * y3;
        t2 = t3 * t1;
        x3 = t2 - x3;
        y3 *= t0;
        t1 *= z3;
        y3 = t1 + y3;
        t0 *= t3;
        z3 *= t4;
        z3 += t0;

        G1Projective tmp = new(in x3, in y3, in z3);
        return ConditionalSelect(in tmp, in a, b.IsIdentity);
    }

    public static G1Projective operator +(in G1Affine a, in G1Projective b)
    {
        return b + a;
    }

    public static G1Projective operator -(in G1Projective a, in G1Projective b)
    {
        return a + -b;
    }

    public static G1Projective operator -(in G1Projective a, in G1Affine b)
    {
        return a + -b;
    }

    public static G1Projective operator -(in G1Affine a, in G1Projective b)
    {
        return -b + a;
    }

    public static G1Projective operator *(in G1Projective a, byte[] b)
    {
        var length = b.Length;
        if (length != 32)
            throw new ArgumentException($"The argument {nameof(b)} must be 32 bytes.");

        var acc = Identity;

        foreach (var bit in b
            .SelectMany(p => Enumerable.Range(0, 8).Select(q => ((p >> q) & 1) == 1))
            .Reverse()
            .Skip(1))
        {
            acc = acc.Double();
            acc = ConditionalSelect(in acc, acc + a, bit);
        }

        return acc;
    }

    public static G1Projective operator *(in G1Projective a, in Scalar b)
    {
        return a * b.ToArray();
    }

    internal G1Projective MulByX()
    {
        var xself = Identity;

        var x = BLS_X >> 1;
        var tmp = this;
        while (x > 0)
        {
            tmp = tmp.Double();

            if (x % 2 == 1)
            {
                xself += tmp;
            }
            x >>= 1;
        }

        if (BLS_X_IS_NEGATIVE)
        {
            xself = -xself;
        }
        return xself;
    }

    public G1Projective ClearCofactor()
    {
        return this - MulByX();
    }

    public static void BatchNormalize(ReadOnlySpan<G1Projective> p, Span<G1Affine> q)
    {
        var length = p.Length;
        if (length != q.Length)
            throw new ArgumentException($"{nameof(p)} and {nameof(q)} must have the same length.");

        Span<Fp> x = stackalloc Fp[length];
        var acc = Fp.One;
        for (var i = 0; i < length; i++)
        {
            x[i] = acc;
            acc = ConditionalSelect(acc * p[i].Z, in acc, p[i].IsIdentity);
        }

        acc = acc.Invert();

        for (var i = length - 1; i >= 0; i--)
        {
            var skip = p[i].IsIdentity;
            var tmp = x[i] * acc;
            acc = ConditionalSelect(acc * p[i].Z, in acc, skip);
            G1Affine qi = new(p[i].X * tmp, p[i].Y * tmp);
            q[i] = ConditionalSelect(in qi, in G1Affine.Identity, skip);
        }
    }
}
