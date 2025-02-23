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
using Neo.Build.Core.Models.Wallets;
using Neo.SmartContract;
using Neo.Wallets;
using System.Collections.Generic;
using System.Linq;
using NContract = Neo.SmartContract.Contract;

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
        /// <param name="walletAccountModel">Wallet account <see cref="JsonModel"/>.</param>
        public DevWalletAccount(
            TestWalletAccountModel walletAccountModel) : base(walletAccountModel.Address, ProtocolSettings.Default)
        {
            _keyPair = walletAccountModel.Key;

            Label = walletAccountModel.Label;
            IsDefault = walletAccountModel.IsDefault;
            Lock = walletAccountModel.Lock;

            if (walletAccountModel.Contract == null)
            {
                Contract = NContract.Create([], []);
                _parameterNames = [];
            }
            else
            {
                Contract = walletAccountModel.Contract.ToObject();
                if (walletAccountModel.Contract.Parameters != null)
                    _parameterNames = walletAccountModel.Contract.Parameters != null ?
                        [.. walletAccountModel.Contract.Parameters.Select(static (s, i) => s.Name ?? $"parameter{i}")] :
                        [.. Contract.ParameterList.Select(static (_, i) => $"parameter{i}")];
                else
                    _parameterNames = [.. Contract.ParameterList.Select(static (_, i) => $"parameter{i}")];
            }
        }

        /// <summary>
        /// Creates a new developer wallet account.
        /// </summary>
        /// <param name="walletKeyPair">Private key pair for the account.</param>
        /// <param name="walletContract">Contract for the account.</param>
        /// <param name="protocolSettings">Settings for the account.</param>
        /// <param name="accountLabel">A name for the account.</param>
        public DevWalletAccount(
            Contract walletContract,
            ProtocolSettings protocolSettings,
            KeyPair? walletKeyPair = null,
            string? accountLabel = null) : base(walletContract.ScriptHash, protocolSettings)
        {
            _keyPair = walletKeyPair;
            Label = accountLabel;
            Contract = walletContract;
            IsDefault = false;
            Lock = false;
            _parameterNames = [.. walletContract.ParameterList.Select(static (_, i) => $"parameter{i}")];
        }

        /// <summary>
        /// Creates a new developer wallet account.
        /// </summary>
        /// <param name="accountScriptHash">Hash160 public address of the account.</param>
        /// <param name="protocolSettings">Settings for the account.</param>
        /// <param name="accountLabel">A name for the account.</param>
        public DevWalletAccount(
            UInt160 accountScriptHash,
            ProtocolSettings protocolSettings,
            string? accountLabel = null) : base(accountScriptHash, protocolSettings)
        {
            Label = accountLabel;
            IsDefault = false;
            Lock = false;
            Contract = NContract.Create(accountScriptHash, ContractParameterType.Signature);
            _parameterNames = [nameof(ContractParameterType.Signature)];
        }

        private readonly ICollection<string> _parameterNames;

        /// <summary>
        /// Public and private key pair.
        /// </summary>
        private readonly KeyPair? _keyPair;

        /// <summary>
        /// Does the account have a key pair.
        /// </summary>
        public override bool HasKey => _keyPair != null;

        /// <summary>
        /// Gets the account's <see cref="KeyPair"/> object.
        /// </summary>
        /// <returns>The associated <see cref="KeyPair"/> for the account</returns>
        public override KeyPair GetKey() =>
            _keyPair!;

        /// <summary>
        /// Converts to a <see cref="JsonModel"/>.
        /// </summary>
        /// <returns>A <see cref="JsonModel"/> that can be serialized to a JSON string.</returns>
        public TestWalletAccountModel ToObject() =>
            new()
            {
                Label = Label,
                IsDefault = IsDefault,
                Address = ScriptHash,
                Key = _keyPair,
                Contract = new()
                {
                    Script = Contract.Script,
                    Parameters = [
                        .. Contract.ParameterList.Zip(_parameterNames, (f, s) =>
                            new ContractParameterModel()
                            {
                                Name = s,
                                Type = f
                            }),
                    ],
                },
            };

        /// <summary>
        /// <see cref="DevWalletAccount"/> as JSON string.
        /// </summary>
        /// <returns>JSON</returns>
        public override string ToString() =>
            ToObject().ToString();
    }
}
