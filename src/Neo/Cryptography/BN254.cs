// Copyright (C) 2015-2026 The Neo Project.
//
// BN254.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Cryptography.BN254Curve;
using System;
using System.Numerics;

namespace Neo.Cryptography
{
    /// <summary>
    /// EIP-196/EIP-197 alt_bn128 (BN254) precompile surface: G1 addition, G1 scalar
    /// multiplication, and the multi-pairing product check. Decoding follows the Ethereum
    /// big-endian encoding, rejects non-canonical field coordinates, enforces the G2 prime
    /// order subgroup, and mirrors the EVM "return zero on malformed point" convention. The
    /// underlying field and curve arithmetic is the managed <see cref="BN254Curve"/> port,
    /// so no native library is required. Beyond the precompile surface this type also exposes a
    /// target-group (GT) serialization (<see cref="SerializeGt"/>/<see cref="TryDeserializeGt"/>)
    /// with no EVM analogue, so the native contract can expose pairing results as first-class,
    /// composable values rather than a single boolean check.
    /// </summary>
    public static class BN254
    {
        public const int FieldElementLength = 32;
        public const int G1EncodedLength = 64;
        public const int G2EncodedLength = FieldElementLength * 4;
        public const int PairInputLength = G1EncodedLength + G2EncodedLength;

        /// <summary>
        /// Encoded length of a target-group (GT) element: the 12 flat-basis F_p12 coefficients,
        /// each a 32-byte big-endian canonical residue. This serialization has no EVM precompile
        /// analogue; it exists so the native contract can hand a pairing result back to a script
        /// and accept it again for equality and product composition.
        /// </summary>
        public const int GtEncodedLength = FieldElementLength * Fp12.Degree;

        public static byte[] Add(ReadOnlySpan<byte> input)
        {
            if (input.Length != G1EncodedLength * 2)
                throw new ArgumentException("Invalid BN254 add input length", nameof(input));

            if (!TryDeserializeG1(input[..G1EncodedLength], out var first))
                return new byte[G1EncodedLength];

            if (!TryDeserializeG1(input[G1EncodedLength..], out var second))
                return new byte[G1EncodedLength];

            return SerializeG1(first.Add(second));
        }

        public static byte[] Mul(ReadOnlySpan<byte> input)
        {
            if (input.Length != G1EncodedLength + FieldElementLength)
                throw new ArgumentException("Invalid BN254 mul input length", nameof(input));

            if (!TryDeserializeG1(input[..G1EncodedLength], out var basePoint))
                return new byte[G1EncodedLength];

            if (!TryDeserializeScalar(input[G1EncodedLength..], out var scalar))
                return new byte[G1EncodedLength];

            return SerializeG1(basePoint.Multiply(scalar));
        }

        public static byte[] Pairing(ReadOnlySpan<byte> input)
        {
            if (input.Length == 0)
                return SuccessWord();

            if (input.Length % PairInputLength != 0)
                throw new ArgumentException("Invalid BN254 pairing input length", nameof(input));

            int pairCount = input.Length / PairInputLength;
            var pairs = new (EcPoint<Fp> G1, EcPoint<Fp2> G2)[pairCount];

            for (int pairIndex = 0; pairIndex < pairCount; pairIndex++)
            {
                int offset = pairIndex * PairInputLength;
                var g1Slice = input.Slice(offset, G1EncodedLength);
                var g2Slice = input.Slice(offset + G1EncodedLength, G2EncodedLength);

                if (!TryDeserializeG1(g1Slice, out var g1))
                    return new byte[FieldElementLength];

                if (!TryDeserializeG2(g2Slice, out var g2))
                    return new byte[FieldElementLength];

                pairs[pairIndex] = (g1, g2);
            }

            return Bn254Pairing.PairingCheck(pairs) ? SuccessWord() : new byte[FieldElementLength];
        }

        /// <summary>
        /// Decodes a 64-byte big-endian G1 point. An all-zero encoding is the point at
        /// infinity. Coordinates that are not the canonical residue below the field modulus
        /// are rejected, matching the EVM precompiles. Because the G1 cofactor is one, the
        /// on-curve test is sufficient to guarantee subgroup membership.
        /// </summary>
        internal static bool TryDeserializeG1(ReadOnlySpan<byte> encoded, out EcPoint<Fp> point)
        {
            point = EcPoint<Fp>.Infinity;

            if (IsAllZero(encoded))
                return true;

            var x = ReadFieldElement(encoded[..FieldElementLength]);
            var y = ReadFieldElement(encoded[FieldElementLength..]);

            if (x >= Fp.Modulus || y >= Fp.Modulus)
                return false;

            point = EcPoint<Fp>.Create(Fp.FromInteger(x), Fp.FromInteger(y));
            return point.IsOnCurve(Bn254Pairing.B1);
        }

        /// <summary>
        /// Decodes a 32-byte big-endian scalar, reduced modulo the curve order. Always
        /// succeeds; the reduced value is a valid exponent for scalar multiplication.
        /// </summary>
        internal static bool TryDeserializeScalar(ReadOnlySpan<byte> encoded, out BigInteger scalar)
        {
            scalar = ReadFieldElement(encoded) % Bn254Pairing.CurveOrder;
            return true;
        }

