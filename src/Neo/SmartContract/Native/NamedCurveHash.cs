// Copyright (C) 2015-2025 The Neo Project.
//
// NamedCurveHash.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

namespace Neo.SmartContract.Native
{
    /// <summary>
    /// Represents a pair of the named curve used in ECDSA and a hash algorithm used to hash message.
    /// </summary>
    public enum NamedCurveHash : byte
    {
        /// <summary>
        /// The secp256k1 curve and SHA256 hash algorithm.
        /// </summary>
        secp256k1SHA256 = 22,

        /// <summary>
        /// The secp256r1 curve, which known as prime256v1 or nistP-256, and SHA256 hash algorithm.
        /// </summary>
        secp256r1SHA256 = 23,

        /// <summary>
        /// The secp256k1 curve and Keccak256 hash algorithm.
        /// </summary>
        secp256k1Keccak256 = 122,

        /// <summary>
        /// The secp256r1 curve, which known as prime256v1 or nistP-256, and Keccak256 hash algorithm.
        /// </summary>
        secp256r1Keccak256 = 123
    }
}
