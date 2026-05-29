// Copyright (C) 2015-2026 The Neo Project.
//
// Fp2.cs file belongs to the neo project and is free
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
    /// Element of the quadratic extension field F_p2 = F_p[u] / (u^2 + 1).
    /// Coefficients follow the py_ecc <c>FQ2</c> convention <c>[c0, c1]</c>, i.e. the
    /// element equals <c>c0 + c1 * u</c> with <c>u^2 = -1</c>. This matches the EIP-197
    /// encoding where the "real" coordinate is c0 and the "imaginary" coordinate is c1.
    /// </summary>
    internal readonly struct Fp2 : IFieldElement<Fp2>
    {
        private readonly Fp _c0;
        private readonly Fp _c1;

        public Fp2(Fp c0, Fp c1)
        {
            _c0 = c0;
            _c1 = c1;
        }

        public static Fp2 FromIntegers(BigInteger c0, BigInteger c1)
            => new(Fp.FromInteger(c0), Fp.FromInteger(c1));

        /// <summary>The real coordinate (py_ecc coeffs[0]).</summary>
        public Fp C0 => _c0;

        /// <summary>The imaginary coordinate (py_ecc coeffs[1]).</summary>
        public Fp C1 => _c1;

        public static Fp2 Zero => new(Fp.Zero, Fp.Zero);

        public static Fp2 One => new(Fp.One, Fp.Zero);

        public bool IsZero => _c0.IsZero && _c1.IsZero;

        public Fp2 Add(Fp2 other) => new(_c0.Add(other._c0), _c1.Add(other._c1));

        public Fp2 Subtract(Fp2 other) => new(_c0.Subtract(other._c0), _c1.Subtract(other._c1));

        // (a0 + a1 u)(b0 + b1 u) = (a0 b0 - a1 b1) + (a0 b1 + a1 b0) u, since u^2 = -1.
        public Fp2 Multiply(Fp2 other)
        {
            var realPart = _c0.Multiply(other._c0).Subtract(_c1.Multiply(other._c1));
            var imagPart = _c0.Multiply(other._c1).Add(_c1.Multiply(other._c0));
            return new Fp2(realPart, imagPart);
        }

        public Fp2 Negate() => new(_c0.Negate(), _c1.Negate());

        // (a0 + a1 u)^-1 = (a0 - a1 u) / (a0^2 + a1^2); the norm a0^2 + a1^2 lies in F_p.
        public Fp2 Inverse()
        {
            var norm = _c0.Multiply(_c0).Add(_c1.Multiply(_c1));
            var normInv = norm.Inverse();
            return new Fp2(_c0.Multiply(normInv), _c1.Negate().Multiply(normInv));
        }

        public bool Equals(Fp2 other) => _c0.Equals(other._c0) && _c1.Equals(other._c1);

        public override bool Equals(object? obj) => obj is Fp2 other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(_c0, _c1);
    }
}
