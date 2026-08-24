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
        /// Width of each <c>GetRandom</c> sample when a beacon is set (32-bit unsigned).
        /// </summary>
        public const int DerivedSize = sizeof(uint);

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
        public static byte[] Finalize(ReadOnlySpan<byte> combinedSignature, ReadOnlySpan<byte> roundId)
        {
            if (combinedSignature.Length == 0)
                throw new ArgumentException("Combined signature must not be empty.", nameof(combinedSignature));
            if (roundId.Length != Size)
                throw new ArgumentException($"Round id must be {Size} bytes.", nameof(roundId));

            var buffer = new byte[combinedSignature.Length + roundId.Length];
            combinedSignature.CopyTo(buffer);
            roundId.CopyTo(buffer.AsSpan(combinedSignature.Length));
            return buffer.Sha256();
        }

        /// <summary>
        /// Contract PRF: first 4 bytes of <c>SHA256(beacon ‖ network ‖ txHash ‖ counter)</c> (uint32).
        /// </summary>
        public static byte[] Derive(ReadOnlySpan<byte> beacon, uint network, ReadOnlySpan<byte> txHash, uint counter)
        {
            if (beacon.Length != Size)
                throw new ArgumentException($"Beacon must be {Size} bytes.", nameof(beacon));
            if (txHash.Length == 0)
                throw new ArgumentException("Transaction hash must not be empty.", nameof(txHash));

            var buffer = new byte[Size + 4 + txHash.Length + 4];
            beacon.CopyTo(buffer);
            BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(Size), network);
            txHash.CopyTo(buffer.AsSpan(Size + 4));
            BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(Size + 4 + txHash.Length), counter);
            var hash = buffer.Sha256();
            return hash[..DerivedSize];
        }
    }
}
