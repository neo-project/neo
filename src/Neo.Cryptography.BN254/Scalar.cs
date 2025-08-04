// Copyright (C) 2015-2025 The Neo Project.
//
// Scalar.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using static Neo.Cryptography.BN254.MathUtility;
using static Neo.Cryptography.BN254.ScalarConstants;

namespace Neo.Cryptography.BN254
{
    [StructLayout(LayoutKind.Explicit, Size = 32)]
    public readonly struct Scalar : INumber<Scalar>
    {
        // BN254 scalar field: r = 21888242871839275222246405745257275088548364400416034343698204186575808495617
        public const int Size = 32;

        [FieldOffset(0)] private readonly ulong u0;
        [FieldOffset(8)] private readonly ulong u1;
        [FieldOffset(16)] private readonly ulong u2;
        [FieldOffset(24)] private readonly ulong u3;

        public static ref readonly Scalar Zero => ref zero;
        public static ref readonly Scalar One => ref one;

        private static readonly Scalar zero = new();
        private static readonly Scalar one = R;

        public Scalar(ulong u0, ulong u1, ulong u2, ulong u3)
        {
            this.u0 = u0;
            this.u1 = u1;
            this.u2 = u2;
            this.u3 = u3;
        }

        public Scalar(ReadOnlySpan<ulong> data)
        {
            if (data.Length < 4)
                throw new ArgumentException($"Input must contain at least 4 ulongs, got {data.Length}");
            u0 = data[0];
            u1 = data[1];
            u2 = data[2];
            u3 = data[3];
        }

        public static Scalar FromBytes(ReadOnlySpan<byte> data)
        {
            if (data.Length != Size)
                throw new ArgumentException($"Invalid data length {data.Length}, expected {Size}");
            
            var tmp = MemoryMarshal.Cast<byte, ulong>(data);
            Scalar result = new(tmp);
            
            // Convert to Montgomery form
            result = result * R2;
            return result.Reduce();
        }

        public static Scalar FromRawUnchecked(ulong u0, ulong u1, ulong u2, ulong u3)
        {
            return new Scalar(u0, u1, u2, u3);
        }

        public static bool operator ==(in Scalar a, in Scalar b)
        {
            return a.u0 == b.u0 && a.u1 == b.u1 && a.u2 == b.u2 && a.u3 == b.u3;
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
            return HashCode.Combine(u0, u1, u2, u3);
        }

        public static Scalar operator +(in Scalar a, in Scalar b)
        {
            return Add(a, b);
        }

        public static Scalar operator -(in Scalar a, in Scalar b)
        {
            return Subtract(a, b);
        }

        public static Scalar operator *(in Scalar a, in Scalar b)
        {
            return Multiply(a, b);
        }

        public static Scalar operator -(in Scalar a)
        {
            return a.Negate();
        }

        private static Scalar Add(in Scalar a, in Scalar b)
        {
            ulong carry = 0;
            (var u0, carry) = Adc(a.u0, b.u0, carry);
            (var u1, carry) = Adc(a.u1, b.u1, carry);
            (var u2, carry) = Adc(a.u2, b.u2, carry);
            (var u3, carry) = Adc(a.u3, b.u3, carry);
            
            var result = new Scalar(u0, u1, u2, u3);
            return result.Reduce();
        }

        private static Scalar Subtract(in Scalar a, in Scalar b)
        {
            return Add(a, b.Negate());
        }

        private static Scalar Multiply(in Scalar a, in Scalar b)
        {
            // Montgomery multiplication
            return MontgomeryReduce(
                a.u0, a.u1, a.u2, a.u3,
                b.u0, b.u1, b.u2, b.u3
            );
        }

        private Scalar Reduce() 
        {
            // Compare with modulus and subtract if needed
            bool geq = u3 > MODULUS.u3 ||
                      (u3 == MODULUS.u3 && 
                       (u2 > MODULUS.u2 ||
                        (u2 == MODULUS.u2 &&
                         (u1 > MODULUS.u1 ||
                          (u1 == MODULUS.u1 && u0 >= MODULUS.u0)))));
            
            if (geq)
            {
                ulong borrow = 0;
                (var r0, borrow) = Sbb(u0, MODULUS.u0, borrow);
                (var r1, borrow) = Sbb(u1, MODULUS.u1, borrow);
                (var r2, borrow) = Sbb(u2, MODULUS.u2, borrow);
                (var r3, borrow) = Sbb(u3, MODULUS.u3, borrow);
                return new Scalar(r0, r1, r2, r3);
            }
            
            return this;
        }

        private static Scalar MontgomeryReduce(ulong r0, ulong r1, ulong r2, ulong r3, 
                                              ulong r4, ulong r5, ulong r6, ulong r7)
        {
            // Montgomery reduction using BN254 scalar field inverse: 0xf0fd797b2c3f5d5
            const ulong inv = 0xf0fd797b2c3f5d5;
            
            // Montgomery reduction steps
            ulong k = r0 * inv;
            (_, var carry) = Mac(r0, k, MODULUS.u0, 0);
            (r1, carry) = Mac(r1, k, MODULUS.u1, carry);
            (r2, carry) = Mac(r2, k, MODULUS.u2, carry);
            (r3, carry) = Mac(r3, k, MODULUS.u3, carry);
            (r4, r5) = Adc(r4, 0, carry);
            
            k = r1 * inv;
            (_, carry) = Mac(r1, k, MODULUS.u0, 0);
            (r2, carry) = Mac(r2, k, MODULUS.u1, carry);
            (r3, carry) = Mac(r3, k, MODULUS.u2, carry);
            (r4, carry) = Mac(r4, k, MODULUS.u3, carry);
            (r5, r6) = Adc(r5, 0, carry);
            
            k = r2 * inv;
            (_, carry) = Mac(r2, k, MODULUS.u0, 0);
            (r3, carry) = Mac(r3, k, MODULUS.u1, carry);
            (r4, carry) = Mac(r4, k, MODULUS.u2, carry);
            (r5, carry) = Mac(r5, k, MODULUS.u3, carry);
            (r6, r7) = Adc(r6, 0, carry);
            
            k = r3 * inv;
            (_, carry) = Mac(r3, k, MODULUS.u0, 0);
            (r4, carry) = Mac(r4, k, MODULUS.u1, carry);
            (r5, carry) = Mac(r5, k, MODULUS.u2, carry);
            (r6, carry) = Mac(r6, k, MODULUS.u3, carry);
            (r7, _) = Adc(r7, 0, carry);
            
            var result = new Scalar(r4, r5, r6, r7);
            return result.Reduce();
        }

        public Scalar Negate()
        {
            if (this == Zero) return Zero;
            
            ulong borrow = 0;
            (var r0, borrow) = Sbb(MODULUS.u0, u0, borrow);
            (var r1, borrow) = Sbb(MODULUS.u1, u1, borrow);
            (var r2, borrow) = Sbb(MODULUS.u2, u2, borrow);
            (var r3, borrow) = Sbb(MODULUS.u3, u3, borrow);
            
            return new Scalar(r0, r1, r2, r3);
        }

        public Scalar Square()
        {
            return this * this;
        }

        public bool TryInvert(out Scalar result)
        {
            if (this == Zero)
            {
                result = Zero;
                return false;
            }
            
            // Fermat's little theorem
            result = this.PowVartime(new ulong[] {
                0x43e1f593f0000001,
                0x2833e84879b97091,
                0xb85045b68181585d,
                0x30644e72e131a029
            });
            
            return true;
        }

        private Scalar PowVartime(ReadOnlySpan<ulong> exponent)
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
            var normalized = this * new Scalar(1, 0, 0, 0);
            
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

        internal ulong GetLimb(int index)
        {
            return index switch
            {
                0 => u0,
                1 => u1,
                2 => u2,
                3 => u3,
                _ => throw new ArgumentOutOfRangeException(nameof(index))
            };
        }

        internal Scalar SetLimb(int index, ulong value)
        {
            return index switch
            {
                0 => new Scalar(value, u1, u2, u3),
                1 => new Scalar(u0, value, u2, u3),
                2 => new Scalar(u0, u1, value, u3),
                3 => new Scalar(u0, u1, u2, value),
                _ => throw new ArgumentOutOfRangeException(nameof(index))
            };
        }
    }
}