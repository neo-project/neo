// Copyright (C) 2015-2025 The Neo Project.
//
// BN254.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Extensions;
using Nethermind.MclBindings;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Neo.Cryptography
{
    public static class BN254
    {
        public const int FieldElementLength = 32;
        public const int G1EncodedLength = 64;
        public const int PairInputLength = 192;

        private static readonly object s_sync = new();
        private static bool s_initialized;

        public static byte[] Add(ReadOnlySpan<byte> input)
        {
            if (input.Length != G1EncodedLength * 2)
                throw new ArgumentException("Invalid BN254 add input length", nameof(input));

            EnsureInitialized();

            if (!TryDeserializeG1(input[..G1EncodedLength], out var first))
                return new byte[G1EncodedLength];

            if (!TryDeserializeG1(input[G1EncodedLength..], out var second))
                return new byte[G1EncodedLength];

            mclBnG1 result = default;
            Mcl.mclBnG1_add(ref result, first, second);
            Mcl.mclBnG1_normalize(ref result, result);

            return SerializeG1(result);
        }

        public static byte[] Mul(ReadOnlySpan<byte> input)
        {
            if (input.Length != G1EncodedLength + FieldElementLength)
                throw new ArgumentException("Invalid BN254 mul input length", nameof(input));

            EnsureInitialized();

            if (!TryDeserializeG1(input[..G1EncodedLength], out var basePoint))
                return new byte[G1EncodedLength];

            if (!TryDeserializeScalar(input[G1EncodedLength..], out var scalar))
                return new byte[G1EncodedLength];

            mclBnG1 result = default;
            Mcl.mclBnG1_mul(ref result, basePoint, scalar);
            Mcl.mclBnG1_normalize(ref result, result);

            return SerializeG1(result);
        }

        public static byte[] Pairing(ReadOnlySpan<byte> input)
        {
            if (input.Length % PairInputLength != 0)
                throw new ArgumentException("Invalid BN254 pairing input length", nameof(input));

            EnsureInitialized();

            if (input.Length == 0)
                return SuccessWord();

            int pairCount = input.Length / PairInputLength;
            bool hasEffectivePair = false;

            mclBnGT accumulator = default;
            Mcl.mclBnGT_setInt32(ref accumulator, 1);

            for (int pairIndex = 0; pairIndex < pairCount; pairIndex++)
            {
                int offset = pairIndex * PairInputLength;
                var g1Slice = input.Slice(offset, G1EncodedLength);
                var g2Slice = input.Slice(offset + G1EncodedLength, 2 * G1EncodedLength);

                if (!TryDeserializeG1(g1Slice, out var g1))
                    return new byte[FieldElementLength];

                if (!TryDeserializeG2(g2Slice, out var g2))
                    return new byte[FieldElementLength];

                if (Mcl.mclBnG1_isZero(g1) == 1 || Mcl.mclBnG2_isZero(g2) == 1)
                    continue;

                hasEffectivePair = true;

                mclBnGT current = default;
                Mcl.mclBn_pairing(ref current, g1, g2);

                if (Mcl.mclBnGT_isValid(current) == 0)
                    return new byte[FieldElementLength];

                mclBnGT temp = accumulator;
                Mcl.mclBnGT_mul(ref accumulator, temp, current);
            }

            if (!hasEffectivePair)
                return SuccessWord();

            return Mcl.mclBnGT_isOne(accumulator) == 1 ? SuccessWord() : new byte[FieldElementLength];
        }

        private static unsafe bool TryDeserializeG1(ReadOnlySpan<byte> encoded, out mclBnG1 point)
        {
            point = default;

            if (!encoded.NotZero())
                return true;

            ReadOnlySpan<byte> xBytes = encoded[..FieldElementLength];
            fixed (byte* ptr = xBytes)
            {
                if (Mcl.mclBnFp_setBigEndianMod(ref point.x, (nint)ptr, (nuint)xBytes.Length) != 0)
                    return false;
            }

            ReadOnlySpan<byte> yBytes = encoded[FieldElementLength..];
            fixed (byte* ptr = yBytes)
            {
                if (Mcl.mclBnFp_setBigEndianMod(ref point.y, (nint)ptr, (nuint)yBytes.Length) != 0)
                    return false;
            }

            Mcl.mclBnFp_setInt32(ref point.z, 1);

            return Mcl.mclBnG1_isValid(point) == 1;
        }

        private static unsafe bool TryDeserializeScalar(ReadOnlySpan<byte> encoded, out mclBnFr scalar)
        {
            scalar = default;

            if (!encoded.NotZero())
            {
                Mcl.mclBnFr_clear(ref scalar);
                return true;
            }

            fixed (byte* ptr = encoded)
            {
                if (Mcl.mclBnFr_setBigEndianMod(ref scalar, (nint)ptr, (nuint)encoded.Length) == -1)
                    return false;
            }

            return Mcl.mclBnFr_isValid(scalar) == 1;
        }

        private static unsafe bool TryDeserializeG2(ReadOnlySpan<byte> encoded, out mclBnG2 point)
        {
            point = default;

            if (!encoded.NotZero())
                return true;

            Span<byte> scratch = stackalloc byte[FieldElementLength];

            var realSegment = encoded.Slice(FieldElementLength, FieldElementLength);
            CopyReversed(realSegment, scratch);
            fixed (byte* ptr = scratch)
            {
                if (Mcl.mclBnFp_deserialize(ref point.x.d0, (nint)ptr, (nuint)scratch.Length) == UIntPtr.Zero)
                    return false;
            }

            var imagSegment = encoded[..FieldElementLength];
            CopyReversed(imagSegment, scratch);
            fixed (byte* ptr = scratch)
            {
                if (Mcl.mclBnFp_deserialize(ref point.x.d1, (nint)ptr, (nuint)scratch.Length) == UIntPtr.Zero)
                    return false;
            }

            var yReal = encoded.Slice(3 * FieldElementLength, FieldElementLength);
            CopyReversed(yReal, scratch);
            fixed (byte* ptr = scratch)
            {
                if (Mcl.mclBnFp_deserialize(ref point.y.d0, (nint)ptr, (nuint)scratch.Length) == UIntPtr.Zero)
                    return false;
            }

            var yImag = encoded.Slice(2 * FieldElementLength, FieldElementLength);
            CopyReversed(yImag, scratch);
            fixed (byte* ptr = scratch)
            {
                if (Mcl.mclBnFp_deserialize(ref point.y.d1, (nint)ptr, (nuint)scratch.Length) == UIntPtr.Zero)
                    return false;
            }

            Mcl.mclBnFp_setInt32(ref point.z.d0, 1);

            return true;
        }

        private static unsafe byte[] SerializeG1(in mclBnG1 point)
        {
            var output = new byte[G1EncodedLength];

            if (Mcl.mclBnG1_isZero(point) == 1)
                return output;

            Span<byte> scratch = stackalloc byte[FieldElementLength];

            fixed (byte* ptr = scratch)
            {
                if (Mcl.mclBnFp_getLittleEndian((nint)ptr, (nuint)scratch.Length, point.x) == UIntPtr.Zero)
                    throw new ArgumentException("Failed to serialize BN254 point");
            }

            WriteBigEndian(scratch, output.AsSpan(0, FieldElementLength));

            fixed (byte* ptr = scratch)
            {
                if (Mcl.mclBnFp_getLittleEndian((nint)ptr, (nuint)scratch.Length, point.y) == UIntPtr.Zero)
                    throw new ArgumentException("Failed to serialize BN254 point");
            }

            WriteBigEndian(scratch, output.AsSpan(FieldElementLength, FieldElementLength));

            return output;
        }

        private static byte[] SuccessWord()
        {
            var output = new byte[FieldElementLength];
            output[^1] = 1;
            return output;
        }

        private static void WriteBigEndian(ReadOnlySpan<byte> littleEndian, Span<byte> destination)
        {
            for (int i = 0; i < littleEndian.Length; ++i)
                destination[i] = littleEndian[littleEndian.Length - 1 - i];
        }

        private static void CopyReversed(ReadOnlySpan<byte> source, Span<byte> destination)
        {
            for (int i = 0; i < source.Length; ++i)
                destination[i] = source[source.Length - 1 - i];
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void EnsureInitialized()
        {
            if (s_initialized)
                return;

            lock (s_sync)
            {
                if (s_initialized)
                    return;

                if (Mcl.mclBn_init(Mcl.MCL_BN_SNARK1, Mcl.MCLBN_COMPILED_TIME_VAR) != 0)
                    throw new InvalidOperationException("BN254 initialization failed");

                Mcl.mclBn_setETHserialization(1);

                s_initialized = true;
            }
        }
    }
}