        /// <summary>
        /// Decodes a 128-byte G2 point in EIP-197 order [x_imag][x_real][y_imag][y_real],
        /// all big-endian. An all-zero encoding is the point at infinity. Coordinates are
        /// rejected if non-canonical. The point must lie on the sextic twist and inside the
        /// prime-order subgroup; the G2 cofactor exceeds one, so the explicit subgroup check
        /// is required to prevent small-subgroup forgeries.
        /// </summary>
        internal static bool TryDeserializeG2(ReadOnlySpan<byte> encoded, out EcPoint<Fp2> point)
        {
            point = EcPoint<Fp2>.Infinity;

            if (IsAllZero(encoded))
                return true;

            var xImag = ReadFieldElement(encoded.Slice(0, FieldElementLength));
            var xReal = ReadFieldElement(encoded.Slice(FieldElementLength, FieldElementLength));
            var yImag = ReadFieldElement(encoded.Slice(2 * FieldElementLength, FieldElementLength));
            var yReal = ReadFieldElement(encoded.Slice(3 * FieldElementLength, FieldElementLength));

            if (xImag >= Fp.Modulus || xReal >= Fp.Modulus || yImag >= Fp.Modulus || yReal >= Fp.Modulus)
                return false;

            var x = new Fp2(Fp.FromInteger(xReal), Fp.FromInteger(xImag));
            var y = new Fp2(Fp.FromInteger(yReal), Fp.FromInteger(yImag));
            point = EcPoint<Fp2>.Create(x, y);

            if (!point.IsOnCurve(Bn254Pairing.B2))
            {
                point = EcPoint<Fp2>.Infinity;
                return false;
            }

            if (!point.Multiply(Bn254Pairing.CurveOrder).IsInfinity)
            {
                point = EcPoint<Fp2>.Infinity;
                return false;
            }

            return true;
        }

        /// <summary>Encodes a G1 point as 64 big-endian bytes; the point at infinity is all zero.</summary>
        internal static byte[] SerializeG1(EcPoint<Fp> point)
        {
            var output = new byte[G1EncodedLength];

            if (point.IsInfinity)
                return output;

            WriteFieldElement(point.X.Value, output.AsSpan(0, FieldElementLength));
            WriteFieldElement(point.Y.Value, output.AsSpan(FieldElementLength, FieldElementLength));
            return output;
        }

        /// <summary>
        /// Encodes a target-group element as <see cref="GtEncodedLength"/> bytes: its 12 flat-basis
        /// F_p12 coefficients in ascending degree, each a 32-byte big-endian canonical residue.
        /// </summary>
        internal static byte[] SerializeGt(Fp12 value)
        {
            var output = new byte[GtEncodedLength];
            for (int i = 0; i < Fp12.Degree; i++)
                WriteFieldElement(value.GetCoefficient(i), output.AsSpan(i * FieldElementLength, FieldElementLength));
            return output;
        }

        /// <summary>
        /// Decodes a <see cref="GtEncodedLength"/>-byte target-group element produced by
        /// <see cref="SerializeGt"/>. Each coefficient must be the canonical residue below the field
        /// modulus, otherwise decoding fails. No subgroup membership test is performed: a GT value is
        /// only ever consumed by equality against a freshly computed pairing or by field-multiplication
        /// composition, neither of which can be subverted by an out-of-subgroup encoding.
        /// </summary>
        internal static bool TryDeserializeGt(ReadOnlySpan<byte> encoded, out Fp12 value)
        {
            value = Fp12.Zero;

            if (encoded.Length != GtEncodedLength)
                return false;

            Span<BigInteger> coeffs = new BigInteger[Fp12.Degree];
            for (int i = 0; i < Fp12.Degree; i++)
            {
                var coefficient = ReadFieldElement(encoded.Slice(i * FieldElementLength, FieldElementLength));
                if (coefficient >= Fp.Modulus)
                    return false;
                coeffs[i] = coefficient;
            }

            value = Fp12.FromCoefficients(coeffs);
            return true;
        }

        private static BigInteger ReadFieldElement(ReadOnlySpan<byte> bytes)
            => new(bytes, isUnsigned: true, isBigEndian: true);

        private static void WriteFieldElement(BigInteger value, Span<byte> destination)
        {
            destination.Clear();

            Span<byte> scratch = stackalloc byte[FieldElementLength];
            if (!value.TryWriteBytes(scratch, out int written, isUnsigned: true, isBigEndian: true))
                throw new ArgumentException("BN254 coordinate does not fit in 32 bytes.");

            scratch[..written].CopyTo(destination[(FieldElementLength - written)..]);
        }

        private static bool IsAllZero(ReadOnlySpan<byte> data)
        {
            for (int i = 0; i < data.Length; i++)
                if (data[i] != 0)
                    return false;
            return true;
        }

        private static byte[] SuccessWord()
        {
            var output = new byte[FieldElementLength];
            output[^1] = 1;
            return output;
        }
    }
}
