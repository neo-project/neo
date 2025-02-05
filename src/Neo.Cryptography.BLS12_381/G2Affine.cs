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

using System.Diagnostics.CodeAnalysis;
using static Neo.Cryptography.BLS12_381.ConstantTimeUtility;
using static Neo.Cryptography.BLS12_381.G2Constants;

namespace Neo.Cryptography.BLS12_381
{
    public readonly struct G2Affine : IEquatable<G2Affine>
    {
        public readonly Fp2 X;
        public readonly Fp2 Y;
        public readonly bool Infinity;

        public static readonly G2Affine Identity = new(in Fp2.Zero, in Fp2.One, true);
        public static readonly G2Affine Generator = new(in GeneratorX, in GeneratorY, false);

        public bool IsIdentity => Infinity;
        public bool IsTorsionFree
        {
            get
            {
                // Algorithm from Section 4 of https://eprint.iacr.org/2021/1130
                // Updated proof of correctness in https://eprint.iacr.org/2022/352
                //
                // Check that psi(P) == [x] P
                var p = new G2Projective(this);
                return p.Psi() == p.MulByX();
            }
        }
        public bool IsOnCurve => ((Y.Square() - X.Square() * X) == B) | Infinity; // y^2 - x^3 ?= 4(u + 1)

        public G2Affine(in Fp2 x, in Fp2 y)
            : this(in x, in y, false)
        {
        }

        private G2Affine(in Fp2 x, in Fp2 y, bool infinity)
        {
            X = x;
            Y = y;
            Infinity = infinity;
        }

        public G2Affine(in G2Projective p)
        {
            bool s = p.Z.TryInvert(out Fp2 zinv);

            zinv = ConditionalSelect(in Fp2.Zero, in zinv, s);
            Fp2 x = p.X * zinv;
            Fp2 y = p.Y * zinv;

            G2Affine tmp = new(in x, in y, false);
            this = ConditionalSelect(in tmp, in Identity, !s);
        }

        public static bool operator ==(in G2Affine a, in G2Affine b)
        {
            // The only cases in which two points are equal are
            // 1. infinity is set on both
            // 2. infinity is not set on both, and their coordinates are equal

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
            return X.GetHashCode() ^ Y.GetHashCode();
        }

        public static G2Affine operator -(in G2Affine a)
        {
            return new G2Affine(
                in a.X,
                ConditionalSelect(-a.Y, in Fp2.One, a.Infinity),
                a.Infinity
            );
        }

        public static G2Projective operator *(in G2Affine a, in Scalar b)
        {
            return new G2Projective(a) * b.ToArray();
        }

        public byte[] ToCompressed()
        {
            // Strictly speaking, self.x is zero already when self.infinity is true, but
            // to guard against implementation mistakes we do not assume this.
            var x = ConditionalSelect(in X, in Fp2.Zero, Infinity);

            var res = new byte[96];

            x.C1.TryWrite(res.AsSpan(0..48));
            x.C0.TryWrite(res.AsSpan(48..96));

            // This point is in compressed form, so we set the most significant bit.
            res[0] |= 0x80;

            // Is this point at infinity? If so, set the second-most significant bit.
            res[0] |= ConditionalSelect((byte)0, (byte)0x40, Infinity);

            // Is the y-coordinate the lexicographically largest of the two associated with the
            // x-coordinate? If so, set the third-most significant bit so long as this is not
            // the point at infinity.
            res[0] |= ConditionalSelect((byte)0, (byte)0x20, !Infinity & Y.LexicographicallyLargest());

            return res;
        }

        public byte[] ToUncompressed()
        {
            var res = new byte[192];

            var x = ConditionalSelect(in X, in Fp2.Zero, Infinity);
            var y = ConditionalSelect(in Y, in Fp2.Zero, Infinity);

            x.C1.TryWrite(res.AsSpan(0..48));
            x.C0.TryWrite(res.AsSpan(48..96));
            y.C1.TryWrite(res.AsSpan(96..144));
            y.C0.TryWrite(res.AsSpan(144..192));

            // Is this point at infinity? If so, set the second-most significant bit.
            res[0] |= ConditionalSelect((byte)0, (byte)0x40, Infinity);

            return res;
        }

        public static G2Affine FromUncompressed(ReadOnlySpan<byte> bytes)
        {
            return FromBytes(bytes, false, true);
        }

        public static G2Affine FromCompressed(ReadOnlySpan<byte> bytes)
        {
            return FromBytes(bytes, true, true);
        }

        private static G2Affine FromBytes(ReadOnlySpan<byte> bytes, bool compressed, bool check)
        {
            // Obtain the three flags from the start of the byte sequence
            bool compression_flag_set = (bytes[0] & 0x80) != 0;
            bool infinity_flag_set = (bytes[0] & 0x40) != 0;
            bool sort_flag_set = (bytes[0] & 0x20) != 0;

            // Attempt to obtain the x-coordinate
            var tmp = bytes[0..48].ToArray();
            tmp[0] &= 0b0001_1111;
            var xc1 = Fp.FromBytes(tmp);
            var xc0 = Fp.FromBytes(bytes[48..96]);
            var x = new Fp2(in xc0, in xc1);

            if (compressed)
            {
                // Recover a y-coordinate given x by y = sqrt(x^3 + 4)
                var y = ((x.Square() * x) + B).Sqrt();
                y = ConditionalSelect(in y, -y, y.LexicographicallyLargest() ^ sort_flag_set);
                G2Affine result = new(in x, in y, infinity_flag_set);
                result = ConditionalSelect(in result, in Identity, infinity_flag_set);
                if (check)
                {
                    bool _checked = (!infinity_flag_set | (infinity_flag_set & !sort_flag_set & x.IsZero))
                        & compression_flag_set;
                    _checked &= result.IsTorsionFree;
                    if (!_checked) throw new FormatException();
                }
                return result;
            }
            else
            {
                // Attempt to obtain the y-coordinate
                var yc1 = Fp.FromBytes(bytes[96..144]);
                var yc0 = Fp.FromBytes(bytes[144..192]);
                var y = new Fp2(in yc0, in yc1);

                // Create a point representing this value
                var p = ConditionalSelect(new G2Affine(in x, in y, infinity_flag_set), in Identity, infinity_flag_set);

                if (check)
                {
                    bool _checked =
                        // If the infinity flag is set, the x and y coordinates should have been zero.
                        ((!infinity_flag_set) | (infinity_flag_set & x.IsZero & y.IsZero)) &
                        // The compression flag should not have been set, as this is an uncompressed element
                        (!compression_flag_set) &
                        // The sort flag should not have been set, as this is an uncompressed element
                        (!sort_flag_set);
                    _checked &= p.IsOnCurve & p.IsTorsionFree;
                    if (!_checked) throw new FormatException();
                }

                return p;
            }
        }

        public G2Projective ToCurve()
        {
            return new(this);
        }
    }
}
