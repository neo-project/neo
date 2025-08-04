// Copyright (C) 2015-2025 The Neo Project.
//
// Fp.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Neo.Cryptography.BN254.FpConstants;
using static Neo.Cryptography.BN254.MathUtility;

namespace Neo.Cryptography.BN254
{
    [StructLayout(LayoutKind.Explicit, Size = 32)]
    public readonly struct Fp : INumber<Fp>
    {
        // BN254 prime field: p = 21888242871839275222246405745257275088696311157297823662689037894645226208583
        // This is split into 4 64-bit limbs
        public const int Size = 32;

        [FieldOffset(0)] private readonly ulong u0;
        [FieldOffset(8)] private readonly ulong u1;
        [FieldOffset(16)] private readonly ulong u2;
        [FieldOffset(24)] private readonly ulong u3;

        // BN254 prime field modulus
        public static readonly Fp Modulus = new(new ulong[]
        {
            0x3c208c16d87cfd47,
            0x97816a916871ca8d,
            0xb85045b68181585d,
            0x30644e72e131a029
        });

        public static ref readonly Fp Zero => ref zero;
        public static ref readonly Fp One => ref one;

        private static readonly Fp zero = new();
        private static readonly Fp one = R;

        public Fp(ulong u0, ulong u1, ulong u2, ulong u3)
        {
            this.u0 = u0;
            this.u1 = u1;
            this.u2 = u2;
            this.u3 = u3;
        }

        public Fp(ReadOnlySpan<ulong> data)
        {
            if (data.Length < 4)
                throw new ArgumentException($"Input must contain at least 4 ulongs, got {data.Length}");
            u0 = data[0];
            u1 = data[1];
            u2 = data[2];
            u3 = data[3];
        }

        public static Fp FromBytes(ReadOnlySpan<byte> data)
        {
            if (data.Length != Size)
                throw new ArgumentException($"Invalid data length {data.Length}, expected {Size}");

            var tmp = MemoryMarshal.Cast<byte, ulong>(data);
            Fp result = new(tmp);

            // Convert from Montgomery form
            result = result * R2;
            return result.Reduce();
        }

        public static Fp FromRawUnchecked(ulong u0, ulong u1, ulong u2, ulong u3)
        {
            return new Fp(u0, u1, u2, u3);
        }

        public static bool operator ==(in Fp a, in Fp b)
        {
            return a.u0 == b.u0 && a.u1 == b.u1 && a.u2 == b.u2 && a.u3 == b.u3;
        }

        public static bool operator !=(in Fp a, in Fp b)
        {
            return !(a == b);
        }

        public override bool Equals(object? obj)
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
            return HashCode.Combine(u0, u1, u2, u3);
        }

        public static Fp operator +(in Fp a, in Fp b)
        {
            return Add(a, b);
        }

        public static Fp operator -(in Fp a, in Fp b)
        {
            return Subtract(a, b);
        }

        public static Fp operator *(in Fp a, in Fp b)
        {
            return Multiply(a, b);
        }

