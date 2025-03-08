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
using Neo.SmartContract;
using System;
using System.Collections.Generic;

namespace Neo.Sign
{
    /// <summary>
    /// Represents a signer that can sign messages.
    /// </summary>
    public interface ISigner
    {
        /// <summary>
        /// Signs the <see cref="ContractParametersContext"/> with the wallet.
        /// </summary>
        /// <param name="context">The <see cref="ContractParametersContext"/> to be used.</param>
        /// <returns>
        /// <see langword="true"/> if any signature is successfully added to the context;
        /// otherwise, <see langword="false"/>.
        /// </returns>
        bool Sign(ContractParametersContext context);

        /// <summary>
        /// Signs the specified data with the corresponding private key of the specified public key.
        /// </summary>
        /// <param name="signData">The data to sign.</param>
        /// <param name="publicKey">The public key.</param>
        /// <returns>The signature.</returns>
        byte[] Sign(byte[] signData, ECPoint publicKey);

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
