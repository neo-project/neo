// Copyright (C) 2015-2025 The Neo Project.
//
// CryptoLib.BLS12_381.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Cryptography.BLS12_381;
using Neo.VM.Types;
using System;
using Array = Neo.VM.Types.Array;
using VMBuffer = Neo.VM.Types.Buffer;

namespace Neo.SmartContract.Native
{
    partial class CryptoLib
    {
        private const int Bls12381MultiExpMaxPairs = 128;
        private const int Bls12FieldElementLength = 64;
        private const int Bls12ScalarLength = Scalar.Size;
        private const int Bls12G1EncodedLength = Bls12FieldElementLength * 2;
        private const int Bls12G2EncodedLength = Bls12FieldElementLength * 4;
        private const int Bls12PairInputLength = Bls12G1EncodedLength + Bls12G2EncodedLength;

        /// <summary>
        /// Serialize a bls12381 point.
        /// </summary>
        /// <param name="g">The point to be serialized.</param>
        /// <returns></returns>
        [ContractMethod(CpuFee = 1 << 19)]
        public static byte[] Bls12381Serialize(InteropInterface g)
        {
            return g.GetInterface<object>() switch
            {
                G1Affine p => p.ToCompressed(),
                G1Projective p => new G1Affine(p).ToCompressed(),
                G2Affine p => p.ToCompressed(),
                G2Projective p => new G2Affine(p).ToCompressed(),
                Gt p => p.ToArray(),
                _ => throw new ArgumentException("BLS12-381 type mismatch")
            };
        }

        /// <summary>
        /// Deserialize a bls12381 point.
        /// </summary>
        /// <param name="data">The point as byte array.</param>
        /// <returns></returns>
        [ContractMethod(CpuFee = 1 << 19)]
        public static InteropInterface Bls12381Deserialize(byte[] data)
        {
            return data.Length switch
            {
                48 => new InteropInterface(G1Affine.FromCompressed(data)),
                96 => new InteropInterface(G2Affine.FromCompressed(data)),
                576 => new InteropInterface(Gt.FromBytes(data)),
                _ => throw new ArgumentException("Invalid BLS12-381 point length"),
            };
        }

        /// <summary>
        /// Determines whether the specified points are equal.
        /// </summary>
        /// <param name="x">The first point.</param>
        /// <param name="y">Teh second point.</param>
        /// <returns><c>true</c> if the specified points are equal; otherwise, <c>false</c>.</returns>
        [ContractMethod(CpuFee = 1 << 5)]
        public static bool Bls12381Equal(InteropInterface x, InteropInterface y)
        {
            return (x.GetInterface<object>(), y.GetInterface<object>()) switch
            {
                (G1Affine p1, G1Affine p2) => p1.Equals(p2),
                (G1Projective p1, G1Projective p2) => p1.Equals(p2),
                (G2Affine p1, G2Affine p2) => p1.Equals(p2),
                (G2Projective p1, G2Projective p2) => p1.Equals(p2),
                (Gt p1, Gt p2) => p1.Equals(p2),
                _ => throw new ArgumentException("BLS12-381 type mismatch")
            };
        }

        /// <summary>
        /// Add operation of two points.
        /// </summary>
        /// <param name="x">The first point.</param>
        /// <param name="y">The second point.</param>
        /// <returns></returns>
        [ContractMethod(CpuFee = 1 << 19)]
        public static InteropInterface Bls12381Add(InteropInterface x, InteropInterface y)
        {
            return (x.GetInterface<object>(), y.GetInterface<object>()) switch
            {
                (G1Affine p1, G1Affine p2) => new(new G1Projective(p1) + p2),
                (G1Affine p1, G1Projective p2) => new(p1 + p2),
                (G1Projective p1, G1Affine p2) => new(p1 + p2),
                (G1Projective p1, G1Projective p2) => new(p1 + p2),
                (G2Affine p1, G2Affine p2) => new(new G2Projective(p1) + p2),
                (G2Affine p1, G2Projective p2) => new(p1 + p2),
                (G2Projective p1, G2Affine p2) => new(p1 + p2),
                (G2Projective p1, G2Projective p2) => new(p1 + p2),
                (Gt p1, Gt p2) => new(p1 + p2),
                _ => throw new ArgumentException("BLS12-381 type mismatch")
            };
        }

