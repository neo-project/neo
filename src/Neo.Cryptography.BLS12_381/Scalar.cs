// Copyright (C) 2015-2024 The Neo Project.
//
// Scalar.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Extensions;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using static Neo.Cryptography.BLS12_381.ConstantTimeUtility;
using static Neo.Cryptography.BLS12_381.MathUtility;
using static Neo.Cryptography.BLS12_381.ScalarConstants;

namespace Neo.Cryptography.BLS12_381
{
    [StructLayout(LayoutKind.Explicit, Size = Size)]
    public readonly struct Scalar : IEquatable<Scalar>, INumber<Scalar>
    {
        public const int Size = 32;
        public const int SizeL = Size / sizeof(ulong);
        public static readonly Scalar Default = new();

        public static ref readonly Scalar Zero => ref Default;
        public static ref readonly Scalar One => ref R;

        public bool IsZero => this == Zero;

        internal Scalar(ulong[] values)
        {
            if (values.Length != SizeL)
                throw new FormatException($"The argument `{nameof(values)}` must contain {SizeL} entries.");

            // This internal method is only used by the constants classes.
            // The data must be in the correct format.
            // So, there is no need to do any additional checks.
            this = Unsafe.As<byte, Scalar>(ref MemoryMarshal.GetReference(MemoryMarshal.Cast<ulong, byte>(values)));
        }

        public Scalar(ulong value)
        {
            Span<ulong> data = stackalloc ulong[SizeL];
            data[0] = value;
            this = FromRaw(data);
        }

        public Scalar(RandomNumberGenerator rng)
        {
            Span<byte> buffer = stackalloc byte[Size * 2];
            rng.GetBytes(buffer);
            this = FromBytesWide(buffer);
        }

        public static Scalar FromBytes(ReadOnlySpan<byte> data)
        {
            if (data.Length != Size)
                throw new FormatException($"The argument `{nameof(data)}` must contain {Size} bytes.");

            ref readonly Scalar ref_ = ref Unsafe.As<byte, Scalar>(ref MemoryMarshal.GetReference(data));

            try
            {
                return ref_ * R2;
            }
            finally
            {
                ReadOnlySpan<ulong> u64 = MemoryMarshal.Cast<byte, ulong>(data);
                ulong borrow = 0;
                (_, borrow) = Sbb(u64[0], MODULUS_LIMBS_64[0], borrow);
                (_, borrow) = Sbb(u64[1], MODULUS_LIMBS_64[1], borrow);
                (_, borrow) = Sbb(u64[2], MODULUS_LIMBS_64[2], borrow);
                (_, borrow) = Sbb(u64[3], MODULUS_LIMBS_64[3], borrow);
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

        public static Scalar FromBytesWide(ReadOnlySpan<byte> data)
        {
            if (data.Length != Size * 2)
                throw new FormatException($"The argument `{nameof(data)}` must contain {Size * 2} bytes.");

            ReadOnlySpan<Scalar> d = MemoryMarshal.Cast<byte, Scalar>(data);
            return d[0] * R2 + d[1] * R3;
        }

        public static Scalar FromRaw(ReadOnlySpan<ulong> data)
        {
            if (data.Length != SizeL)
                throw new FormatException($"The argument `{nameof(data)}` must contain {SizeL} entries.");

            ReadOnlySpan<Scalar> span = MemoryMarshal.Cast<ulong, Scalar>(data);
            return span[0] * R2;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ReadOnlySpan<byte> GetSpan()
        {
            return MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef(in this), 1));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Span<ulong> GetSpanU64()
        {
            return MemoryMarshal.Cast<Scalar, ulong>(MemoryMarshal.CreateSpan(ref Unsafe.AsRef(in this), 1));
        }

        public override string ToString()
        {
            var bytes = ToArray();

            StringBuilder sb = new(2 + (bytes.Length * 2));
            sb.Append("0x");
            sb.Append(bytes.ToHexString(true));
            return sb.ToString();
        }

        public static bool operator ==(in Scalar a, in Scalar b)
        {
            return ConstantTimeEq(in a, in b);
        }

        public static bool operator !=(in Scalar a, in Scalar b)
        {
            return !(a == b);
        }

        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            if (obj is not Scalar other) return false;
            return this == other;
        }

        public bool Equals(Scalar other)
        {
            return this == other;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public Scalar Double()
        {
            return this + this;
        }

        public byte[] ToArray()
        {
            ReadOnlySpan<ulong> self = GetSpanU64();

            // Turn into canonical form by computing
            // (a.R) / R = a
            Scalar result = MontgomeryReduce(self[0], self[1], self[2], self[3], 0, 0, 0, 0);
            return result.GetSpan().ToArray();
        }

        public Scalar Square()
        {
            ReadOnlySpan<ulong> self = GetSpanU64();
            ulong r0, r1, r2, r3, r4, r5, r6, r7;
            ulong carry;

            (r1, carry) = Mac(0, self[0], self[1], 0);
            (r2, carry) = Mac(0, self[0], self[2], carry);
            (r3, r4) = Mac(0, self[0], self[3], carry);

            (r3, carry) = Mac(r3, self[1], self[2], 0);
            (r4, r5) = Mac(r4, self[1], self[3], carry);

            (r5, r6) = Mac(r5, self[2], self[3], 0);

            r7 = r6 >> 63;
            r6 = (r6 << 1) | (r5 >> 63);
            r5 = (r5 << 1) | (r4 >> 63);
            r4 = (r4 << 1) | (r3 >> 63);
            r3 = (r3 << 1) | (r2 >> 63);
            r2 = (r2 << 1) | (r1 >> 63);
            r1 <<= 1;

            (r0, carry) = Mac(0, self[0], self[0], 0);
            (r1, carry) = Adc(r1, carry, 0);
            (r2, carry) = Mac(r2, self[1], self[1], carry);
            (r3, carry) = Adc(r3, carry, 0);
            (r4, carry) = Mac(r4, self[2], self[2], carry);
            (r5, carry) = Adc(r5, carry, 0);
            (r6, carry) = Mac(r6, self[3], self[3], carry);
            (r7, _) = Adc(r7, carry, 0);

            return MontgomeryReduce(r0, r1, r2, r3, r4, r5, r6, r7);
        }

        public Scalar Sqrt()
        {
            // Tonelli-Shank's algorithm for q mod 16 = 1
            // https://eprint.iacr.org/2012/685.pdf (page 12, algorithm 5)

            // w = self^((t - 1) // 2)
            //   = self^6104339283789297388802252303364915521546564123189034618274734669823
            var w = this.PowVartime(new ulong[]
            {
                0x7fff_2dff_7fff_ffff,
                0x04d0_ec02_a9de_d201,
                0x94ce_bea4_199c_ec04,
                0x0000_0000_39f6_d3a9
            });

            var v = S;
            var x = this * w;
            var b = x * w;

            // Initialize z as the 2^S root of unity.
            var z = ROOT_OF_UNITY;

            for (uint max_v = S; max_v >= 1; max_v--)
            {
                uint k = 1;
                var tmp = b.Square();
                var j_less_than_v = true;

                for (uint j = 2; j < max_v; j++)
                {
                    var tmp_is_one = tmp == One;
                    var squared = ConditionalSelect(in tmp, in z, tmp_is_one).Square();
                    tmp = ConditionalSelect(in squared, in tmp, tmp_is_one);
                    var new_z = ConditionalSelect(in z, in squared, tmp_is_one);
                    j_less_than_v &= j != v;
                    k = ConditionalSelect(j, k, tmp_is_one);
                    z = ConditionalSelect(in z, in new_z, j_less_than_v);
                }

                var result = x * z;
                x = ConditionalSelect(in result, in x, b == One);
                z = z.Square();
                b *= z;
                v = k;
            }

            if (x * x != this) throw new ArithmeticException();
            return x;
        }

        public Scalar Pow(ulong[] by)
        {
            if (by.Length != SizeL)
                throw new ArgumentException($"The length of the parameter `{nameof(by)}` must be {SizeL}.");

            var res = One;
            for (int j = by.Length - 1; j >= 0; j--)
            {
                for (int i = 63; i >= 0; i--)
                {
                    res = res.Square();
                    var tmp = res;
                    tmp *= this;
                    res.ConditionalAssign(in tmp, ((by[j] >> i) & 1) == 1);
                }
            }
            return res;
        }

        public Scalar Invert()
        {
            static void SquareAssignMulti(ref Scalar n, int num_times)
            {
                for (int i = 0; i < num_times; i++)
                {
                    n = n.Square();
                }
            }

            var t0 = Square();
            var t1 = t0 * this;
            var t16 = t0.Square();
            var t6 = t16.Square();
            var t5 = t6 * t0;
            t0 = t6 * t16;
            var t12 = t5 * t16;
            var t2 = t6.Square();
            var t7 = t5 * t6;
            var t15 = t0 * t5;
            var t17 = t12.Square();
            t1 *= t17;
            var t3 = t7 * t2;
            var t8 = t1 * t17;
            var t4 = t8 * t2;
            var t9 = t8 * t7;
            t7 = t4 * t5;
            var t11 = t4 * t17;
            t5 = t9 * t17;
            var t14 = t7 * t15;
            var t13 = t11 * t12;
            t12 = t11 * t17;
            t15 *= t12;
            t16 *= t15;
            t3 *= t16;
            t17 *= t3;
            t0 *= t17;
            t6 *= t0;
            t2 *= t6;
            SquareAssignMulti(ref t0, 8);
            t0 *= t17;
            SquareAssignMulti(ref t0, 9);
            t0 *= t16;
            SquareAssignMulti(ref t0, 9);
            t0 *= t15;
            SquareAssignMulti(ref t0, 9);
            t0 *= t15;
            SquareAssignMulti(ref t0, 7);
            t0 *= t14;
            SquareAssignMulti(ref t0, 7);
            t0 *= t13;
            SquareAssignMulti(ref t0, 10);
            t0 *= t12;
            SquareAssignMulti(ref t0, 9);
            t0 *= t11;
            SquareAssignMulti(ref t0, 8);
            t0 *= t8;
            SquareAssignMulti(ref t0, 8);
            t0 *= this;
            SquareAssignMulti(ref t0, 14);
            t0 *= t9;
            SquareAssignMulti(ref t0, 10);
            t0 *= t8;
            SquareAssignMulti(ref t0, 15);
            t0 *= t7;
            SquareAssignMulti(ref t0, 10);
            t0 *= t6;
            SquareAssignMulti(ref t0, 8);
            t0 *= t5;
            SquareAssignMulti(ref t0, 16);
            t0 *= t3;
            SquareAssignMulti(ref t0, 8);
            t0 *= t2;
            SquareAssignMulti(ref t0, 7);
            t0 *= t4;
            SquareAssignMulti(ref t0, 9);
            t0 *= t2;
            SquareAssignMulti(ref t0, 8);
            t0 *= t3;
            SquareAssignMulti(ref t0, 8);
            t0 *= t2;
            SquareAssignMulti(ref t0, 8);
            t0 *= t2;
            SquareAssignMulti(ref t0, 8);
            t0 *= t2;
            SquareAssignMulti(ref t0, 8);
            t0 *= t3;
            SquareAssignMulti(ref t0, 8);
            t0 *= t2;
            SquareAssignMulti(ref t0, 8);
            t0 *= t2;
            SquareAssignMulti(ref t0, 5);
            t0 *= t1;
            SquareAssignMulti(ref t0, 5);
            t0 *= t1;

            if (this == Zero) throw new DivideByZeroException();
            return t0;
        }

        private static Scalar MontgomeryReduce(ulong r0, ulong r1, ulong r2, ulong r3, ulong r4, ulong r5, ulong r6, ulong r7)
        {
            // The Montgomery reduction here is based on Algorithm 14.32 in
            // Handbook of Applied Cryptography
            // <http://cacr.uwaterloo.ca/hac/about/chap14.pdf>.

            ulong carry, carry2;

            var k = unchecked(r0 * INV);
            (_, carry) = Mac(r0, k, MODULUS_LIMBS_64[0], 0);
            (r1, carry) = Mac(r1, k, MODULUS_LIMBS_64[1], carry);
            (r2, carry) = Mac(r2, k, MODULUS_LIMBS_64[2], carry);
            (r3, carry) = Mac(r3, k, MODULUS_LIMBS_64[3], carry);
            (r4, carry2) = Adc(r4, 0, carry);

            k = unchecked(r1 * INV);
            (_, carry) = Mac(r1, k, MODULUS_LIMBS_64[0], 0);
            (r2, carry) = Mac(r2, k, MODULUS_LIMBS_64[1], carry);
            (r3, carry) = Mac(r3, k, MODULUS_LIMBS_64[2], carry);
            (r4, carry) = Mac(r4, k, MODULUS_LIMBS_64[3], carry);
            (r5, carry2) = Adc(r5, carry2, carry);

            k = unchecked(r2 * INV);
            (_, carry) = Mac(r2, k, MODULUS_LIMBS_64[0], 0);
            (r3, carry) = Mac(r3, k, MODULUS_LIMBS_64[1], carry);
            (r4, carry) = Mac(r4, k, MODULUS_LIMBS_64[2], carry);
            (r5, carry) = Mac(r5, k, MODULUS_LIMBS_64[3], carry);
            (r6, carry2) = Adc(r6, carry2, carry);

            k = unchecked(r3 * INV);
            (_, carry) = Mac(r3, k, MODULUS_LIMBS_64[0], 0);
            (r4, carry) = Mac(r4, k, MODULUS_LIMBS_64[1], carry);
            (r5, carry) = Mac(r5, k, MODULUS_LIMBS_64[2], carry);
            (r6, carry) = Mac(r6, k, MODULUS_LIMBS_64[3], carry);
            (r7, _) = Adc(r7, carry2, carry);

            // Result may be within MODULUS of the correct value
            ReadOnlySpan<ulong> tmp = stackalloc[] { r4, r5, r6, r7 };
            return MemoryMarshal.Cast<ulong, Scalar>(tmp)[0] - MODULUS;
        }

        public static Scalar operator *(in Scalar a, in Scalar b)
        {
            ReadOnlySpan<ulong> self = a.GetSpanU64(), rhs = b.GetSpanU64();
            ulong r0, r1, r2, r3, r4, r5, r6, r7;
            ulong carry;

            (r0, carry) = Mac(0, self[0], rhs[0], 0);
            (r1, carry) = Mac(0, self[0], rhs[1], carry);
            (r2, carry) = Mac(0, self[0], rhs[2], carry);
            (r3, r4) = Mac(0, self[0], rhs[3], carry);

            (r1, carry) = Mac(r1, self[1], rhs[0], 0);
            (r2, carry) = Mac(r2, self[1], rhs[1], carry);
            (r3, carry) = Mac(r3, self[1], rhs[2], carry);
            (r4, r5) = Mac(r4, self[1], rhs[3], carry);

            (r2, carry) = Mac(r2, self[2], rhs[0], 0);
            (r3, carry) = Mac(r3, self[2], rhs[1], carry);
            (r4, carry) = Mac(r4, self[2], rhs[2], carry);
            (r5, r6) = Mac(r5, self[2], rhs[3], carry);

            (r3, carry) = Mac(r3, self[3], rhs[0], 0);
            (r4, carry) = Mac(r4, self[3], rhs[1], carry);
            (r5, carry) = Mac(r5, self[3], rhs[2], carry);
            (r6, r7) = Mac(r6, self[3], rhs[3], carry);

            return MontgomeryReduce(r0, r1, r2, r3, r4, r5, r6, r7);
        }

        public static Scalar operator -(in Scalar a, in Scalar b)
        {
            ReadOnlySpan<ulong> self = a.GetSpanU64(), rhs = b.GetSpanU64();
            ulong d0, d1, d2, d3;
            ulong carry, borrow;

            (d0, borrow) = Sbb(self[0], rhs[0], 0);
            (d1, borrow) = Sbb(self[1], rhs[1], borrow);
            (d2, borrow) = Sbb(self[2], rhs[2], borrow);
            (d3, borrow) = Sbb(self[3], rhs[3], borrow);

            borrow = borrow == 0 ? ulong.MinValue : ulong.MaxValue;
            (d0, carry) = Adc(d0, MODULUS_LIMBS_64[0] & borrow, 0);
            (d1, carry) = Adc(d1, MODULUS_LIMBS_64[1] & borrow, carry);
            (d2, carry) = Adc(d2, MODULUS_LIMBS_64[2] & borrow, carry);
            (d3, _) = Adc(d3, MODULUS_LIMBS_64[3] & borrow, carry);

            ReadOnlySpan<ulong> tmp = stackalloc[] { d0, d1, d2, d3 };
            return MemoryMarshal.Cast<ulong, Scalar>(tmp)[0];
        }

        public static Scalar operator +(in Scalar a, in Scalar b)
        {
            ReadOnlySpan<ulong> self = a.GetSpanU64(), rhs = b.GetSpanU64();
            ulong d0, d1, d2, d3;
            ulong carry;

            (d0, carry) = Adc(self[0], rhs[0], 0);
            (d1, carry) = Adc(self[1], rhs[1], carry);
            (d2, carry) = Adc(self[2], rhs[2], carry);
            (d3, _) = Adc(self[3], rhs[3], carry);

            // Attempt to subtract the modulus, to ensure the value
            // is smaller than the modulus.
            ReadOnlySpan<ulong> tmp = stackalloc[] { d0, d1, d2, d3 };
            return MemoryMarshal.Cast<ulong, Scalar>(tmp)[0] - MODULUS;
        }

        public static Scalar operator -(in Scalar a)
        {
            ReadOnlySpan<ulong> self = a.GetSpanU64();
            ulong d0, d1, d2, d3;
            ulong borrow;

            // Subtract `self` from `MODULUS` to negate. Ignore the final
            // borrow because it cannot underflow; self is guaranteed to
            // be in the field.
            (d0, borrow) = Sbb(MODULUS_LIMBS_64[0], self[0], 0);
            (d1, borrow) = Sbb(MODULUS_LIMBS_64[1], self[1], borrow);
            (d2, borrow) = Sbb(MODULUS_LIMBS_64[2], self[2], borrow);
            (d3, _) = Sbb(MODULUS_LIMBS_64[3], self[3], borrow);

            // `tmp` could be `MODULUS` if `self` was zero. Create a mask that is
            // zero if `self` was zero, and `u64::max_value()` if self was nonzero.
            ulong mask = a.IsZero ? ulong.MinValue : ulong.MaxValue;

            ReadOnlySpan<ulong> tmp = stackalloc[] { d0 & mask, d1 & mask, d2 & mask, d3 & mask };
            return MemoryMarshal.Cast<ulong, Scalar>(tmp)[0];
        }

        #region Instance math methods

        public Scalar Negate() => -this;
        public Scalar Multiply(in Scalar value) => this * value;
        public Scalar Sum(in Scalar value) => this + value;
        public Scalar Subtract(in Scalar value) => this - value;

        #endregion
    }
}
