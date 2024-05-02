// Copyright (C) 2015-2024 The Neo Project.
//
// Fp6.cs file belongs to the neo project and is free
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
public readonly struct Fp6 : IEquatable<Fp6>, INumber<Fp6>
{
    [FieldOffset(0)]
    public readonly Fp2 C0;
    [FieldOffset(Fp2.Size)]
    public readonly Fp2 C1;
    [FieldOffset(Fp2.Size * 2)]
    public readonly Fp2 C2;

    public const int Size = Fp2.Size * 3;

    private static readonly Fp6 _zero = new();
    private static readonly Fp6 _one = new(in Fp2.One);

    public static ref readonly Fp6 Zero => ref _zero;
    public static ref readonly Fp6 One => ref _one;

    public bool IsZero => C0.IsZero & C1.IsZero & C2.IsZero;

    public Fp6(in Fp f)
        : this(new Fp2(in f), in Fp2.Zero, in Fp2.Zero)
    {
    }

    public Fp6(in Fp2 f)
        : this(in f, in Fp2.Zero, in Fp2.Zero)
    {
    }

    public Fp6(in Fp2 c0, in Fp2 c1, in Fp2 c2)
    {
        C0 = c0;
        C1 = c1;
        C2 = c2;
    }

    public static Fp6 FromBytes(ReadOnlySpan<byte> data)
    {
        if (data.Length != Size)
            throw new FormatException($"The argument `{nameof(data)}` must contain {Size} bytes.");
        var c0 = Fp2.FromBytes(data[(Fp2.Size * 2)..]);
        var c1 = Fp2.FromBytes(data[Fp2.Size..(Fp2.Size * 2)]);
        var c2 = Fp2.FromBytes(data[..Fp2.Size]);
        return new(in c0, in c1, in c2);
    }

    public static bool operator ==(in Fp6 a, in Fp6 b)
    {
        return a.C0 == b.C0 & a.C1 == b.C1 & a.C2 == b.C2;
    }

    public static bool operator !=(in Fp6 a, in Fp6 b)
    {
        return !(a == b);
    }

    public override bool Equals([NotNullWhen(true)] object? obj)
    {
        if (obj is not Fp6 other) return false;
        return this == other;
    }

    public bool Equals(Fp6 other)
    {
        return this == other;
    }

    public override int GetHashCode()
    {
        return C0.GetHashCode() ^ C1.GetHashCode() ^ C2.GetHashCode();
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
        C0.TryWrite(buffer[(Fp2.Size * 2)..Size]);
        C1.TryWrite(buffer[Fp2.Size..(Fp2.Size * 2)]);
        C2.TryWrite(buffer[0..Fp2.Size]);
        return true;
    }

    public static Fp6 Random(RandomNumberGenerator rng)
    {
        return new(Fp2.Random(rng), Fp2.Random(rng), Fp2.Random(rng));
    }

    internal Fp6 MulBy_1(in Fp2 c1)
    {
        var b_b = C1 * c1;

        var t1 = (C1 + C2) * c1 - b_b;
        t1 = t1.MulByNonresidue();

        var t2 = (C0 + C1) * c1 - b_b;

        return new Fp6(in t1, in t2, in b_b);
    }

    internal Fp6 MulBy_01(in Fp2 c0, in Fp2 c1)
    {
        var a_a = C0 * c0;
        var b_b = C1 * c1;

        var t1 = (C1 + C2) * c1 - b_b;
        t1 = t1.MulByNonresidue() + a_a;

        var t2 = (c0 + c1) * (C0 + C1) - a_a - b_b;

        var t3 = (C0 + C2) * c0 - a_a + b_b;

        return new Fp6(in t1, in t2, in t3);
    }

    public Fp6 MulByNonresidue()
    {
        // Given a + bv + cv^2, this produces
        //     av + bv^2 + cv^3
        // but because v^3 = u + 1, we have
        //     c(u + 1) + av + v^2

        return new Fp6(C2.MulByNonresidue(), in C0, in C1);
    }

    public Fp6 FrobeniusMap()
    {
        var c0 = C0.FrobeniusMap();
        var c1 = C1.FrobeniusMap();
        var c2 = C2.FrobeniusMap();

        // c1 = c1 * (u + 1)^((p - 1) / 3)
        c1 *= new Fp2(in Fp.Zero, Fp.FromRawUnchecked(
        [
            0xcd03_c9e4_8671_f071,
            0x5dab_2246_1fcd_a5d2,
            0x5870_42af_d385_1b95,
            0x8eb6_0ebe_01ba_cb9e,
            0x03f9_7d6e_83d0_50d2,
            0x18f0_2065_5463_8741
        ]));

        // c2 = c2 * (u + 1)^((2p - 2) / 3)
        c2 *= new Fp2(Fp.FromRawUnchecked(
        [
            0x890d_c9e4_8675_45c3,
            0x2af3_2253_3285_a5d5,
            0x5088_0866_309b_7e2c,
            0xa20d_1b8c_7e88_1024,
            0x14e4_f04f_e2db_9068,
            0x14e5_6d3f_1564_853a
        ]), in Fp.Zero);

        return new Fp6(c0, c1, c2);
    }

    public Fp6 Square()
    {
        var s0 = C0.Square();
        var ab = C0 * C1;
        var s1 = ab + ab;
        var s2 = (C0 - C1 + C2).Square();
        var bc = C1 * C2;
        var s3 = bc + bc;
        var s4 = C2.Square();

        return new Fp6(
            s3.MulByNonresidue() + s0,
            s4.MulByNonresidue() + s1,
            s1 + s2 + s3 - s0 - s4
        );
    }

    public Fp6 Invert()
    {
        var c0 = (C1 * C2).MulByNonresidue();
        c0 = C0.Square() - c0;

        var c1 = C2.Square().MulByNonresidue();
        c1 -= C0 * C1;

        var c2 = C1.Square();
        c2 -= C0 * C2;

        var t = (C1 * c2 + C2 * c1).MulByNonresidue();
        t += C0 * c0;

        t = t.Invert();
        return new Fp6(t * c0, t * c1, t * c2);
    }

    public static Fp6 operator -(in Fp6 a)
    {
        return new Fp6(-a.C0, -a.C1, -a.C2);
    }

    public static Fp6 operator +(in Fp6 a, in Fp6 b)
    {
        return new Fp6(a.C0 + b.C0, a.C1 + b.C1, a.C2 + b.C2);
    }

    public static Fp6 operator -(in Fp6 a, in Fp6 b)
    {
        return new Fp6(a.C0 - b.C0, a.C1 - b.C1, a.C2 - b.C2);
    }

    public static Fp6 operator *(in Fp6 a, in Fp6 b)
    {
        // The intuition for this algorithm is that we can look at F_p^6 as a direct
        // extension of F_p^2, and express the overall operations down to the base field
        // F_p instead of only over F_p^2. This enables us to interleave multiplications
        // and reductions, ensuring that we don't require double-width intermediate
        // representations (with around twice as many limbs as F_p elements).

        // We want to express the multiplication c = a x b, where a = (a_0, a_1, a_2) is
        // an element of F_p^6, and a_i = (a_i,0, a_i,1) is an element of F_p^2. The fully
        // expanded multiplication is given by (2022-376 §5):
        //
        //   c_0,0 = a_0,0 b_0,0 - a_0,1 b_0,1 + a_1,0 b_2,0 - a_1,1 b_2,1 + a_2,0 b_1,0 - a_2,1 b_1,1
        //                                     - a_1,0 b_2,1 - a_1,1 b_2,0 - a_2,0 b_1,1 - a_2,1 b_1,0.
        //         = a_0,0 b_0,0 - a_0,1 b_0,1 + a_1,0 (b_2,0 - b_2,1) - a_1,1 (b_2,0 + b_2,1)
        //                                     + a_2,0 (b_1,0 - b_1,1) - a_2,1 (b_1,0 + b_1,1).
        //
        //   c_0,1 = a_0,0 b_0,1 + a_0,1 b_0,0 + a_1,0 b_2,1 + a_1,1 b_2,0 + a_2,0 b_1,1 + a_2,1 b_1,0
        //                                     + a_1,0 b_2,0 - a_1,1 b_2,1 + a_2,0 b_1,0 - a_2,1 b_1,1.
        //         = a_0,0 b_0,1 + a_0,1 b_0,0 + a_1,0(b_2,0 + b_2,1) + a_1,1(b_2,0 - b_2,1)
        //                                     + a_2,0(b_1,0 + b_1,1) + a_2,1(b_1,0 - b_1,1).
        //
        //   c_1,0 = a_0,0 b_1,0 - a_0,1 b_1,1 + a_1,0 b_0,0 - a_1,1 b_0,1 + a_2,0 b_2,0 - a_2,1 b_2,1
        //                                                                 - a_2,0 b_2,1 - a_2,1 b_2,0.
        //         = a_0,0 b_1,0 - a_0,1 b_1,1 + a_1,0 b_0,0 - a_1,1 b_0,1 + a_2,0(b_2,0 - b_2,1)
        //                                                                 - a_2,1(b_2,0 + b_2,1).
        //
        //   c_1,1 = a_0,0 b_1,1 + a_0,1 b_1,0 + a_1,0 b_0,1 + a_1,1 b_0,0 + a_2,0 b_2,1 + a_2,1 b_2,0
        //                                                                 + a_2,0 b_2,0 - a_2,1 b_2,1
        //         = a_0,0 b_1,1 + a_0,1 b_1,0 + a_1,0 b_0,1 + a_1,1 b_0,0 + a_2,0(b_2,0 + b_2,1)
        //                                                                 + a_2,1(b_2,0 - b_2,1).
        //
        //   c_2,0 = a_0,0 b_2,0 - a_0,1 b_2,1 + a_1,0 b_1,0 - a_1,1 b_1,1 + a_2,0 b_0,0 - a_2,1 b_0,1.
        //   c_2,1 = a_0,0 b_2,1 + a_0,1 b_2,0 + a_1,0 b_1,1 + a_1,1 b_1,0 + a_2,0 b_0,1 + a_2,1 b_0,0.
        //
        // Each of these is a "sum of products", which we can compute efficiently.

        var b10_p_b11 = b.C1.C0 + b.C1.C1;
        var b10_m_b11 = b.C1.C0 - b.C1.C1;
        var b20_p_b21 = b.C2.C0 + b.C2.C1;
        var b20_m_b21 = b.C2.C0 - b.C2.C1;

        return new Fp6(new Fp2(
            Fp.SumOfProducts(
                [a.C0.C0, -a.C0.C1, a.C1.C0, -a.C1.C1, a.C2.C0, -a.C2.C1],
                [b.C0.C0, b.C0.C1, b20_m_b21, b20_p_b21, b10_m_b11, b10_p_b11]
            ),
            Fp.SumOfProducts(
                [a.C0.C0, a.C0.C1, a.C1.C0, a.C1.C1, a.C2.C0, a.C2.C1],
                [b.C0.C1, b.C0.C0, b20_p_b21, b20_m_b21, b10_p_b11, b10_m_b11]
            )), new Fp2(
            Fp.SumOfProducts(
                [a.C0.C0, -a.C0.C1, a.C1.C0, -a.C1.C1, a.C2.C0, -a.C2.C1],
                [b.C1.C0, b.C1.C1, b.C0.C0, b.C0.C1, b20_m_b21, b20_p_b21]
            ),
            Fp.SumOfProducts(
                [a.C0.C0, a.C0.C1, a.C1.C0, a.C1.C1, a.C2.C0, a.C2.C1],
                [b.C1.C1, b.C1.C0, b.C0.C1, b.C0.C0, b20_p_b21, b20_m_b21]
            )), new Fp2(
            Fp.SumOfProducts(
                [a.C0.C0, -a.C0.C1, a.C1.C0, -a.C1.C1, a.C2.C0, -a.C2.C1],
                [b.C2.C0, b.C2.C1, b.C1.C0, b.C1.C1, b.C0.C0, b.C0.C1]
            ),
            Fp.SumOfProducts(
                [a.C0.C0, a.C0.C1, a.C1.C0, a.C1.C1, a.C2.C0, a.C2.C1],
                [b.C2.C1, b.C2.C0, b.C1.C1, b.C1.C0, b.C0.C1, b.C0.C0]
            ))
        );
    }

    #region Instance math methods

    public Fp6 Negate() => -this;
    public Fp6 Multiply(in Fp6 value) => this * value;
    public Fp6 Sum(in Fp6 value) => this + value;
    public Fp6 Subtract(in Fp6 value) => this - value;

    #endregion
}
