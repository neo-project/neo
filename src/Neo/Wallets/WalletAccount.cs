// Copyright (C) 2015-2023 The Neo Project.
// 
// The neo is free software distributed under the MIT software license, 
// see the accompanying file LICENSE in the main directory of the
// project or http://www.opensource.org/licenses/mit-license.php 
// for more details.
// 
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.SmartContract;

namespace Neo.Wallets
{
    /// <summary>
    /// Represents an account in a wallet.
    /// </summary>
    public abstract class WalletAccount
    {
        /// <summary>
        /// The <see cref="Neo.ProtocolSettings"/> to be used by the wallet.
        /// </summary>
        protected readonly ProtocolSettings ProtocolSettings;

        /// <summary>
        /// The hash of the account.
        /// </summary>
        public readonly UInt160 ScriptHash;

        /// <summary>
        /// The label of the account.
        /// </summary>
        public string Label;

        /// <summary>
        /// Indicates whether the account is the default account in the wallet.
        /// </summary>
        public bool IsDefault;

        /// <summary>
        /// Indicates whether the account is locked.
        /// </summary>
        public bool Lock;

        /// <summary>
        /// The contract of the account.
        /// </summary>
        public Contract Contract;

        /// <summary>
        /// The address of the account.
        /// </summary>
        public string Address => ScriptHash.ToAddress(ProtocolSettings.AddressVersion);

        /// <summary>
        /// Indicates whether the account contains a private key.
        /// </summary>
        public abstract bool HasKey { get; }

        /// <summary>
        /// Indicates whether the account is a watch-only account.
        /// </summary>
        public bool WatchOnly => Contract == null;

        /// <summary>
        /// Gets the private key of the account.
        /// </summary>
        /// <returns>The private key of the account. Or <see langword="null"/> if there is no private key in the account.</returns>
        public abstract KeyPair GetKey();

        /// <summary>
        /// Initializes a new instance of the <see cref="WalletAccount"/> class.
        /// </summary>
        /// <param name="scriptHash">The hash of the account.</param>
        /// <param name="settings">The <see cref="Neo.ProtocolSettings"/> to be used by the wallet.</param>
        protected WalletAccount(UInt160 scriptHash, ProtocolSettings settings)
        {
            this.ProtocolSettings = settings;
            this.ScriptHash = scriptHash;
        }
    }
}