        [ContractMethod(Hardfork.HF_Faun, CpuFee = 1 << 19, Name = "bls12_g1add")]
        public static byte[] Bls12G1Add(byte[] input)
        {
            ArgumentNullException.ThrowIfNull(input);
            if (input.Length != Bls12G1EncodedLength * 2)
                throw new ArgumentException("Invalid BLS12-381 g1add input length", nameof(input));

            var p1 = ParseEthereumG1Point(input.AsSpan(0, Bls12G1EncodedLength));
            var p2 = ParseEthereumG1Point(input.AsSpan(Bls12G1EncodedLength, Bls12G1EncodedLength));
            var result = new G1Projective(p1) + p2;
            return EncodeEthereumG1(result);
        }

        [ContractMethod(Hardfork.HF_Faun, CpuFee = 1 << 19, Name = "bls12_g2add")]
        public static byte[] Bls12G2Add(byte[] input)
        {
            ArgumentNullException.ThrowIfNull(input);
            if (input.Length != Bls12G2EncodedLength * 2)
                throw new ArgumentException("Invalid BLS12-381 g2add input length", nameof(input));

            var p1 = ParseEthereumG2Point(input.AsSpan(0, Bls12G2EncodedLength));
            var p2 = ParseEthereumG2Point(input.AsSpan(Bls12G2EncodedLength, Bls12G2EncodedLength));
            var result = new G2Projective(p1) + p2;
            return EncodeEthereumG2(result);
        }

        /// <summary>
        /// Mul operation of gt point and multiplier
        /// </summary>
        /// <param name="x">The point</param>
        /// <param name="mul">Multiplier,32 bytes,little-endian</param>
        /// <param name="neg">negative number</param>
        /// <returns></returns>
        [ContractMethod(CpuFee = 1 << 21)]
        public static InteropInterface Bls12381Mul(InteropInterface x, byte[] mul, bool neg)
        {
            Scalar X = neg ? -Scalar.FromBytes(mul) : Scalar.FromBytes(mul);
            return x.GetInterface<object>() switch
            {
                G1Affine p => new(p * X),
                G1Projective p => new(p * X),
                G2Affine p => new(p * X),
                G2Projective p => new(p * X),
                Gt p => new(p * X),
                _ => throw new ArgumentException("BLS12-381 type mismatch")
            };
        }

        [ContractMethod(Hardfork.HF_Faun, CpuFee = 1 << 21, Name = "bls12_g1mul")]
        public static byte[] Bls12G1Mul(byte[] input)
        {
            ArgumentNullException.ThrowIfNull(input);
            if (input.Length != Bls12G1EncodedLength + Bls12ScalarLength)
                throw new ArgumentException("Invalid BLS12-381 g1mul input length", nameof(input));

            var point = ParseEthereumG1Point(input.AsSpan(0, Bls12G1EncodedLength));
            var scalar = ParseEthereumScalar(input.AsSpan(Bls12G1EncodedLength, Bls12ScalarLength));
            var result = new G1Projective(point) * scalar;
            return EncodeEthereumG1(result);
        }

        [ContractMethod(Hardfork.HF_Faun, CpuFee = 1 << 21, Name = "bls12_g2mul")]
        public static byte[] Bls12G2Mul(byte[] input)
        {
            ArgumentNullException.ThrowIfNull(input);
            if (input.Length != Bls12G2EncodedLength + Bls12ScalarLength)
                throw new ArgumentException("Invalid BLS12-381 g2mul input length", nameof(input));

            var point = ParseEthereumG2Point(input.AsSpan(0, Bls12G2EncodedLength));
            var scalar = ParseEthereumScalar(input.AsSpan(Bls12G2EncodedLength, Bls12ScalarLength));
            var result = new G2Projective(point) * scalar;
            return EncodeEthereumG2(result);
        }

