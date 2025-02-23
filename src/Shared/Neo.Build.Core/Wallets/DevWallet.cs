// Copyright (C) 2015-2025 The Neo Project.
//
// DevWallet.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Build.Core.Exceptions;
using Neo.Build.Core.Interfaces;
using Neo.Build.Core.Models;
using Neo.Build.Core.Models.Wallets;
using Neo.SmartContract;
using Neo.Wallets;
using Neo.Wallets.NEP6;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Neo.Build.Core.Wallets
{
    /// <summary>
    /// Developer wallet.
    /// </summary>
    public class DevWallet : Wallet, IConvertToObject<TestWalletModel>
    {
        /// <summary>
        /// Creates a new developer wallet.
        /// </summary>
        /// <param name="walletModel">Wallet <see cref="JsonModel"/>.</param>
        /// <exception cref="NeoBuildInvalidVersionFormatException"></exception>
        public DevWallet(
            TestWalletModel walletModel) : base(string.Empty, ProtocolSettings.Default)
        {
            if (walletModel.Version != Version)
                throw new NeoBuildInvalidVersionFormatException();

            _walletName = walletModel.Name;
            _sCryptParameters = walletModel.Scrypt ?? SCryptModel.Default;

            if (walletModel.Accounts != null)
            {
                foreach (var account in walletModel.Accounts)
                {
                    if (account is null) continue;
                    if (account.Address is null) continue;
                    _walletAccounts[account.Address] = new(account);
                }
            }
        }

        private readonly ConcurrentDictionary<UInt160, DevWalletAccount> _walletAccounts = new();

        private readonly string? _walletName;

        private readonly SCryptModel _sCryptParameters;

        public ScryptParameters SCryptParameters => _sCryptParameters.ToObject();

        public override string? Name => _walletName;

        public override Version Version => new(1, 0);

        public override bool Contains(UInt160 scriptHash) =>
            _walletAccounts.ContainsKey(scriptHash);

        public override WalletAccount CreateAccount(byte[] privateKey)
        {
            var kp = new KeyPair(privateKey);
            var c = Contract.CreateSignatureContract(kp.PublicKey);
            var wa = new DevWalletAccount(c, ProtocolSettings, kp);

            _ = _walletAccounts.TryAdd(wa.ScriptHash, wa);

            return wa;
        }

        public override WalletAccount CreateAccount(Contract contract, KeyPair? key = null)
        {
            var wa = new DevWalletAccount(contract, ProtocolSettings, key);

            _ = _walletAccounts.TryAdd(wa.ScriptHash, wa);

            return wa;
        }

        public override WalletAccount CreateAccount(UInt160 scriptHash)
        {
            var wa = new DevWalletAccount(scriptHash, ProtocolSettings);

            _ = _walletAccounts.TryAdd(wa.ScriptHash, wa);

            return wa;
        }

        public override bool DeleteAccount(UInt160 scriptHash) =>
            _ = _walletAccounts.TryRemove(scriptHash, out _);

        public override WalletAccount? GetAccount(UInt160 scriptHash)
        {
            _ = _walletAccounts.TryGetValue(scriptHash, out var wa);

            return wa;
        }

        public override IEnumerable<WalletAccount> GetAccounts() =>
            _walletAccounts.Values;

        public override void Delete() { }

        public override void Save() { }

        public override bool ChangePassword(string oldPassword, string newPassword) =>
            throw new NotImplementedException();

        public override bool VerifyPassword(string password) =>
            throw new NotImplementedException();

        /// <summary>
        /// Converts to a <see cref="JsonModel"/>.
        /// </summary>
        /// <returns>A <see cref="JsonModel"/> that can be serialized to a JSON string.</returns>
        public TestWalletModel ToObject() =>
            new()
            {
                Name = Name,
                Version = Version,
                Scrypt = _sCryptParameters,
                Accounts = [.. _walletAccounts.Values.Select(s => s.ToObject())]
            };

        /// <summary>
        /// <see cref="DevWallet"/> as JSON string.
        /// </summary>
        /// <returns>JSON</returns>
        public override string ToString() =>
            ToObject().ToString();
    }
}
