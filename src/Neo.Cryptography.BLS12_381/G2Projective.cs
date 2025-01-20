// Copyright (C) 2015-2025 The Neo Project.
//
// G2Projective.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using static Neo.Cryptography.BLS12_381.Constants;
using static Neo.Cryptography.BLS12_381.ConstantTimeUtility;
using static Neo.Cryptography.BLS12_381.G2Constants;

namespace Neo.Cryptography.BLS12_381
{
    [StructLayout(LayoutKind.Explicit, Size = Fp2.Size * 3)]
    public readonly struct G2Projective : IEquatable<G2Projective>
    {
        [FieldOffset(0)]
        public readonly Fp2 X;
        [FieldOffset(Fp2.Size)]
        public readonly Fp2 Y;
        [FieldOffset(Fp2.Size * 2)]
        public readonly Fp2 Z;

        public static readonly G2Projective Identity = new(in Fp2.Zero, in Fp2.One, in Fp2.Zero);
        public static readonly G2Projective Generator = new(in GeneratorX, in GeneratorY, in Fp2.One);

        public bool IsIdentity => Z.IsZero;
        public bool IsOnCurve => ((Y.Square() * Z) == (X.Square() * X + Z.Square() * Z * B)) | Z.IsZero; // Y^2 Z = X^3 + b Z^3

        public G2Projective(in Fp2 x, in Fp2 y, in Fp2 z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public G2Projective(in G2Affine p)
        {
            X = p.X;
            Y = p.Y;
            Z = ConditionalSelect(in Fp2.One, in Fp2.Zero, p.Infinity);
        }

        public static bool operator ==(in G2Projective a, in G2Projective b)
        {
            // Is (xz, yz, z) equal to (x'z', y'z', z') when converted to affine?

            var x1 = a.X * b.Z;
            var x2 = b.X * a.Z;

            var y1 = a.Y * b.Z;
            var y2 = b.Y * a.Z;

            var self_is_zero = a.Z.IsZero;
            var other_is_zero = b.Z.IsZero;

            return (self_is_zero & other_is_zero) // Both point at infinity
                | ((!self_is_zero) & (!other_is_zero) & x1 == x2 & y1 == y2);
            // Neither point at infinity, coordinates are the same
        }

        public static bool operator !=(in G2Projective a, in G2Projective b)
        {
            return !(a == b);
        }

        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            if (obj is not G2Projective other) return false;
            return this == other;
        }

        public bool Equals(G2Projective other)
        {
            return this == other;
        }

        public override int GetHashCode()
        {
            return X.GetHashCode() ^ Y.GetHashCode() ^ Z.GetHashCode();
        }

        public static G2Projective operator -(in G2Projective a)
        {
            return new(in a.X, -a.Y, in a.Z);
        }

        public static G2Projective operator +(in G2Projective a, in G2Projective b)
        {
            // Algorithm 7, https://eprint.iacr.org/2015/1060.pdf

            var t0 = a.X * b.X;
            var t1 = a.Y * b.Y;
            var t2 = a.Z * b.Z;
            var t3 = a.X + a.Y;
            var t4 = b.X + b.Y;
            t3 *= t4;
            t4 = t0 + t1;
            t3 -= t4;
            t4 = a.Y + a.Z;
            var x3 = b.Y + b.Z;
            t4 *= x3;
            x3 = t1 + t2;
            t4 -= x3;
            x3 = a.X + a.Z;
            var y3 = b.X + b.Z;
            x3 *= y3;
            y3 = t0 + t2;
            y3 = x3 - y3;
            x3 = t0 + t0;
            t0 = x3 + t0;
            t2 = MulBy3B(t2);
            var z3 = t1 + t2;
            t1 -= t2;
            y3 = MulBy3B(y3);
            x3 = t4 * y3;
            t2 = t3 * t1;
            x3 = t2 - x3;
            y3 *= t0;
            t1 *= z3;
            y3 = t1 + y3;
            t0 *= t3;
            z3 *= t4;
            z3 += t0;

            return new G2Projective(in x3, in y3, in z3);
        }

        public static G2Projective operator +(in G2Affine a, in G2Projective b)
        {
            return b + a;
        }

        public static G2Projective operator +(in G2Projective a, in G2Affine b)
        {
            // Algorithm 8, https://eprint.iacr.org/2015/1060.pdf

            var t0 = a.X * b.X;
            var t1 = a.Y * b.Y;
            var t3 = b.X + b.Y;
            var t4 = a.X + a.Y;
            t3 *= t4;
            t4 = t0 + t1;
            t3 -= t4;
            t4 = b.Y * a.Z;
            t4 += a.Y;
            var y3 = b.X * a.Z;
            y3 += a.X;
            var x3 = t0 + t0;
            t0 = x3 + t0;
            var t2 = MulBy3B(a.Z);
            var z3 = t1 + t2;
            t1 -= t2;
            y3 = MulBy3B(y3);
            x3 = t4 * y3;
            t2 = t3 * t1;
            x3 = t2 - x3;
            y3 *= t0;
            t1 *= z3;
            y3 = t1 + y3;
            t0 *= t3;
            z3 *= t4;
            z3 += t0;

            var tmp = new G2Projective(in x3, in y3, in z3);

            return ConditionalSelect(in tmp, in a, b.IsIdentity);
        }

        public static G2Projective operator -(in G2Projective a, in G2Projective b)
        {
            return a + -b;
        }

        public static G2Projective operator -(in G2Affine a, in G2Projective b)
        {
            return a + -b;
        }

        public static G2Projective operator -(in G2Projective a, in G2Affine b)
        {
            return a + -b;
        }

        public static G2Projective operator *(in G2Projective a, in Scalar b)
        {
            return a * b.ToArray();
        }

        public static G2Projective operator *(in G2Projective a, byte[] b)
        {
            var acc = Identity;

            // This is a simple double-and-add implementation of point
            // multiplication, moving from most significant to least
            // significant bit of the scalar.
            //
            // We skip the leading bit because it's always unset for Fq
            // elements.
            foreach (bool bit in b
                .SelectMany(p => Enumerable.Range(0, 8).Select(q => ((p >> q) & 1) == 1))
                .Reverse()
                .Skip(1))
            {
                acc = acc.Double();
                acc = ConditionalSelect(in acc, acc + a, bit);
            }

            return acc;
        }

        private static Fp2 MulBy3B(Fp2 x)
        {
            return x * B3;
        }

        public G2Projective Double()
        {
            // Algorithm 9, https://eprint.iacr.org/2015/1060.pdf

            var t0 = Y.Square();
            var z3 = t0 + t0;
            z3 += z3;
            z3 += z3;
            var t1 = Y * Z;
            var t2 = Z.Square();
            t2 = MulBy3B(t2);
            var x3 = t2 * z3;
            var y3 = t0 + t2;
            z3 = t1 * z3;
            t1 = t2 + t2;
            t2 = t1 + t2;
            t0 -= t2;
            y3 = t0 * y3;
            y3 = x3 + y3;
            t1 = X * Y;
            x3 = t0 * t1;
            x3 += x3;

            var tmp = new G2Projective(in x3, in y3, in z3);

            return ConditionalSelect(in tmp, in Identity, IsIdentity);
        }

        internal G2Projective Psi()
        {
            return new G2Projective(
                // x = frobenius(x)/((u+1)^((p-1)/3))
                X.FrobeniusMap() * PsiCoeffX,
                // y = frobenius(y)/(u+1)^((p-1)/2)
                Y.FrobeniusMap() * PsiCoeffY,
                // z = frobenius(z)
                Z.FrobeniusMap()
            );
        }

        internal G2Projective Psi2()
        {
            return new G2Projective(
                // x = frobenius^2(x)/2^((p-1)/3); note that q^2 is the order of the field.
                X * Psi2CoeffX,
                // y = -frobenius^2(y); note that q^2 is the order of the field.
                -Y,
                // z = z
                in Z
            );
        }

        internal G2Projective MulByX()
        {
            var xself = Identity;
            // NOTE: in BLS12-381 we can just skip the first bit.
            var x = BLS_X >> 1;
            var acc = this;
            while (x != 0)
            {
                acc = acc.Double();
                if (x % 2 == 1)
                {
                    xself += acc;
                }
                x >>= 1;
            }
            // finally, flip the sign
            if (BLS_X_IS_NEGATIVE)
            {
                xself = -xself;
            }
            return xself;
        }

        public G2Projective ClearCofactor()
        {
            var t1 = MulByX(); // [x] P
            var t2 = Psi(); // psi(P)

            return Double().Psi2() // psi^2(2P)
                + (t1 + t2).MulByX() // psi^2(2P) + [x^2] P + [x] psi(P)
                - t1 // psi^2(2P) + [x^2 - x] P + [x] psi(P)
                - t2 // psi^2(2P) + [x^2 - x] P + [x - 1] psi(P)
                - this; // psi^2(2P) + [x^2 - x - 1] P + [x - 1] psi(P)
        }

        public static void BatchNormalize(ReadOnlySpan<G2Projective> p, Span<G2Affine> q)
        {
            int length = p.Length;
            if (length != q.Length)
                throw new ArgumentException($"{nameof(p)} and {nameof(q)} must have the same length.");

            Span<Fp2> x = stackalloc Fp2[length];
            Fp2 acc = Fp2.One;
            for (int i = 0; i < length; i++)
            {
                // We use the `x` field of `G2Affine` to store the product
                // of previous z-coordinates seen.
                x[i] = acc;

                // We will end up skipping all identities in p
                acc = ConditionalSelect(acc * p[i].Z, in acc, p[i].IsIdentity);
            }

            // This is the inverse, as all z-coordinates are nonzero and the ones
            // that are not are skipped.
            acc = acc.Invert();

            for (int i = length - 1; i >= 0; i--)
            {
                bool skip = p[i].IsIdentity;

                // Compute tmp = 1/z
                var tmp = x[i] * acc;

                // Cancel out z-coordinate in denominator of `acc`
                acc = ConditionalSelect(acc * p[i].Z, in acc, skip);

                // Set the coordinates to the correct value
                G2Affine qi = new(p[i].X * tmp, p[i].Y * tmp);

                q[i] = ConditionalSelect(in qi, in G2Affine.Identity, skip);
            }
        }

        public static G2Projective Random(RandomNumberGenerator rng)
        {
            Span<byte> buffer = stackalloc byte[sizeof(uint)];
            while (true)
            {
                var x = Fp2.Random(rng);
                rng.GetBytes(buffer);
                var flip_sign = BinaryPrimitives.ReadUInt32LittleEndian(buffer) % 2 != 0;

                // Obtain the corresponding y-coordinate given x as y = sqrt(x^3 + 4)
                var y = ((x.Square() * x) + B).Sqrt();

                G2Affine p;
                try
                {
                    p = new G2Affine(in x, flip_sign ? -y : y);
                }
                catch
                {
                    continue;
                }

                var result = p.ToCurve().ClearCofactor();
                if (!result.IsIdentity) return result;
            }
        }
    }
}