        /// <summary>
        /// Multi exponentiation operation for bls12381 points.
        /// </summary>
        /// <param name="pairs">Array of [point, scalar] pairs.</param>
        /// <returns>The accumulated point.</returns>
        [ContractMethod(Hardfork.HF_Faun, CpuFee = 1 << 23)]
        public static InteropInterface Bls12381MultiExp(Array pairs)
        {
            if (pairs is null || pairs.Count == 0)
                throw new ArgumentException("BLS12-381 multi exponent requires at least one pair");
            if (pairs.Count > Bls12381MultiExpMaxPairs)
                throw new ArgumentOutOfRangeException(nameof(pairs), $"BLS12-381 multi exponent supports at most {Bls12381MultiExpMaxPairs} pairs");

            bool? useG2 = null;
            G1Projective g1Accumulator = G1Projective.Identity;
            G2Projective g2Accumulator = G2Projective.Identity;

            foreach (StackItem item in pairs)
            {
                if (item is not Array pair || pair.Count != 2)
                    throw new ArgumentException("BLS12-381 multi exponent pair must contain point and scalar");

                if (pair[0] is not InteropInterface pointInterface)
                    throw new ArgumentException("BLS12-381 multi exponent requires interop points");

                var point = pointInterface.GetInterface<object>();
                switch (point)
                {
                    case G1Affine g1Affine:
                        EnsureG1PointValid(in g1Affine);
                        EnsureGroupType(ref useG2, false);
                        {
                            var scalar = ParseScalar(pair[1]);
                            if (!scalar.IsZero)
                                g1Accumulator += new G1Projective(g1Affine) * scalar;
                        }
                        break;
                    case G1Projective g1Projective:
                        EnsureG1PointValid(new G1Affine(g1Projective));
                        EnsureGroupType(ref useG2, false);
                        {
                            var scalar = ParseScalar(pair[1]);
                            if (!scalar.IsZero)
                                g1Accumulator += g1Projective * scalar;
                        }
                        break;
                    case G2Affine g2Affine:
                        EnsureG2PointValid(in g2Affine);
                        EnsureGroupType(ref useG2, true);
                        {
                            var scalar = ParseScalar(pair[1]);
                            if (!scalar.IsZero)
                                g2Accumulator += new G2Projective(g2Affine) * scalar;
                        }
                        break;
                    case G2Projective g2Projective:
                        EnsureG2PointValid(new G2Affine(g2Projective));
                        EnsureGroupType(ref useG2, true);
                        {
                            var scalar = ParseScalar(pair[1]);
                            if (!scalar.IsZero)
                                g2Accumulator += g2Projective * scalar;
                        }
                        break;
                    default:
                        throw new ArgumentException("BLS12-381 type mismatch");
                }
            }

            if (useG2 is null)
                throw new ArgumentException("BLS12-381 multi exponent requires at least one valid pair");

            return useG2.Value
                ? new InteropInterface(g2Accumulator)
                : new InteropInterface(g1Accumulator);
        }

        /// <summary>
        /// Pairing operation of g1 and g2
        /// </summary>
        /// <param name="g1">The g1 point.</param>
        /// <param name="g2">The g2 point.</param>
        /// <returns></returns>
        [ContractMethod(CpuFee = 1 << 23)]
        public static InteropInterface Bls12381Pairing(InteropInterface g1, InteropInterface g2)
        {
            G1Affine g1a = g1.GetInterface<object>() switch
            {
                G1Affine g => g,
                G1Projective g => new(g),
                _ => throw new ArgumentException("BLS12-381 type mismatch")
            };
            EnsureG1PointValid(in g1a);
            G2Affine g2a = g2.GetInterface<object>() switch
            {
                G2Affine g => g,
                G2Projective g => new(g),
                _ => throw new ArgumentException("BLS12-381 type mismatch")
            };
            EnsureG2PointValid(in g2a);
            return new(Bls12.Pairing(in g1a, in g2a));
        }

