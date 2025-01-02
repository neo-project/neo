// Copyright (C) 2015-2024 The Neo Project.
//
// Hasher.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;

namespace Neo.Cryptography
{
    /// <summary>
    /// Represents hash function identifiers supported by ECDSA message signature and verification.
    /// </summary>
    [Obsolete("Use HashAlgorithm instead")]
    public enum Hasher : byte
    {
        /// <summary>
        /// The SHA256 hash algorithm.
        /// </summary>
        SHA256 = 0x00,

        /// <summary>
        /// The Keccak256 hash algorithm.
        /// </summary>
        Keccak256 = 0x01,
    }
}
