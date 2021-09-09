// Copyright (C) 2015-2021 The Neo Project.
// 
// The neo is free software distributed under the MIT software license, 
// see the accompanying file LICENSE in the main directory of the
// project or http://www.opensource.org/licenses/mit-license.php 
// for more details.
// 
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

namespace Neo.SmartContract.Native
{
    /// <summary>
    /// Represents the named curve used in ECDSA.
    /// </summary>
    /// <remarks>
    /// https://tools.ietf.org/html/rfc4492#section-5.1.1
    /// </remarks>
    public enum NamedCurve : byte
    {
        /// <summary>
        /// The secp256k1 curve.
        /// </summary>
        secp256k1 = 22,

        /// <summary>
        /// The secp256r1 curve, which known as prime256v1 or nistP-256.
        /// </summary>
        secp256r1 = 23
    }
}