        [ContractMethod(Hardfork.HF_Faun, CpuFee = 1 << 23, Name = "bls12_pairing")]
        public static byte[] Bls12Pairing(byte[] input)
        {
            ArgumentNullException.ThrowIfNull(input);
            if (input.Length % Bls12PairInputLength != 0)
                throw new ArgumentException("Invalid BLS12-381 pairing input length", nameof(input));

            if (input.Length == 0)
                return EncodePairingResult(true);

            Gt accumulator = Gt.Identity;

            for (int offset = 0; offset < input.Length; offset += Bls12PairInputLength)
            {
                var g1 = ParseEthereumG1Point(input.AsSpan(offset, Bls12G1EncodedLength));
                var g2 = ParseEthereumG2Point(input.AsSpan(offset + Bls12G1EncodedLength, Bls12G2EncodedLength));
                accumulator += Bls12.Pairing(in g1, in g2);
            }

            return EncodePairingResult(accumulator.IsIdentity);
        }

        private static void EnsureGroupType(ref bool? current, bool isG2)
        {
            if (current is null)
            {
                current = isG2;
            }
            else if (current.Value != isG2)
            {
                throw new ArgumentException("BLS12-381 multi exponent cannot mix groups");
            }
        }

        private static Scalar ParseScalar(StackItem scalarItem)
        {
            ReadOnlySpan<byte> bigEndian = scalarItem switch
            {
                ByteString bs when bs.GetSpan().Length == Scalar.Size => bs.GetSpan(),
                VMBuffer buffer when buffer.Size == Scalar.Size => buffer.InnerBuffer.Span,
                _ => throw new ArgumentException("BLS12-381 scalar must be 32 bytes"),
            };

            Span<byte> littleEndian = stackalloc byte[Scalar.Size];
            for (int i = 0; i < Scalar.Size; i++)
                littleEndian[i] = bigEndian[Scalar.Size - 1 - i];

            Span<byte> wide = stackalloc byte[Scalar.Size * 2];
            littleEndian.CopyTo(wide);

            try
            {
                return Scalar.FromBytes(littleEndian);
            }
            catch (FormatException)
            {
                return Scalar.FromBytesWide(wide);
            }
        }

        private static void EnsureG1PointValid(in G1Affine point)
        {
            if (point.IsIdentity)
                return;
            if (!point.IsOnCurve || !point.IsTorsionFree)
                throw new ArgumentException("BLS12-381 point must be on-curve and in the prime-order subgroup");
        }

        private static void EnsureG2PointValid(in G2Affine point)
        {
            if (point.IsIdentity)
                return;
            if (!point.IsOnCurve || !point.IsTorsionFree)
                throw new ArgumentException("BLS12-381 point must be on-curve and in the prime-order subgroup");
        }

        private static G1Affine ParseEthereumG1Point(ReadOnlySpan<byte> data)
        {
            if (data.Length != Bls12G1EncodedLength)
                throw new ArgumentException("BLS12-381 G1 points must be 128 bytes");
            if (IsAllZero(data))
                return G1Affine.Identity;

            var x = ParseEthereumFp(data[..Bls12FieldElementLength]);
            var y = ParseEthereumFp(data[Bls12FieldElementLength..]);
            var point = new G1Affine(in x, in y);
            EnsureG1PointValid(in point);
            return point;
        }

        private static G2Affine ParseEthereumG2Point(ReadOnlySpan<byte> data)
        {
            if (data.Length != Bls12G2EncodedLength)
                throw new ArgumentException("BLS12-381 G2 points must be 256 bytes");
            if (IsAllZero(data))
                return G2Affine.Identity;

            var x0 = ParseEthereumFp(data[..Bls12FieldElementLength]);
            var x1 = ParseEthereumFp(data.Slice(Bls12FieldElementLength, Bls12FieldElementLength));
            var y0 = ParseEthereumFp(data.Slice(Bls12FieldElementLength * 2, Bls12FieldElementLength));
            var y1 = ParseEthereumFp(data.Slice(Bls12FieldElementLength * 3, Bls12FieldElementLength));
            var x = new Fp2(in x0, in x1);
            var y = new Fp2(in y0, in y1);
            var point = new G2Affine(in x, in y);
            EnsureG2PointValid(in point);
            return point;
        }

