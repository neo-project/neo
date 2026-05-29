// Copyright (C) 2015-2026 The Neo Project.
//
// EcPoint.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Numerics;

namespace Neo.Cryptography.BN254Curve
{
    /// <summary>
    /// Affine point on a short Weierstrass curve y^2 = x^3 + b over a BN254 tower field
    /// <typeparamref name="T"/>. The default value represents the point at infinity
    /// (the group identity). The group laws are a direct port of py_ecc's bn128 curve
    /// arithmetic and are shared across F_p (G1), F_p2 (G2) and F_p12 (the pairing target).
    /// </summary>
    /// <typeparam name="T">The coordinate field element type.</typeparam>
    internal readonly struct EcPoint<T> where T : IFieldElement<T>
    {
        private readonly bool _isFinite;

        public T X { get; }
        public T Y { get; }

        private EcPoint(T x, T y)
        {
            X = x;
            Y = y;
            _isFinite = true;
        }

        /// <summary>The point at infinity (group identity).</summary>
        public static EcPoint<T> Infinity => default;

        public bool IsInfinity => !_isFinite;

        public static EcPoint<T> Create(T x, T y) => new(x, y);

        /// <summary>Tests y^2 - x^3 == b. The point at infinity is always considered on-curve.</summary>
        public bool IsOnCurve(T b)
        {
            if (IsInfinity)
                return true;
            var lhs = Y.Multiply(Y).Subtract(X.Multiply(X).Multiply(X));
            return lhs.Equals(b);
        }

        public EcPoint<T> Negate()
        {
            if (IsInfinity)
                return this;
            return new EcPoint<T>(X, Y.Negate());
        }

        // Tangent doubling: m = 3x^2 / 2y; x' = m^2 - 2x; y' = m(x - x') - y.
        public EcPoint<T> Double()
        {
            if (IsInfinity)
                return this;

            var x = X;
            var y = Y;
            var xSquared = x.Multiply(x);
            var threeXSquared = xSquared.Add(xSquared).Add(xSquared);
            var twoY = y.Add(y);
            var m = threeXSquared.Multiply(twoY.Inverse());
            var newX = m.Multiply(m).Subtract(x).Subtract(x);
            var newY = m.Multiply(x.Subtract(newX)).Subtract(y);
            return new EcPoint<T>(newX, newY);
        }

        // Chord addition; equal x with equal y doubles, equal x with unequal y is the
        // vertical line and yields the point at infinity.
        public EcPoint<T> Add(EcPoint<T> other)
        {
            if (IsInfinity)
                return other;
            if (other.IsInfinity)
                return this;

            var x1 = X;
            var y1 = Y;
            var x2 = other.X;
            var y2 = other.Y;

            if (x1.Equals(x2))
            {
                if (y1.Equals(y2))
                    return Double();
                return Infinity;
            }

            var m = y2.Subtract(y1).Multiply(x2.Subtract(x1).Inverse());
            var newX = m.Multiply(m).Subtract(x1).Subtract(x2);
            var newY = m.Multiply(x1.Subtract(newX)).Subtract(y1);
            return new EcPoint<T>(newX, newY);
        }

        /// <summary>Scalar multiplication by a non-negative integer (double-and-add).</summary>
        public EcPoint<T> Multiply(BigInteger scalar)
        {
            if (scalar.Sign < 0)
                throw new ArgumentOutOfRangeException(nameof(scalar), "Scalar must be non-negative.");

            var result = Infinity;
            var addend = this;
            var k = scalar;
            while (k.Sign > 0)
            {
                if (!(k & BigInteger.One).IsZero)
                    result = result.Add(addend);
                addend = addend.Double();
                k >>= 1;
            }
            return result;
        }
    }
}
