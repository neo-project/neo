// Copyright (C) 2015-2024 The Neo Project.
//
// Fp.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using static Neo.Cryptography.BLS12_381.ConstantTimeUtility;
using static Neo.Cryptography.BLS12_381.FpConstants;
using static Neo.Cryptography.BLS12_381.MathUtility;

namespace Neo.Cryptography.BLS12_381;

[StructLayout(LayoutKind.Explicit, Size = Size)]
public readonly struct Fp : IEquatable<Fp>, INumber<Fp>
{
    public const int Size = 48;
    public const int SizeL = Size / sizeof(ulong);

    private static readonly Fp _zero = new();

    public static ref readonly Fp Zero => ref _zero;
    public static ref readonly Fp One => ref R;

    public bool IsZero => this == Zero;

    public static Fp FromBytes(ReadOnlySpan<byte> data)
    {
        if (data.Length != Size)
            throw new FormatException($"The argument `{nameof(data)}` must contain {Size} bytes.");

        Span<ulong> tmp = stackalloc ulong[SizeL];
        BinaryPrimitives.TryReadUInt64BigEndian(data[0..8], out tmp[5]);
        BinaryPrimitives.TryReadUInt64BigEndian(data[8..16], out tmp[4]);
        BinaryPrimitives.TryReadUInt64BigEndian(data[16..24], out tmp[3]);
        BinaryPrimitives.TryReadUInt64BigEndian(data[24..32], out tmp[2]);
        BinaryPrimitives.TryReadUInt64BigEndian(data[32..40], out tmp[1]);
        BinaryPrimitives.TryReadUInt64BigEndian(data[40..48], out tmp[0]);
        ReadOnlySpan<Fp> span = MemoryMarshal.Cast<ulong, Fp>(tmp);

        try
        {
            return span[0] * R2;
        }
        finally
        {
            ulong borrow;
            (_, borrow) = Sbb(tmp[0], MODULUS[0], 0);
            (_, borrow) = Sbb(tmp[1], MODULUS[1], borrow);
            (_, borrow) = Sbb(tmp[2], MODULUS[2], borrow);
            (_, borrow) = Sbb(tmp[3], MODULUS[3], borrow);
            (_, borrow) = Sbb(tmp[4], MODULUS[4], borrow);
            (_, borrow) = Sbb(tmp[5], MODULUS[5], borrow);
            if (borrow == 0)
            {
                // If the element is smaller than MODULUS then the subtraction will underflow.
                // Otherwise, throws.
                // Why not throw before return?
                // Because we want to run the method in a constant time.
                throw new FormatException();
            }
        }
    }

    internal static Fp FromRawUnchecked(ulong[] values)
    {
        if (values.Length != SizeL)
            throw new FormatException($"The argument `{nameof(values)}` must contain {SizeL} entries.");

        return MemoryMarshal.Cast<ulong, Fp>(values)[0];
    }

    public static Fp Random(RandomNumberGenerator rng)
    {
        Span<byte> buffer = stackalloc byte[Size * 2];
        rng.GetBytes(buffer);
        var d = MemoryMarshal.Cast<byte, Fp>(buffer);
        return d[0] * R2 + d[1] * R3;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ReadOnlySpan<byte> GetSpan()
    {
        return MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef(in this), 1));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Span<ulong> GetSpanU64()
    {
        return MemoryMarshal.Cast<Fp, ulong>(MemoryMarshal.CreateSpan(ref Unsafe.AsRef(in this), 1));
    }

    public static bool operator ==(in Fp left, in Fp right)
    {
        return ConstantTimeEq(in left, in right);
    }

    public static bool operator !=(in Fp left, in Fp right)
    {
        return !(left == right);
    }

    public override bool Equals([NotNullWhen(true)] object? obj)
    {
        if (obj is not Fp other) return false;
        return this == other;
    }

    public bool Equals(Fp other)
    {
        return this == other;
    }

    public override int GetHashCode()
    {
        return base.GetHashCode();
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

        ReadOnlySpan<ulong> u64 = GetSpanU64();
        var tmp = MontgomeryReduce(u64[0], u64[1], u64[2], u64[3], u64[4], u64[5], 0, 0, 0, 0, 0, 0);
        u64 = tmp.GetSpanU64();

        BinaryPrimitives.WriteUInt64BigEndian(buffer[0..8], u64[5]);
        BinaryPrimitives.WriteUInt64BigEndian(buffer[8..16], u64[4]);
        BinaryPrimitives.WriteUInt64BigEndian(buffer[16..24], u64[3]);
        BinaryPrimitives.WriteUInt64BigEndian(buffer[24..32], u64[2]);
        BinaryPrimitives.WriteUInt64BigEndian(buffer[32..40], u64[1]);
        BinaryPrimitives.WriteUInt64BigEndian(buffer[40..48], u64[0]);

        return true;
    }

    public override string ToString()
    {
        var output = string.Empty;
        foreach (var b in ToArray())
            output += b.ToString("x2");

        return "0x" + output;
    }

    public bool LexicographicallyLargest()
    {
        ReadOnlySpan<ulong> s = GetSpanU64();
        var tmp = MontgomeryReduce(s[0], s[1], s[2], s[3], s[4], s[5], 0, 0, 0, 0, 0, 0);
        ReadOnlySpan<ulong> t = tmp.GetSpanU64();
        ulong borrow;

        (_, borrow) = Sbb(t[0], 0xdcff_7fff_ffff_d556, 0);
        (_, borrow) = Sbb(t[1], 0x0f55_ffff_58a9_ffff, borrow);
        (_, borrow) = Sbb(t[2], 0xb398_6950_7b58_7b12, borrow);
        (_, borrow) = Sbb(t[3], 0xb23b_a5c2_79c2_895f, borrow);
        (_, borrow) = Sbb(t[4], 0x258d_d3db_21a5_d66b, borrow);
        (_, borrow) = Sbb(t[5], 0x0d00_88f5_1cbf_f34d, borrow);

        return borrow == 0;
    }

    public Fp Sqrt()
    {
        // We use Shank's method, as p = 3 (mod 4). This means
        // we only need to exponentiate by (p + 1) / 4. This only
        // works for elements that are actually quadratic residue,
        // so we check that we got the correct result at the end.
        var result = this.PowVartime(P_1_4);
        if (result.Square() != this) throw new ArithmeticException();
        return result;
    }

    public Fp Invert()
    {
        if (!TryInvert(out var result))
            throw new DivideByZeroException();
        return result;
    }

    public bool TryInvert(out Fp result)
    {
        // Exponentiate by p - 2
        result = this.PowVartime(P_2);

        // Why not return before Pow() if IsZero?
        // Because we want to run the method in a constant time.
        return !IsZero;
    }

    private Fp SubtractP()
    {
        Fp result;
        ReadOnlySpan<ulong> s = GetSpanU64();
        var r = result.GetSpanU64();
        ulong borrow;

        (r[0], borrow) = Sbb(s[0], MODULUS[0], 0);
        (r[1], borrow) = Sbb(s[1], MODULUS[1], borrow);
        (r[2], borrow) = Sbb(s[2], MODULUS[2], borrow);
        (r[3], borrow) = Sbb(s[3], MODULUS[3], borrow);
        (r[4], borrow) = Sbb(s[4], MODULUS[4], borrow);
        (r[5], borrow) = Sbb(s[5], MODULUS[5], borrow);

        borrow = borrow == 0 ? ulong.MinValue : ulong.MaxValue;
        r[0] = (s[0] & borrow) | (r[0] & ~borrow);
        r[1] = (s[1] & borrow) | (r[1] & ~borrow);
        r[2] = (s[2] & borrow) | (r[2] & ~borrow);
        r[3] = (s[3] & borrow) | (r[3] & ~borrow);
        r[4] = (s[4] & borrow) | (r[4] & ~borrow);
        r[5] = (s[5] & borrow) | (r[5] & ~borrow);

        return result;
    }

    public static Fp operator +(in Fp a, in Fp b)
    {
        Fp result;
        ReadOnlySpan<ulong> s = a.GetSpanU64(), r = b.GetSpanU64();
        var d = result.GetSpanU64();

        ulong carry = 0;
        (d[0], carry) = Adc(s[0], r[0], carry);
        (d[1], carry) = Adc(s[1], r[1], carry);
        (d[2], carry) = Adc(s[2], r[2], carry);
        (d[3], carry) = Adc(s[3], r[3], carry);
        (d[4], carry) = Adc(s[4], r[4], carry);
        (d[5], _) = Adc(s[5], r[5], carry);

        return result.SubtractP();
    }

    public static Fp operator -(in Fp a)
    {
        Fp result;
        ReadOnlySpan<ulong> self = a.GetSpanU64();
        var d = result.GetSpanU64();

        ulong borrow = 0;
        (d[0], borrow) = Sbb(MODULUS[0], self[0], borrow);
        (d[1], borrow) = Sbb(MODULUS[1], self[1], borrow);
        (d[2], borrow) = Sbb(MODULUS[2], self[2], borrow);
        (d[3], borrow) = Sbb(MODULUS[3], self[3], borrow);
        (d[4], borrow) = Sbb(MODULUS[4], self[4], borrow);
        (d[5], _) = Sbb(MODULUS[5], self[5], borrow);

        var mask = a.IsZero ? ulong.MinValue : ulong.MaxValue;
        d[0] &= mask;
        d[1] &= mask;
        d[2] &= mask;
        d[3] &= mask;
        d[4] &= mask;
        d[5] &= mask;

        return result;
    }

    public static Fp operator -(in Fp a, in Fp b)
    {
        return -b + a;
    }

    public static Fp SumOfProducts(ReadOnlySpan<Fp> a, ReadOnlySpan<Fp> b)
    {
        var length = a.Length;
        if (length != b.Length)
            throw new ArgumentException("The lengths of the two arrays must be the same.");

        Fp result;
        var au = MemoryMarshal.Cast<Fp, ulong>(a);
        var bu = MemoryMarshal.Cast<Fp, ulong>(b);
        var u = result.GetSpanU64();

        for (var j = 0; j < 6; j++)
        {
            ulong carry;

            var (t0, t1, t2, t3, t4, t5, t6) = (u[0], u[1], u[2], u[3], u[4], u[5], 0ul);
            for (var i = 0; i < length; i++)
            {
                (t0, carry) = Mac(t0, au[i * SizeL + j], bu[i * SizeL + 0], 0);
                (t1, carry) = Mac(t1, au[i * SizeL + j], bu[i * SizeL + 1], carry);
                (t2, carry) = Mac(t2, au[i * SizeL + j], bu[i * SizeL + 2], carry);
                (t3, carry) = Mac(t3, au[i * SizeL + j], bu[i * SizeL + 3], carry);
                (t4, carry) = Mac(t4, au[i * SizeL + j], bu[i * SizeL + 4], carry);
                (t5, carry) = Mac(t5, au[i * SizeL + j], bu[i * SizeL + 5], carry);
                (t6, _) = Adc(t6, 0, carry);
            }

            var k = unchecked(t0 * INV);
            (_, carry) = Mac(t0, k, MODULUS[0], 0);
            (u[0], carry) = Mac(t1, k, MODULUS[1], carry);
            (u[1], carry) = Mac(t2, k, MODULUS[2], carry);
            (u[2], carry) = Mac(t3, k, MODULUS[3], carry);
            (u[3], carry) = Mac(t4, k, MODULUS[4], carry);
            (u[4], carry) = Mac(t5, k, MODULUS[5], carry);
            (u[5], _) = Adc(t6, 0, carry);
        }

        return result.SubtractP();
    }

    private static Fp MontgomeryReduce(ulong r0, ulong r1, ulong r2, ulong r3, ulong r4, ulong r5, ulong r6, ulong r7, ulong r8, ulong r9, ulong r10, ulong r11)
    {
        ulong carry, carry2;

        var k = unchecked(r0 * INV);
        (_, carry) = Mac(r0, k, MODULUS[0], 0);
        (r1, carry) = Mac(r1, k, MODULUS[1], carry);
        (r2, carry) = Mac(r2, k, MODULUS[2], carry);
        (r3, carry) = Mac(r3, k, MODULUS[3], carry);
        (r4, carry) = Mac(r4, k, MODULUS[4], carry);
        (r5, carry) = Mac(r5, k, MODULUS[5], carry);
        (r6, carry2) = Adc(r6, 0, carry);

        k = unchecked(r1 * INV);
        (_, carry) = Mac(r1, k, MODULUS[0], 0);
        (r2, carry) = Mac(r2, k, MODULUS[1], carry);
        (r3, carry) = Mac(r3, k, MODULUS[2], carry);
        (r4, carry) = Mac(r4, k, MODULUS[3], carry);
        (r5, carry) = Mac(r5, k, MODULUS[4], carry);
        (r6, carry) = Mac(r6, k, MODULUS[5], carry);
        (r7, carry2) = Adc(r7, carry2, carry);

        k = unchecked(r2 * INV);
        (_, carry) = Mac(r2, k, MODULUS[0], 0);
        (r3, carry) = Mac(r3, k, MODULUS[1], carry);
        (r4, carry) = Mac(r4, k, MODULUS[2], carry);
        (r5, carry) = Mac(r5, k, MODULUS[3], carry);
        (r6, carry) = Mac(r6, k, MODULUS[4], carry);
        (r7, carry) = Mac(r7, k, MODULUS[5], carry);
        (r8, carry2) = Adc(r8, carry2, carry);

        k = unchecked(r3 * INV);
        (_, carry) = Mac(r3, k, MODULUS[0], 0);
        (r4, carry) = Mac(r4, k, MODULUS[1], carry);
        (r5, carry) = Mac(r5, k, MODULUS[2], carry);
        (r6, carry) = Mac(r6, k, MODULUS[3], carry);
        (r7, carry) = Mac(r7, k, MODULUS[4], carry);
        (r8, carry) = Mac(r8, k, MODULUS[5], carry);
        (r9, carry2) = Adc(r9, carry2, carry);

        k = unchecked(r4 * INV);
        (_, carry) = Mac(r4, k, MODULUS[0], 0);
        (r5, carry) = Mac(r5, k, MODULUS[1], carry);
        (r6, carry) = Mac(r6, k, MODULUS[2], carry);
        (r7, carry) = Mac(r7, k, MODULUS[3], carry);
        (r8, carry) = Mac(r8, k, MODULUS[4], carry);
        (r9, carry) = Mac(r9, k, MODULUS[5], carry);
        (r10, carry2) = Adc(r10, carry2, carry);

        k = unchecked(r5 * INV);
        (_, carry) = Mac(r5, k, MODULUS[0], 0);
        (r6, carry) = Mac(r6, k, MODULUS[1], carry);
        (r7, carry) = Mac(r7, k, MODULUS[2], carry);
        (r8, carry) = Mac(r8, k, MODULUS[3], carry);
        (r9, carry) = Mac(r9, k, MODULUS[4], carry);
        (r10, carry) = Mac(r10, k, MODULUS[5], carry);
        (r11, _) = Adc(r11, carry2, carry);

        ReadOnlySpan<ulong> tmp = stackalloc[] { r6, r7, r8, r9, r10, r11 };
        return MemoryMarshal.Cast<ulong, Fp>(tmp)[0].SubtractP();
    }

    public static Fp operator *(in Fp a, in Fp b)
    {
        ReadOnlySpan<ulong> s = a.GetSpanU64(), r = b.GetSpanU64();
        Span<ulong> t = stackalloc ulong[SizeL * 2];
        ulong carry;

        (t[0], carry) = Mac(0, s[0], r[0], 0);
        (t[1], carry) = Mac(0, s[0], r[1], carry);
        (t[2], carry) = Mac(0, s[0], r[2], carry);
        (t[3], carry) = Mac(0, s[0], r[3], carry);
        (t[4], carry) = Mac(0, s[0], r[4], carry);
        (t[5], t[6]) = Mac(0, s[0], r[5], carry);

        (t[1], carry) = Mac(t[1], s[1], r[0], 0);
        (t[2], carry) = Mac(t[2], s[1], r[1], carry);
        (t[3], carry) = Mac(t[3], s[1], r[2], carry);
        (t[4], carry) = Mac(t[4], s[1], r[3], carry);
        (t[5], carry) = Mac(t[5], s[1], r[4], carry);
        (t[6], t[7]) = Mac(t[6], s[1], r[5], carry);

        (t[2], carry) = Mac(t[2], s[2], r[0], 0);
        (t[3], carry) = Mac(t[3], s[2], r[1], carry);
        (t[4], carry) = Mac(t[4], s[2], r[2], carry);
        (t[5], carry) = Mac(t[5], s[2], r[3], carry);
        (t[6], carry) = Mac(t[6], s[2], r[4], carry);
        (t[7], t[8]) = Mac(t[7], s[2], r[5], carry);
        (t[3], carry) = Mac(t[3], s[3], r[0], 0);
        (t[4], carry) = Mac(t[4], s[3], r[1], carry);
        (t[5], carry) = Mac(t[5], s[3], r[2], carry);
        (t[6], carry) = Mac(t[6], s[3], r[3], carry);
        (t[7], carry) = Mac(t[7], s[3], r[4], carry);
        (t[8], t[9]) = Mac(t[8], s[3], r[5], carry);
        (t[4], carry) = Mac(t[4], s[4], r[0], 0);
        (t[5], carry) = Mac(t[5], s[4], r[1], carry);
        (t[6], carry) = Mac(t[6], s[4], r[2], carry);
        (t[7], carry) = Mac(t[7], s[4], r[3], carry);
        (t[8], carry) = Mac(t[8], s[4], r[4], carry);
        (t[9], t[10]) = Mac(t[9], s[4], r[5], carry);
        (t[5], carry) = Mac(t[5], s[5], r[0], 0);
        (t[6], carry) = Mac(t[6], s[5], r[1], carry);
        (t[7], carry) = Mac(t[7], s[5], r[2], carry);
        (t[8], carry) = Mac(t[8], s[5], r[3], carry);
        (t[9], carry) = Mac(t[9], s[5], r[4], carry);
        (t[10], t[11]) = Mac(t[10], s[5], r[5], carry);

        return MontgomeryReduce(t[0], t[1], t[2], t[3], t[4], t[5], t[6], t[7], t[8], t[9], t[10], t[11]);
    }

    public Fp Square()
    {
        ReadOnlySpan<ulong> self = GetSpanU64();
        ulong t0, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11;
        ulong carry;

        (t1, carry) = Mac(0, self[0], self[1], 0);
        (t2, carry) = Mac(0, self[0], self[2], carry);
        (t3, carry) = Mac(0, self[0], self[3], carry);
        (t4, carry) = Mac(0, self[0], self[4], carry);
        (t5, t6) = Mac(0, self[0], self[5], carry);

        (t3, carry) = Mac(t3, self[1], self[2], 0);
        (t4, carry) = Mac(t4, self[1], self[3], carry);
        (t5, carry) = Mac(t5, self[1], self[4], carry);
        (t6, t7) = Mac(t6, self[1], self[5], carry);

        (t5, carry) = Mac(t5, self[2], self[3], 0);
        (t6, carry) = Mac(t6, self[2], self[4], carry);
        (t7, t8) = Mac(t7, self[2], self[5], carry);

        (t7, carry) = Mac(t7, self[3], self[4], 0);
        (t8, t9) = Mac(t8, self[3], self[5], carry);

        (t9, t10) = Mac(t9, self[4], self[5], 0);

        t11 = t10 >> 63;
        t10 = (t10 << 1) | (t9 >> 63);
        t9 = (t9 << 1) | (t8 >> 63);
        t8 = (t8 << 1) | (t7 >> 63);
        t7 = (t7 << 1) | (t6 >> 63);
        t6 = (t6 << 1) | (t5 >> 63);
        t5 = (t5 << 1) | (t4 >> 63);
        t4 = (t4 << 1) | (t3 >> 63);
        t3 = (t3 << 1) | (t2 >> 63);
        t2 = (t2 << 1) | (t1 >> 63);
        t1 <<= 1;

        (t0, carry) = Mac(0, self[0], self[0], 0);
        (t1, carry) = Adc(t1, carry, 0);
        (t2, carry) = Mac(t2, self[1], self[1], carry);
        (t3, carry) = Adc(t3, carry, 0);
        (t4, carry) = Mac(t4, self[2], self[2], carry);
        (t5, carry) = Adc(t5, carry, 0);
        (t6, carry) = Mac(t6, self[3], self[3], carry);
        (t7, carry) = Adc(t7, carry, 0);
        (t8, carry) = Mac(t8, self[4], self[4], carry);
        (t9, carry) = Adc(t9, carry, 0);
        (t10, carry) = Mac(t10, self[5], self[5], carry);
        (t11, _) = Adc(t11, carry, 0);

        return MontgomeryReduce(t0, t1, t2, t3, t4, t5, t6, t7, t8, t9, t10, t11);
    }

    #region Instance math methods

    public Fp Negate() => -this;
    public Fp Multiply(in Fp value) => this * value;
    public Fp Sum(in Fp value) => this + value;
    public Fp Subtract(in Fp value) => this - value;

    #endregion
}