        private static Fp ParseEthereumFp(ReadOnlySpan<byte> data)
        {
            if (data.Length != Bls12FieldElementLength)
                throw new ArgumentException("BLS12-381 field elements must be 64 bytes");
            for (int i = 0; i < Bls12FieldElementLength - Fp.Size; i++)
                if (data[i] != 0)
                    throw new ArgumentException("BLS12-381 field element overflow");

            Span<byte> fieldBytes = stackalloc byte[Fp.Size];
            data[(Bls12FieldElementLength - Fp.Size)..].CopyTo(fieldBytes);
            return Fp.FromBytes(fieldBytes);
        }

        private static Scalar ParseEthereumScalar(ReadOnlySpan<byte> data)
        {
            if (data.Length != Bls12ScalarLength)
                throw new ArgumentException("BLS12-381 scalars must be 32 bytes");

            Span<byte> littleEndian = stackalloc byte[Scalar.Size];
            for (int i = 0; i < Scalar.Size; i++)
                littleEndian[i] = data[Scalar.Size - 1 - i];

            Span<byte> wide = stackalloc byte[Scalar.Size * 2];
            littleEndian.CopyTo(wide);

            try
            {
                return Scalar.FromBytes(littleEndian);
            }
            catch (FormatException)
            {
                return Scalar.FromBytesWide(wide);
            }
        }

        private static byte[] EncodeEthereumG1(G1Projective point)
        {
            var affine = new G1Affine(point);
            if (affine.IsIdentity)
                return new byte[Bls12G1EncodedLength];

            byte[] output = new byte[Bls12G1EncodedLength];
            WriteEthereumFp(in affine.X, output.AsSpan(0, Bls12FieldElementLength));
            WriteEthereumFp(in affine.Y, output.AsSpan(Bls12FieldElementLength, Bls12FieldElementLength));
            return output;
        }

        private static byte[] EncodeEthereumG2(G2Projective point)
        {
            var affine = new G2Affine(point);
            if (affine.IsIdentity)
                return new byte[Bls12G2EncodedLength];

            byte[] output = new byte[Bls12G2EncodedLength];
            WriteEthereumFp(in affine.X.C0, output.AsSpan(0, Bls12FieldElementLength));
            WriteEthereumFp(in affine.X.C1, output.AsSpan(Bls12FieldElementLength, Bls12FieldElementLength));
            WriteEthereumFp(in affine.Y.C0, output.AsSpan(Bls12FieldElementLength * 2, Bls12FieldElementLength));
            WriteEthereumFp(in affine.Y.C1, output.AsSpan(Bls12FieldElementLength * 3, Bls12FieldElementLength));
            return output;
        }

        private static void WriteEthereumFp(in Fp value, Span<byte> destination)
        {
            if (destination.Length != Bls12FieldElementLength)
                throw new ArgumentException("BLS12-381 field element encodings must be 64 bytes");
            destination.Clear();
            Span<byte> buffer = stackalloc byte[Fp.Size];
            if (!value.TryWrite(buffer))
                throw new ArgumentException("Failed to serialize BLS12-381 field element");
            buffer.CopyTo(destination[(Bls12FieldElementLength - Fp.Size)..]);
        }

        private static bool IsAllZero(ReadOnlySpan<byte> data)
        {
            foreach (byte b in data)
                if (b != 0)
                    return false;
            return true;
        }

        private static byte[] EncodePairingResult(bool success)
        {
            byte[] result = new byte[32];
            result[^1] = success ? (byte)1 : (byte)0;
            return result;
        }
    }
}
