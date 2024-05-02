// Copyright (C) 2015-2024 The Neo Project.
//
// Fp2.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using static Neo.Cryptography.BLS12_381.ConstantTimeUtility;

namespace Neo.Cryptography.BLS12_381;

[StructLayout(LayoutKind.Explicit, Size = Size)]
public readonly struct Fp2 : IEquatable<Fp2>, INumber<Fp2>
{
    [FieldOffset(0)]
    public readonly Fp C0;
    [FieldOffset(Fp.Size)]
    public readonly Fp C1;

    public const int Size = Fp.Size * 2;

    private static readonly Fp2 _zero = new();
    private static readonly Fp2 _one = new(in Fp.One);

    public static ref readonly Fp2 Zero => ref _zero;
    public static ref readonly Fp2 One => ref _one;

    public bool IsZero => C0.IsZero & C1.IsZero;

    public Fp2(in Fp f)
        : this(in f, in Fp.Zero)
    {
    }

    public Fp2(in Fp c0, in Fp c1)
    {
        C0 = c0;
        C1 = c1;
    }

    public static Fp2 FromBytes(ReadOnlySpan<byte> data)
    {
        if (data.Length != Size)
            throw new FormatException($"The argument `{nameof(data)}` must contain {Size} bytes.");
        var c0 = Fp.FromBytes(data[Fp.Size..]);
        var c1 = Fp.FromBytes(data[..Fp.Size]);
        return new(in c0, in c1);
    }

    public static Fp2 Random(RandomNumberGenerator rng)
    {
        return new(Fp.Random(rng), Fp.Random(rng));
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
        return C0.GetHashCode() ^ C1.GetHashCode();
    }

    public byte[] ToArray()
    {
        var result = new byte[Size];
        TryWrite(result);
        return result;
    }

    public bool TryWrite(Span<byte> buffer)
    {
        if (buffer.Length < Size) return false;
        C0.TryWrite(buffer[Fp.Size..Size]);
        C1.TryWrite(buffer[0..Fp.Size]);
        return true;
    }

    public Fp2 FrobeniusMap()
    {
        // This is always just a conjugation. If you're curious why, here's
        // an article about it: https://alicebob.cryptoland.net/the-frobenius-endomorphism-with-finite-fields/
        return Conjugate();
    }

    public Fp2 Conjugate()
    {
        return new(in C0, -C1);
    }

    public Fp2 MulByNonresidue()
    {
        // Multiply a + bu by u + 1, getting
        // au + a + bu^2 + bu
        // and because u^2 = -1, we get
        // (a - b) + (a + b)u

        return new(C0 - C1, C0 + C1);
    }

    public bool LexicographicallyLargest()
    {
        // If this element's c1 coefficient is lexicographically largest
        // then it is lexicographically largest. Otherwise, in the event
        // the c1 coefficient is zero and the c0 coefficient is
        // lexicographically largest, then this element is lexicographically
        // largest.

        return C1.LexicographicallyLargest() | (C1.IsZero & C0.LexicographicallyLargest());
    }

    public Fp2 Square()
    {
        // Complex squaring:
        //
        // v0  = c0 * c1
        // c0' = (c0 + c1) * (c0 + \beta*c1) - v0 - \beta * v0
        // c1' = 2 * v0
        //
        // In BLS12-381's F_{p^2}, our \beta is -1 so we
        // can modify this formula:
        //
        // c0' = (c0 + c1) * (c0 - c1)
        // c1' = 2 * c0 * c1

        var a = C0 + C1;
        var b = C0 - C1;
        var c = C0 + C0;

        return new(a * b, c * C1);
    }

    public static Fp2 operator *(in Fp2 a, in Fp2 b)
    {
        // F_{p^2} x F_{p^2} multiplication implemented with operand scanning (schoolbook)
        // computes the result as:
        //
        //   a·b = (a_0 b_0 + a_1 b_1 β) + (a_0 b_1 + a_1 b_0)i
        //
        // In BLS12-381's F_{p^2}, our β is -1, so the resulting F_{p^2} element is:
        //
        //   c_0 = a_0 b_0 - a_1 b_1
        //   c_1 = a_0 b_1 + a_1 b_0
        //
        // Each of these is a "sum of products", which we can compute efficiently.

        return new(
            Fp.SumOfProducts([a.C0, -a.C1], [b.C0, b.C1]),
            Fp.SumOfProducts([a.C0, a.C1], [b.C1, b.C0])
        );
    }

    public static Fp2 operator +(in Fp2 a, in Fp2 b)
    {
        return new(a.C0 + b.C0, a.C1 + b.C1);
    }

    public static Fp2 operator -(in Fp2 a, in Fp2 b)
    {
        return new(a.C0 - b.C0, a.C1 - b.C1);
    }

    public static Fp2 operator -(in Fp2 a)
    {
        return new(-a.C0, -a.C1);
    }

    public Fp2 Sqrt()
    {
        // Algorithm 9, https://eprint.iacr.org/2012/685.pdf
        // with constant time modifications.

        // a1 = self^((p - 3) / 4)
        var a1 = this.PowVartime(
        [
            0xee7f_bfff_ffff_eaaa,
            0x07aa_ffff_ac54_ffff,
            0xd9cc_34a8_3dac_3d89,
            0xd91d_d2e1_3ce1_44af,
            0x92c6_e9ed_90d2_eb35,
            0x0680_447a_8e5f_f9a6
        ]);

        // alpha = a1^2 * self = self^((p - 3) / 2 + 1) = self^((p - 1) / 2)
        var alpha = a1.Square() * this;

        // x0 = self^((p + 1) / 4)
        var x0 = a1 * this;

        // (1 + alpha)^((q - 1) // 2) * x0
        var sqrt = (alpha + One).PowVartime([
            0xdcff_7fff_ffff_d555,
            0x0f55_ffff_58a9_ffff,
            0xb398_6950_7b58_7b12,
            0xb23b_a5c2_79c2_895f,
            0x258d_d3db_21a5_d66b,
            0x0d00_88f5_1cbf_f34d,
        ]) * x0;

        // In the event that alpha = -1, the element is order p - 1 and so
        // we're just trying to get the square of an element of the subfield
        // Fp. This is given by x0 * u, since u = sqrt(-1). Since the element
        // x0 = a + bu has b = 0, the solution is therefore au.
        sqrt = ConditionalSelect(in sqrt, new(-x0.C1, in x0.C0), alpha == -One);

        sqrt = ConditionalSelect(in sqrt, in Zero, IsZero);

        // Only return the result if it's really the square root (and so
        // self is actually quadratic nonresidue)
        if (sqrt.Square() != this) throw new ArithmeticException();
        return sqrt;
    }

    public Fp2 Invert()
    {
        if (!TryInvert(out var result))
            throw new DivideByZeroException();
        return result;
    }

    public bool TryInvert(out Fp2 result)
    {
        // We wish to find the multiplicative inverse of a nonzero
        // element a + bu in Fp2. We leverage an identity
        //
        // (a + bu)(a - bu) = a^2 + b^2
        //
        // which holds because u^2 = -1. This can be rewritten as
        //
        // (a + bu)(a - bu)/(a^2 + b^2) = 1
        //
        // because a^2 + b^2 = 0 has no nonzero solutions for (a, b).
        // This gives that (a - bu)/(a^2 + b^2) is the inverse
        // of (a + bu). Importantly, this can be computing using
        // only a single inversion in Fp.

        var s = (C0.Square() + C1.Square()).TryInvert(out var t);
        result = new Fp2(C0 * t, C1 * -t);
        return s;
    }

    #region Instance math methods

    public Fp2 Negate() => -this;
    public Fp2 Multiply(in Fp2 value) => this * value;
    public Fp2 Sum(in Fp2 value) => this + value;
    public Fp2 Subtract(in Fp2 value) => this - value;

    #endregion
}
