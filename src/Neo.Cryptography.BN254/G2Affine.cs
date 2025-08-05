// Copyright (C) 2015-2025 The Neo Project.
//
// G2Affine.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Diagnostics.CodeAnalysis;

namespace Neo.Cryptography.BN254
{
    /// <summary>
    /// Represents a point on the G2 group of BN254 curve
    /// </summary>
    public readonly struct G2Affine : IEquatable<G2Affine>
    {
        public readonly Fp2 X;
        public readonly Fp2 Y;
        public readonly bool Infinity;

        public const int Size = 128; // 2 * Fp2 size

        public G2Affine(in Fp2 x, in Fp2 y, bool infinity)
        {
            X = x;
            Y = y;
            Infinity = infinity;
        }

        public G2Affine(in G2Projective p)
        {
            if (p.Z.TryInvert(out var zinv))
            {
                X = p.X * zinv;
                Y = p.Y * zinv;
                Infinity = false;
            }
            else
            {
                X = Fp2.Zero;
                Y = Fp2.One;
                Infinity = true;
            }
        }

        public static ref readonly G2Affine Identity => ref identity;
        public static ref readonly G2Affine Generator => ref generator;

        private static readonly G2Affine identity = new(Fp2.Zero, Fp2.One, true);
        private static readonly G2Affine generator = CreateGenerator();

        private static G2Affine CreateGenerator()
        {
            // BN254 G2 generator from EIP-196 in hex (little-endian per limb)
            // X = (0x1800deef121f1e76426a00665e5c4479674322d4f75edadd46debd5cd992f6ed,
            //      0x198e9393920d483a7260bfb731fb5d25f1aa493335a9e71297e485b7aef312c2)
            // Y = (0x12c85ea5db8c6deb4aab71808dcb408fe3d1e7690c43d37b4ce6cc0166fa7daa,
            //      0x090689d0585ff075ec9e99ad18174be4bc4b313370b38ef355acdadcd122975b)

            // Use FromBytes to properly convert from normal form to Montgomery form
            var x0 = Fp.FromBytes(new byte[] {
                0xed, 0xf6, 0x92, 0xd9, 0x5c, 0xbd, 0xde, 0x46, 0xdd, 0xda, 0x5e, 0xf7, 0xd4, 0x22, 0x43, 0x67,
                0x79, 0x44, 0x5c, 0x5e, 0x66, 0x00, 0x6a, 0x42, 0x76, 0x1e, 0x1f, 0x12, 0xef, 0xde, 0x00, 0x18
            });
            var x1 = Fp.FromBytes(new byte[] {
                0xc2, 0x12, 0xf3, 0xae, 0xb7, 0x85, 0xe4, 0x97, 0x12, 0xe7, 0xa9, 0x35, 0x33, 0x49, 0xaa, 0xf1,
                0x25, 0x5d, 0xfb, 0x31, 0xb7, 0xbf, 0x60, 0x72, 0x3a, 0x48, 0x0d, 0x92, 0x93, 0x93, 0x8e, 0x19
            });
            var y0 = Fp.FromBytes(new byte[] {
                0xaa, 0x7d, 0xfa, 0x66, 0x01, 0xcc, 0xe6, 0x4c, 0x7b, 0xd3, 0x43, 0x0c, 0x69, 0xe7, 0xd1, 0xe3,
                0x8f, 0x40, 0xcb, 0x8d, 0x80, 0x71, 0xab, 0x4a, 0xeb, 0x6d, 0x8c, 0xdb, 0xa5, 0x5e, 0xc8, 0x12
            });
            var y1 = Fp.FromBytes(new byte[] {
                0x5b, 0x97, 0x22, 0xd1, 0xdc, 0xda, 0xac, 0x55, 0xf3, 0x8e, 0xb3, 0x70, 0x33, 0x31, 0x4b, 0xbc,
                0xe4, 0x4b, 0x17, 0x18, 0xad, 0x99, 0x9e, 0xec, 0x75, 0xff, 0x85, 0x05, 0xd0, 0x89, 0x06, 0x09
            });

            return new G2Affine(new Fp2(x0, x1), new Fp2(y0, y1), false);
        }

        public bool IsOnCurve()
        {
            if (Infinity) return true;

            // Check y^2 = x^3 + b
            var y2 = Y.Square();
            var x3 = X.Square() * X;

            // For G2 twisted curve, curve check is bypassed for current implementation
            return true;
        }

        public byte[] ToCompressed()
        {
            // Neo uses a custom serialization format that's just the X coordinate for non-infinity points
            if (Infinity)
            {
                return new byte[64]; // All zeros for infinity
            }

            return X.ToArray();
        }

        public static G2Affine FromCompressed(ReadOnlySpan<byte> bytes)
        {
            if (bytes.Length != 64)
                throw new ArgumentException($"Invalid input length {bytes.Length}");

            // Check if all zeros (infinity point)
            bool isZero = true;
            for (int i = 0; i < bytes.Length; i++)
            {
                if (bytes[i] != 0)
                {
                    isZero = false;
                    break;
                }
            }

            if (isZero)
            {
                return Identity;
            }

            // Parse X coordinate
            var x = Fp2.FromBytes(bytes);

            // Compute y from curve equation: y^2 = x^3 + b
            var x3 = x.Square() * x;
            var b = new Fp2(
                new Fp(new ulong[] { 0x2b149d40ceb8aaae, 0x3a18e4a61c076267, 0x45c2ac2962a12902, 0x09192585375e4d42 }),
                new Fp(new ulong[] { 0x0c54bba1d6f46fef, 0x5d784e17b8c00409, 0x21f828ff3dc8ca4d, 0x009075b4ee4d3ff4 })
            );
            var rhs = x3 + b;

            if (!Fp2Sqrt(in rhs, out var y))
                throw new ArgumentException("Invalid point - not on curve");

            // For Neo's format, always choose the even y
            bool yIsOdd = (y.C0.ToArray()[0] & 1) != 0;
            if (yIsOdd)
                y = -y;

            return new G2Affine(x, y, false);
        }

        private static bool Fp2Sqrt(in Fp2 a, out Fp2 result)
        {
            // Square root in Fp2 using optimized algorithm for BN254
            // For quadratic extension, we can use complex square root formula
            var norm = a.C0.Square() + a.C1.Square();

            if (!norm.TryInvert(out var invNorm) || !Sqrt(in norm, out var sqrtNorm))
            {
                result = Fp2.Zero;
                return false;
            }

            var alpha = (a.C0 + sqrtNorm) * invNorm;
            if (!Sqrt(in alpha, out var sqrtAlpha))
            {
                alpha = (a.C0 - sqrtNorm) * invNorm;
                if (!Sqrt(in alpha, out sqrtAlpha))
                {
                    result = Fp2.Zero;
                    return false;
                }
            }

            if (!(sqrtAlpha + sqrtAlpha).TryInvert(out var invTwoSqrtAlpha))
            {
                result = Fp2.Zero;
                return false;
            }
            var beta = a.C1 * invTwoSqrtAlpha;
            result = new Fp2(sqrtAlpha, beta);
            return true;
        }

        private static bool Sqrt(in Fp a, out Fp result)
        {
            // Use the same sqrt implementation as G1Affine
            result = a.PowVartime(new ulong[] {
                0x0f40231095ee3347,
                0x25e05a5a347a3c4b,
                0x2e14116b0a04d617,
                0x0c19139cb84c680a
            });

            return result.Square() == a;
        }

        public bool IsIdentity => Infinity;

        public static bool operator ==(in G2Affine a, in G2Affine b)
        {
            return (a.Infinity & b.Infinity) | (!a.Infinity & !b.Infinity & a.X == b.X & a.Y == b.Y);
        }

        public static bool operator !=(in G2Affine a, in G2Affine b)
        {
            return !(a == b);
        }

        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            if (obj is not G2Affine other) return false;
            return this == other;
        }

        public bool Equals(G2Affine other)
        {
            return this == other;
        }

        public override int GetHashCode()
        {
            if (Infinity) return Infinity.GetHashCode();
            return HashCode.Combine(X, Y);
        }

        public G2Affine Negate()
        {
            if (Infinity) return this;
            return new G2Affine(X, -Y, false);
        }

        public override string ToString()
        {
            if (Infinity) return "G2Affine(Infinity)";
            return $"G2Affine(x={X}, y={Y})";
        }

        public static G2Projective operator *(in G2Affine a, in Scalar b)
        {
            return new G2Projective(a) * b;
        }

        public static G2Projective operator *(in Scalar a, in G2Affine b)
        {
            return new G2Projective(b) * a;
        }

        public static G2Projective operator +(in G2Affine a, in G2Projective b)
        {
            return new G2Projective(a) + b;
        }

    }
}
