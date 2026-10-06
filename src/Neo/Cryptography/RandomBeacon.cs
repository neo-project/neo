// Copyright (C) 2015-2026 The Neo Project.
//
// RandomBeacon.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Cryptography.BLS12_381;
using Neo.Extensions;
using System;
using System.Buffers.Binary;

namespace Neo.Cryptography
{
    /// <summary>
    /// Domain-separated helpers for a consensus-backed random beacon (issue #4724).
    /// Round id binds (network, height, view) so a dBFT view-change cannot reuse entropy.
    /// </summary>
    public static class RandomBeacon
    {
        /// <summary>
        /// Compressed beacon size in bytes (SHA-256 of the combined BLS signature).
        /// </summary>
        public const int Size = 32;

        /// <summary>
        /// Compressed BLS12-381 G1 size in bytes. <see cref="Finalize(ReadOnlySpan{byte}, ReadOnlySpan{byte})"/>
        /// accepts only this encoding.
        /// </summary>
        public const int G1CompressedSize = 48;

        /// <summary>
        /// Width of each <c>GetRandom</c> sample when a beacon is set (32-byte NeoVM integer).
        /// The high bit of the little-endian encoding is cleared so the value fits
        /// signed 32-byte <c>Integer</c> (<c>GetByteCount() ≤ 32</c>, at most 2^255 − 1).
        /// </summary>
        public const int DerivedSize = 32;

        /// <summary>
        /// Computes <c>rn = SHA256(network ‖ height ‖ view)</c>.
        /// </summary>
        public static byte[] ComputeRoundId(uint network, uint height, byte view)
        {
            Span<byte> buffer = stackalloc byte[4 + 4 + 1];
            BinaryPrimitives.WriteUInt32LittleEndian(buffer, network);
            BinaryPrimitives.WriteUInt32LittleEndian(buffer[4..], height);
            buffer[8] = view;
            return buffer.Sha256();
        }

        /// <summary>
        /// Final 32-byte beacon: <c>SHA256(combined_g1_compressed ‖ round_id)</c>.
        /// </summary>
        public static byte[] Finalize(G1Affine combinedSignature, ReadOnlySpan<byte> roundId)
            => Finalize(combinedSignature.ToCompressed(), roundId);

        /// <summary>
        /// Final 32-byte beacon: <c>SHA256(combined_g1_compressed ‖ round_id)</c>.
        /// <paramref name="combinedSignature"/> must be a 48-byte compressed G1 point.
        /// </summary>
        public static byte[] Finalize(ReadOnlySpan<byte> combinedSignature, ReadOnlySpan<byte> roundId)
        {
            if (combinedSignature.Length != G1CompressedSize)
                throw new ArgumentException($"Combined signature must be {G1CompressedSize} compressed G1 bytes.", nameof(combinedSignature));
            if (roundId.Length != Size)
                throw new ArgumentException($"Round id must be {Size} bytes.", nameof(roundId));

            var buffer = new byte[G1CompressedSize + Size];
            combinedSignature.CopyTo(buffer);
            roundId.CopyTo(buffer.AsSpan(G1CompressedSize));
            return buffer.Sha256();
        }

        /// <summary>
        /// Contract PRF: 32-byte <c>SHA256(beacon ‖ network ‖ txHash ‖ counter)</c>,
        /// with the high bit cleared so the unsigned integer fits NeoVM 32-byte size.
        /// <paramref name="txHash"/> must be 32 bytes (use a zero hash for non-transaction executions).
        /// </summary>
        public static byte[] Derive(ReadOnlySpan<byte> beacon, uint network, ReadOnlySpan<byte> txHash, uint counter)
        {
            if (beacon.Length != Size)
                throw new ArgumentException($"Beacon must be {Size} bytes.", nameof(beacon));
            if (txHash.Length != Size)
                throw new ArgumentException($"Transaction hash must be {Size} bytes.", nameof(txHash));

            var buffer = new byte[Size + 4 + Size + 4];
            beacon.CopyTo(buffer);
            BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(Size), network);
            txHash.CopyTo(buffer.AsSpan(Size + 4));
            BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(Size + 4 + Size), counter);
            var hash = buffer.Sha256();
            hash[31] &= 0x7F;
            return hash;
        }
    }
}
