// Copyright (C) 2015-2024 The Neo Project.
//
// Fp12.cs file belongs to the neo project and is free
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

namespace Neo.Cryptography.BLS12_381;

[StructLayout(LayoutKind.Explicit, Size = Size)]
public readonly struct Fp12 : IEquatable<Fp12>, INumber<Fp12>
{
    [FieldOffset(0)]
    public readonly Fp6 C0;
    [FieldOffset(Fp6.Size)]
    public readonly Fp6 C1;

    public const int Size = Fp6.Size * 2;

    private static readonly Fp12 _zero = new();
    private static readonly Fp12 _one = new(in Fp6.One);

    public static ref readonly Fp12 Zero => ref _zero;
    public static ref readonly Fp12 One => ref _one;

    public bool IsZero => C0.IsZero & C1.IsZero;

    public Fp12(in Fp f)
        : this(new Fp6(in f), in Fp6.Zero)
    {
    }

    public Fp12(in Fp2 f)
        : this(new Fp6(in f), in Fp6.Zero)
    {
    }

    public Fp12(in Fp6 f)
        : this(in f, in Fp6.Zero)
    {
    }

    public Fp12(in Fp6 c0, in Fp6 c1)
    {
        C0 = c0;
        C1 = c1;
    }

    public static Fp12 FromBytes(ReadOnlySpan<byte> data)
    {
        if (data.Length != Size)
            throw new FormatException($"The argument `{nameof(data)}` must contain {Size} bytes.");
        var c0 = Fp6.FromBytes(data[Fp6.Size..]);
        var c1 = Fp6.FromBytes(data[..Fp6.Size]);
        return new(in c0, in c1);
    }

    public static bool operator ==(in Fp12 a, in Fp12 b)
    {
        return a.C0 == b.C0 & a.C1 == b.C1;
    }

    public static bool operator !=(in Fp12 a, in Fp12 b)
    {
        return !(a == b);
    }

    public override bool Equals([NotNullWhen(true)] object? obj)
    {
        if (obj is not Fp12 other) return false;
        return this == other;
    }

    public bool Equals(Fp12 other)
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
        C0.TryWrite(buffer[Fp6.Size..Size]);
        C1.TryWrite(buffer[0..Fp6.Size]);
        return true;
    }

    public static Fp12 Random(RandomNumberGenerator rng)
    {
        return new(Fp6.Random(rng), Fp6.Random(rng));
    }

    internal Fp12 MulBy_014(in Fp2 c0, in Fp2 c1, in Fp2 c4)
    {
        var aa = C0.MulBy_01(in c0, in c1);
        var bb = C1.MulBy_1(in c4);
        var o = c1 + c4;
        var _c1 = C1 + C0;
        _c1 = _c1.MulBy_01(in c0, in o);
        _c1 = _c1 - aa - bb;
        var _c0 = bb;
        _c0 = _c0.MulByNonresidue();
        _c0 += aa;

        return new Fp12(in _c0, in _c1);
    }

    public Fp12 Conjugate()
    {
        return new Fp12(in C0, -C1);
    }

    public Fp12 FrobeniusMap()
    {
        var c0 = C0.FrobeniusMap();
        var c1 = C1.FrobeniusMap();

        // c1 = c1 * (u + 1)^((p - 1) / 6)
        c1 *= new Fp6(new Fp2(
            Fp.FromRawUnchecked(
            [
            0x0708_9552_b319_d465,
            0xc669_5f92_b50a_8313,
            0x97e8_3ccc_d117_228f,
            0xa35b_aeca_b2dc_29ee,
            0x1ce3_93ea_5daa_ce4d,
            0x08f2_220f_b0fb_66eb
        ]), Fp.FromRawUnchecked(
        [
            0xb2f6_6aad_4ce5_d646,
            0x5842_a06b_fc49_7cec,
            0xcf48_95d4_2599_d394,
            0xc11b_9cba_40a8_e8d0,
            0x2e38_13cb_e5a0_de89,
            0x110e_efda_8884_7faf
        ])));

        return new Fp12(in c0, in c1);
    }

    public Fp12 Square()
    {
        var ab = C0 * C1;
        var c0c1 = C0 + C1;
        var c0 = C1.MulByNonresidue();
        c0 += C0;
        c0 *= c0c1;
        c0 -= ab;
        var c1 = ab + ab;
        c0 -= ab.MulByNonresidue();

        return new Fp12(in c0, in c1);
    }

    public Fp12 Invert()
    {
        var t = (C0.Square() - C1.Square().MulByNonresidue()).Invert();
        return new Fp12(C0 * t, C1 * -t);
    }

    public static Fp12 operator -(in Fp12 a)
    {
        return new Fp12(-a.C0, -a.C1);
    }

    public static Fp12 operator +(in Fp12 a, in Fp12 b)
    {
        return new Fp12(a.C0 + b.C0, a.C1 + b.C1);
    }

    public static Fp12 operator -(in Fp12 a, in Fp12 b)
    {
        return new Fp12(a.C0 - b.C0, a.C1 - b.C1);
    }

    public static Fp12 operator *(in Fp12 a, in Fp12 b)
    {
        var aa = a.C0 * b.C0;
        var bb = a.C1 * b.C1;
        var o = b.C0 + b.C1;
        var c1 = a.C1 + a.C0;
        c1 *= o;
        c1 -= aa;
        c1 -= bb;
        var c0 = bb.MulByNonresidue();
        c0 += aa;

        return new Fp12(in c0, in c1);
    }

    #region Instance math methods

    public Fp12 Negate() => -this;
    public Fp12 Multiply(in Fp12 value) => this * value;
    public Fp12 Sum(in Fp12 value) => this + value;
    public Fp12 Subtract(in Fp12 value) => this - value;

    #endregion
}
