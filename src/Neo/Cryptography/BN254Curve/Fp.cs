// Copyright (C) 2015-2026 The Neo Project.
//
// Fp.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

#nullable enable

using System;
using System.Numerics;

namespace Neo.Cryptography.BN254Curve
{
    /// <summary>
    /// Common contract for the BN254 field tower elements (the base prime field
    /// <see cref="Fp"/> and the extension fields <see cref="Fp2"/> and <see cref="Fp12"/>).
    /// The generic <see cref="EcPoint{T}"/> and the pairing routines operate against this
    /// abstraction so a single tested implementation of the curve laws serves every field.
    /// </summary>
    /// <typeparam name="T">The concrete field element type.</typeparam>
    internal interface IFieldElement<T> : IEquatable<T> where T : IFieldElement<T>
    {
        static abstract T Zero { get; }
        static abstract T One { get; }

        T Add(T other);
        T Subtract(T other);
        T Multiply(T other);
        T Negate();
        T Inverse();
        bool IsZero { get; }
    }

    /// <summary>
    /// Element of the BN254 base prime field F_p, where
    /// p = 21888242871839275222246405745257275088696311157297823662689037894645226208583.
    /// Values are always stored as the canonical representative in the range [0, p).
    /// </summary>
    internal readonly struct Fp : IFieldElement<Fp>
    {
        /// <summary>The BN254 base field modulus p.</summary>
        public static readonly BigInteger Modulus = BigInteger.Parse(
            "21888242871839275222246405745257275088696311157297823662689037894645226208583");

        private readonly BigInteger _value;

        private Fp(BigInteger value)
        {
            _value = value;
        }

        /// <summary>The canonical integer representative in [0, p).</summary>
        public BigInteger Value => _value;

        public static Fp Zero => new(BigInteger.Zero);

        public static Fp One => new(BigInteger.One);

        public bool IsZero => _value.IsZero;

        /// <summary>
        /// Reduces an arbitrary integer into [0, p) and wraps it as an <see cref="Fp"/>.
        /// </summary>
        public static Fp FromInteger(BigInteger value)
        {
            var reduced = value % Modulus;
            if (reduced.Sign < 0)
                reduced += Modulus;
            return new Fp(reduced);
        }

        public Fp Add(Fp other)
        {
            var sum = _value + other._value;
            if (sum >= Modulus)
                sum -= Modulus;
            return new Fp(sum);
        }

        public Fp Subtract(Fp other)
        {
            var diff = _value - other._value;
            if (diff.Sign < 0)
                diff += Modulus;
            return new Fp(diff);
        }

        public Fp Multiply(Fp other) => new((_value * other._value) % Modulus);

        public Fp Negate() => _value.IsZero ? this : new Fp(Modulus - _value);

        /// <summary>
        /// Multiplicative inverse via Fermat's little theorem (a^(p-2) mod p).
        /// p is prime, so this is well defined for every non-zero element.
        /// </summary>
        public Fp Inverse()
        {
            if (_value.IsZero)
                throw new InvalidOperationException("Cannot invert the zero element of F_p.");
            return new Fp(BigInteger.ModPow(_value, Modulus - 2, Modulus));
        }

        public bool Equals(Fp other) => _value == other._value;

        public override bool Equals(object? obj) => obj is Fp other && Equals(other);

        public override int GetHashCode() => _value.GetHashCode();
    }
}
