// Copyright (C) 2015-2021 The Neo Project.
// 
// The neo is free software distributed under the MIT software license, 
// see the accompanying file LICENSE in the main directory of the
// project or http://www.opensource.org/licenses/mit-license.php 
// for more details.
// 
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Network.P2P.Payloads;

namespace Neo.Ledger
{
    /// <summary>
    /// Represents a verifying result of <see cref="IInventory"/>.
    /// </summary>
    public enum VerifyResult : byte
    {
        /// <summary>
        /// Indicates that the verification was successful.
        /// </summary>
        Succeed,

        /// <summary>
        /// Indicates that an <see cref="IInventory"/> with the same hash already exists.
        /// </summary>
        AlreadyExists,

        /// <summary>
        /// Indicates that the <see cref="MemoryPool"/> is full and the transaction cannot be verified.
        /// </summary>
        OutOfMemory,

        /// <summary>
        /// Indicates that the previous block of the current block has not been received, so the block cannot be verified.
        /// </summary>
        UnableToVerify,

        /// <summary>
        /// Indicates that the <see cref="IInventory"/> is invalid.
        /// </summary>
        Invalid,

        /// <summary>
        /// Indicates that the <see cref="Transaction"/> has expired.
        /// </summary>
        Expired,

        /// <summary>
        /// Indicates that the <see cref="Transaction"/> failed to verify due to insufficient fees.
        /// </summary>
        InsufficientFunds,

        /// <summary>
        /// Indicates that the <see cref="Transaction"/> failed to verify because it didn't comply with the policy.
        /// </summary>
        PolicyFail,

        /// <summary>
        /// Indicates that the <see cref="IInventory"/> failed to verify due to other reasons.
        /// </summary>
        Unknown
    }
}
