// Copyright (C) 2015-2026 The Neo Project.
//
// Fp12.cs file belongs to the neo project and is free
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
    /// Element of the degree-12 extension field used by the BN254 pairing. This is the flat
    /// representation from py_ecc: F_p[X] / (X^12 - 18*X^6 + 82), i.e. the reduction relation
    /// is X^12 ≡ 18*X^6 - 82. Coefficients are stored low-degree first as canonical residues
    /// in [0, p). Multiplication is schoolbook followed by the py_ecc reduction loop; inversion
    /// solves the F_p-linear "multiply by this" system via Gaussian elimination.
    /// </summary>
    internal readonly struct Fp12 : IFieldElement<Fp12>
    {
        public const int Degree = 12;

        // py_ecc fq12_modulus_coeffs = (82, 0, 0, 0, 0, 0, -18, 0, 0, 0, 0, 0), reduced mod p.
        // The reduction relation is X^Degree ≡ -Σ modulusCoeffs[i] * X^i.
        private static readonly BigInteger[] s_modulusCoeffs = BuildModulusCoeffs();

        private readonly BigInteger[] _coeffs; // length == Degree, each in [0, p)

        private Fp12(BigInteger[] coeffs)
        {
            _coeffs = coeffs;
        }

        public static Fp12 FromCoefficients(ReadOnlySpan<BigInteger> coeffs)
        {
            if (coeffs.Length != Degree)
                throw new ArgumentException($"Fp12 requires exactly {Degree} coefficients.", nameof(coeffs));

            var reduced = new BigInteger[Degree];
            for (int i = 0; i < Degree; i++)
            {
                var v = coeffs[i] % Fp.Modulus;
                if (v.Sign < 0)
                    v += Fp.Modulus;
                reduced[i] = v;
            }
            return new Fp12(reduced);
        }

        public static Fp12 Zero => new(NewCoeffs());

        public static Fp12 One
        {
            get
            {
                var c = NewCoeffs();
                c[0] = BigInteger.One;
                return new Fp12(c);
            }
        }

        public bool IsZero
        {
            get
            {
                for (int i = 0; i < Degree; i++)
                    if (!_coeffs[i].IsZero)
                        return false;
                return true;
            }
        }

        /// <summary>
        /// Returns the canonical residue of the coefficient of X^<paramref name="index"/>, in [0, p).
        /// Used to serialize a target-group element as its 12 flat-basis coordinates.
        /// </summary>
        public BigInteger GetCoefficient(int index) => _coeffs[index];

        public Fp12 Add(Fp12 other)
        {
            var c = new BigInteger[Degree];
            for (int i = 0; i < Degree; i++)
            {
                var s = _coeffs[i] + other._coeffs[i];
                if (s >= Fp.Modulus)
                    s -= Fp.Modulus;
                c[i] = s;
            }
            return new Fp12(c);
        }

        public Fp12 Subtract(Fp12 other)
        {
            var c = new BigInteger[Degree];
            for (int i = 0; i < Degree; i++)
            {
                var d = _coeffs[i] - other._coeffs[i];
                if (d.Sign < 0)
                    d += Fp.Modulus;
                c[i] = d;
            }
            return new Fp12(c);
        }

        public Fp12 Negate()
        {
            var c = new BigInteger[Degree];
            for (int i = 0; i < Degree; i++)
                c[i] = _coeffs[i].IsZero ? BigInteger.Zero : Fp.Modulus - _coeffs[i];
            return new Fp12(c);
        }

        public Fp12 Multiply(Fp12 other) => new(MultiplyReduce(_coeffs, other._coeffs));

        /// <summary>Square-and-multiply exponentiation by a non-negative integer.</summary>
        public Fp12 Pow(BigInteger exponent)
        {
            if (exponent.Sign < 0)
                throw new ArgumentOutOfRangeException(nameof(exponent), "Exponent must be non-negative.");

            var result = One;
            var baseValue = this;
            var e = exponent;
            while (e.Sign > 0)
            {
                if (!(e & BigInteger.One).IsZero)
                    result = result.Multiply(baseValue);
                e >>= 1;
                if (e.Sign > 0)
                    baseValue = baseValue.Multiply(baseValue);
            }
            return result;
        }

        /// <summary>
        /// Inverts the element by solving (multiply-by-this) * x = 1 over F_p. The map
        /// v -> this * v is F_p-linear; its matrix columns are this*X^j reduced. Gaussian
        /// elimination on [M | e0] yields the coefficient vector of the inverse.
        /// </summary>
        public Fp12 Inverse()
        {
            if (IsZero)
                throw new InvalidOperationException("Cannot invert the zero element of F_p12.");

            // Augmented matrix: rows of length Degree + 1; column Degree holds the target e0.
            var m = new BigInteger[Degree][];
            for (int i = 0; i < Degree; i++)
            {
                m[i] = new BigInteger[Degree + 1];
                m[i][Degree] = i == 0 ? BigInteger.One : BigInteger.Zero;
            }

            for (int j = 0; j < Degree; j++)
            {
                var column = ShiftReduce(_coeffs, j); // this * X^j, reduced
                for (int i = 0; i < Degree; i++)
                    m[i][j] = column[i];
            }

            var p = Fp.Modulus;
            for (int col = 0; col < Degree; col++)
            {
                int pivot = -1;
                for (int row = col; row < Degree; row++)
                {
                    if (!m[row][col].IsZero)
                    {
                        pivot = row;
                        break;
                    }
                }
                if (pivot < 0)
                    throw new InvalidOperationException("F_p12 inversion failed: singular system.");

                if (pivot != col)
                    (m[col], m[pivot]) = (m[pivot], m[col]);

                var inv = BigInteger.ModPow(m[col][col], p - 2, p);
                for (int k = col; k <= Degree; k++)
                    m[col][k] = (m[col][k] * inv) % p;

                for (int row = 0; row < Degree; row++)
                {
                    if (row == col || m[row][col].IsZero)
                        continue;
                    var factor = m[row][col];
                    for (int k = col; k <= Degree; k++)
                    {
                        var sub = m[row][k] - factor * m[col][k];
                        sub %= p;
                        if (sub.Sign < 0)
                            sub += p;
                        m[row][k] = sub;
                    }
                }
            }

            var result = new BigInteger[Degree];
            for (int i = 0; i < Degree; i++)
                result[i] = m[i][Degree];
            return new Fp12(result);
        }

        public bool Equals(Fp12 other)
        {
            for (int i = 0; i < Degree; i++)
                if (_coeffs[i] != other._coeffs[i])
                    return false;
            return true;
        }

        public override bool Equals(object? obj) => obj is Fp12 other && Equals(other);

        public override int GetHashCode()
        {
            var hash = new HashCode();
            for (int i = 0; i < Degree; i++)
                hash.Add(_coeffs[i]);
            return hash.ToHashCode();
        }

        private static BigInteger[] NewCoeffs()
        {
            var c = new BigInteger[Degree];
            for (int i = 0; i < Degree; i++)
                c[i] = BigInteger.Zero;
            return c;
        }

        // Schoolbook product of two degree-<Degree polynomials, then reduce mod the field
        // polynomial using X^Degree ≡ -Σ modulusCoeffs[i] X^i (py_ecc __mul__ reduction).
        private static BigInteger[] MultiplyReduce(BigInteger[] a, BigInteger[] b)
        {
            var p = Fp.Modulus;
            var prod = new BigInteger[Degree * 2 - 1];
            for (int i = 0; i < prod.Length; i++)
                prod[i] = BigInteger.Zero;

            for (int i = 0; i < Degree; i++)
            {
                if (a[i].IsZero)
                    continue;
                for (int j = 0; j < Degree; j++)
                {
                    if (b[j].IsZero)
                        continue;
                    prod[i + j] = (prod[i + j] + a[i] * b[j]) % p;
                }
            }

            return Reduce(prod, p);
        }

        // Returns (coeffs * X^shift) reduced into Degree coefficients.
        private static BigInteger[] ShiftReduce(BigInteger[] coeffs, int shift)
        {
            if (shift == 0)
            {
                var copy = new BigInteger[Degree];
                Array.Copy(coeffs, copy, Degree);
                return copy;
            }

            var p = Fp.Modulus;
            var prod = new BigInteger[Degree * 2 - 1];
            for (int i = 0; i < prod.Length; i++)
                prod[i] = BigInteger.Zero;
            for (int i = 0; i < Degree; i++)
                prod[i + shift] = coeffs[i];

            return Reduce(prod, p);
        }

        // Reduce a polynomial of length (2*Degree - 1) modulo X^Degree ≡ -Σ modulusCoeffs[i] X^i.
        private static BigInteger[] Reduce(BigInteger[] prod, BigInteger p)
        {
            for (int h = prod.Length - 1; h >= Degree; h--)
            {
                var top = prod[h];
                if (top.IsZero)
                    continue;
                prod[h] = BigInteger.Zero;
                int exp = h - Degree;
                for (int i = 0; i < Degree; i++)
                {
                    if (s_modulusCoeffs[i].IsZero)
                        continue;
                    var sub = prod[exp + i] - top * s_modulusCoeffs[i];
                    sub %= p;
                    if (sub.Sign < 0)
                        sub += p;
                    prod[exp + i] = sub;
                }
            }

            var result = new BigInteger[Degree];
            Array.Copy(prod, result, Degree);
            return result;
        }

        private static BigInteger[] BuildModulusCoeffs()
        {
            var coeffs = new BigInteger[Degree];
            for (int i = 0; i < Degree; i++)
                coeffs[i] = BigInteger.Zero;
            coeffs[0] = new BigInteger(82);
            coeffs[6] = Fp.Modulus - 18; // -18 mod p
            return coeffs;
        }
    }
}