        public static Fp operator -(in Fp a)
        {
            return a.Negate();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Fp Add(in Fp a, in Fp b)
        {
            ulong carry = 0;
            (var u0, carry) = Adc(a.u0, b.u0, carry);
            (var u1, carry) = Adc(a.u1, b.u1, carry);
            (var u2, carry) = Adc(a.u2, b.u2, carry);
            (var u3, carry) = Adc(a.u3, b.u3, carry);

            var result = new Fp(u0, u1, u2, u3);
            return result.Reduce();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Fp Subtract(in Fp a, in Fp b)
        {
            return Add(a, b.Negate());
        }

        private static Fp Multiply(in Fp a, in Fp b)
        {
            // Montgomery multiplication
            return MontgomeryReduce(
                a.u0, a.u1, a.u2, a.u3,
                b.u0, b.u1, b.u2, b.u3
            );
        }


<<<<<<< TODO: Unmerged change from project 'Neo.Cryptography.BN254(net9.0)', Before:
        private Fp Reduce() 
=======
        private Fp Reduce()
>>>>>>> After
        private Fp Reduce()
<<<<<<< TODO: Unmerged change from project 'Neo.Cryptography.BN254(net9.0)', Before:
                      (u3 == Modulus.u3 && 
=======
                      (u3 == Modulus.u3 &&
>>>>>>> After

<<<<<<< TODO: Unmerged change from project 'Neo.Cryptography.BN254(net9.0)', Before:
        private static Fp MontgomeryReduce(ulong r0, ulong r1, ulong r2, ulong r3, 
=======
        private static Fp MontgomeryReduce(ulong r0, ulong r1, ulong r2, ulong r3,
>>>>>>> After

        {
            // Compare with modulus and subtract if needed
            bool geq = u3 > Modulus.u3 ||
                      (u3 == Modulus.u3 &&
                       (u2 > Modulus.u2 ||
                        (u2 == Modulus.u2 &&
                         (u1 > Modulus.u1 ||
                          (u1 == Modulus.u1 && u0 >= Modulus.u0)))));

            if (geq)
            {
                ulong borrow = 0;
                (var r0, borrow) = Sbb(u0, Modulus.u0, borrow);
                (var r1, borrow) = Sbb(u1, Modulus.u1, borrow);
                (var r2, borrow) = Sbb(u2, Modulus.u2, borrow);
                (var r3, borrow) = Sbb(u3, Modulus.u3, borrow);
                return new Fp(r0, r1, r2, r3);
            }

            return this;
        }

        private static Fp MontgomeryReduce(ulong r0, ulong r1, ulong r2, ulong r3,
                                          ulong r4, ulong r5, ulong r6, ulong r7)
        {
            // Montgomery reduction using BN254 inverse: 0xc2e1f593efffffff
            const ulong inv = 0xc2e1f593efffffff;

            // Montgomery reduction steps
            ulong k = r0 * inv;
            (_, var carry) = Mac(r0, k, Modulus.u0, 0);
            (r1, carry) = Mac(r1, k, Modulus.u1, carry);
            (r2, carry) = Mac(r2, k, Modulus.u2, carry);
            (r3, carry) = Mac(r3, k, Modulus.u3, carry);
            (r4, r5) = Adc(r4, 0, carry);

            k = r1 * inv;
            (_, carry) = Mac(r1, k, Modulus.u0, 0);
            (r2, carry) = Mac(r2, k, Modulus.u1, carry);
            (r3, carry) = Mac(r3, k, Modulus.u2, carry);
            (r4, carry) = Mac(r4, k, Modulus.u3, carry);
            (r5, r6) = Adc(r5, 0, carry);

            k = r2 * inv;
            (_, carry) = Mac(r2, k, Modulus.u0, 0);
            (r3, carry) = Mac(r3, k, Modulus.u1, carry);
            (r4, carry) = Mac(r4, k, Modulus.u2, carry);
            (r5, carry) = Mac(r5, k, Modulus.u3, carry);
            (r6, r7) = Adc(r6, 0, carry);

            k = r3 * inv;
            (_, carry) = Mac(r3, k, Modulus.u0, 0);
            (r4, carry) = Mac(r4, k, Modulus.u1, carry);
            (r5, carry) = Mac(r5, k, Modulus.u2, carry);
            (r6, carry) = Mac(r6, k, Modulus.u3, carry);
            (r7, _) = Adc(r7, 0, carry);

            var result = new Fp(r4, r5, r6, r7);
            return result.Reduce();
        }

        public Fp Negate()
        {
            // Return p - self
            if (this == Zero) return Zero;

            ulong borrow = 0;
            (var r0, borrow) = Sbb(Modulus.u0, u0, borrow);
            (var r1, borrow) = Sbb(Modulus.u1, u1, borrow);
            (var r2, borrow) = Sbb(Modulus.u2, u2, borrow);
            (var r3, borrow) = Sbb(Modulus.u3, u3, borrow);

            return new Fp(r0, r1, r2, r3);
        }

        public Fp Square()
        {
            return this * this;
        }

        public bool TryInvert(out Fp result)
        {
            // Fermat's little theorem: a^(p-2) = a^(-1) mod p
            if (this == Zero)
            {
                result = Zero;
                return false;
            }

            // Use optimized exponentiation with BN254 modulus - 2
            // p - 2 = 0x30644e72e131a029b85045b68181585d97816a916871ca8d3c208c16d87cfd45
            result = PowVartime(new ulong[] {
                0x3c208c16d87cfd45,
                0x97816a916871ca8d,
                0xb85045b68181585d,
                0x30644e72e131a029
            });

            return true;
        }

        internal Fp PowVartime(ReadOnlySpan<ulong> exponent)
        {
            var result = One;
            var base_ = this;

            foreach (var limb in exponent)
            {
                for (int i = 0; i < 64; i++)
                {
                    if ((limb & (1UL << i)) != 0)
                    {
                        result *= base_;
                    }
                    base_ = base_.Square();
                }
            }

            return result;
        }

        public byte[] ToArray()
        {
            var result = new byte[Size];
            var span = MemoryMarshal.Cast<byte, ulong>(result.AsSpan());

            // Convert from Montgomery form
            var normalized = this * new Fp(1, 0, 0, 0);

            span[0] = normalized.u0;
            span[1] = normalized.u1;
            span[2] = normalized.u2;
            span[3] = normalized.u3;

            return result;
        }

        public bool IsZero => this == Zero;
        public bool IsOne => this == One;

        public override string ToString()
        {
            return $"0x{u3:x16}{u2:x16}{u1:x16}{u0:x16}";
        }
    }
}
