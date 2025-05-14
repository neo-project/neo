// Copyright (C) 2015-2025 The Neo Project.
//
// ISigner.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Cryptography.ECC;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using System;

namespace Neo.Sign
{
    /// <summary>
    /// Represents a signer that can sign messages.
    /// </summary>
    public interface ISigner
    {
        /// <summary>
        /// Signs the <see cref="ExtensiblePayload"/> with the wallet.
        /// </summary>
        /// <param name="payload">The <see cref="ExtensiblePayload"/> to be used.</param>
        /// <param name="snapshot">The snapshot.</param>
        /// <param name="network">The network.</param>
        /// <returns>The witness.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the payload is null.</exception>
        Witness SignExtensiblePayload(ExtensiblePayload payload, DataCache snapshot, uint network);

        /// <summary>
        /// Signs the specified data with the corresponding private key of the specified public key.
        /// </summary>
        /// <param name="block">The block to sign.</param>
        /// <param name="publicKey">The public key.</param>
        /// <param name="network">The network.</param>
        /// <returns>The signature.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the block or public key is null.</exception>
        /// <exception cref="SignException">
        /// Thrown when the account is not found or not signable, or the network is not matching.
        /// </exception>
        ReadOnlyMemory<byte> SignBlock(Block block, ECPoint publicKey, uint network);

        /// <summary>
        /// Checks if the wallet contains an account(has private key and is not locked) with the specified public key.
        /// If the wallet has the public key but not the private key or the account is locked, it will return false.
        /// </summary>
        /// <param name="publicKey">The public key.</param>
        /// <returns>
        /// <see langword="true"/> if the wallet contains the specified public key and the corresponding unlocked private key;
        /// otherwise, <see langword="false"/>.
        /// </returns>
        bool ContainsSignable(ECPoint publicKey);
    }
}
