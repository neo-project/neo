// Copyright (C) 2015-2025 The Neo Project.
//
// DevWalletAccount.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Build.Core.Interfaces;
using Neo.Build.Core.Models;
using Neo.Build.Core.Models.Wallet;
using Neo.SmartContract;
using Neo.Wallets;

namespace Neo.Build.Core.Wallets
{
    /// <summary>
    /// Developer wallet account.
    /// </summary>
    public class DevWalletAccount : WalletAccount, IConvertToObject<TestWalletAccountModel>
    {
        /// <summary>
        /// Creates a new developer wallet account.
        /// </summary>
        /// <param name="walletAccountModel">Wallet account model.</param>
        public DevWalletAccount(
            TestWalletAccountModel walletAccountModel) : base(walletAccountModel.ScriptHash, ProtocolSettings.Default)
        {
            _keyPair = walletAccountModel.Key;

            Label = walletAccountModel.Label;
            IsDefault = walletAccountModel.IsDefault;
            Lock = false;

            if (walletAccountModel.Contract != null)
                Contract = walletAccountModel.Contract.ToObject();
        }

        public DevWalletAccount(
            KeyPair? walletKeyPair,
            Contract walletContract,
            ProtocolSettings protocolSettings,
            string? accountLabel = null) : base(walletContract.ScriptHash, protocolSettings)
        {
            _keyPair = walletKeyPair;
            Contract = walletContract;
            Label = accountLabel;
            IsDefault = false;
            Lock = false;
        }

        public DevWalletAccount(
            UInt160 accountScriptHash,
            ProtocolSettings protocolSettings,
            string? accountLabel = null) : base(accountScriptHash, protocolSettings)
        {
            Label = accountLabel;
            IsDefault = false;
            Lock = false;
        }

        /// <summary>
        /// Public and private key pair.
        /// </summary>
        private readonly KeyPair? _keyPair;

        /// <summary>
        /// Does the account have a key pair.
        /// </summary>
        public override bool HasKey => true;

        /// <summary>
        /// Gets the account's <see cref="KeyPair"/> object.
        /// </summary>
        /// <returns>The associated <see cref="KeyPair"/> for the account</returns>
        public override KeyPair? GetKey() =>
            _keyPair;

        /// <summary>
        /// Converts to a <see cref="JsonModel"/>.
        /// </summary>
        /// <returns>A <see cref="JsonModel"/> that can be serialized.</returns>
        public TestWalletAccountModel ToObject() =>
            new()
            {
                Label = Label,
                IsDefault = IsDefault,
                ScriptHash = ScriptHash,
                Key = _keyPair,
                Contract = new()
                {
                    Script = Contract.Script,
                    Parameters = Contract.ParameterList,
                },
            };
    }
}
