// Copyright (C) 2015-2025 The Neo Project.
//
// ProofOfWork.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Extensions;
using Neo.Extensions.Factories;
using Newtonsoft.Json.Linq;
using System;
using System.Buffers.Binary;

namespace Neo.Cryptography
{
    public class ProofOfWork
    {
        /// <summary>
        /// Verify if the proof of work match with the difficulty.
        /// </summary>
        /// <param name="proofOfWork">Proof of Work</param>
        /// <param name="difficulty">Difficulty</param>
        /// <returns></returns>
        public static bool VerifyDifficulty(UInt256 proofOfWork, ulong difficulty)
        {
            // Take the first 8 bytes in order to check the proof of work difficulty

            var bytes = proofOfWork.ToArray();
            var value = BinaryPrimitives.ReadUInt64BigEndian(bytes.AsSpan(0, 8));
            return value < difficulty;
        }

        /// <summary>
        /// Compute proof of work
        /// </summary>
        /// <param name="blockHash">BlockHash</param>
        /// <param name="nonce">Nonce</param>
        /// <returns>Proof of Work</returns>
        public static UInt256 Compute(UInt256 blockHash, long? nonce = 0)
        {
            var salt = new byte[16];

            if (nonce.HasValue)
            {
                BinaryPrimitives.WriteInt64BigEndian(salt, nonce.Value);
            }

            return (UInt256)Helper.Blake2b_256(blockHash.ToArray(), salt);
        }

        /// <summary>
        /// Compute proof of work with difficulty
        /// </summary>
        /// <param name="blockHash">Block hash</param>
        /// <param name="difficulty">Difficulty</param>
        /// <returns>Nonce</returns>
        public static long ComputeNonce(UInt256 blockHash, ulong difficulty)
        {
            while (true)
            {
                var nonce = RandomNumberFactory.NextInt64();
                var pow = Compute(blockHash, nonce);

                if (VerifyDifficulty(pow, difficulty))
                    return nonce;
            }
        }
    }
}
