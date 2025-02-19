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

using Neo.SmartContract;
using Neo.Wallets;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Neo.Build.Core.Wallets
{
    public class DevWallet : Wallet
    {
        public DevWallet(string walletName) : base(string.Empty, ProtocolSettings.Default)
        {
            _walletName = walletName;
        }

        private readonly ConcurrentDictionary<UInt160, DevWalletAccount> _walletAccounts = new();

        private readonly string _walletName;

        public override string Name => _walletName;

        public override Version Version => new(1, 0);

        public override bool ChangePassword(string oldPassword, string newPassword) =>
            throw new NotImplementedException();

        public override bool Contains(UInt160 scriptHash) =>
            _walletAccounts.ContainsKey(scriptHash);

        public override WalletAccount CreateAccount(byte[] privateKey)
        {
            var kp = new KeyPair(privateKey);
            var c = Contract.CreateSignatureContract(kp.PublicKey);
            var wa = new DevWalletAccount(kp, c, ProtocolSettings);

            _ = _walletAccounts.TryAdd(wa.ScriptHash, wa);

            return wa;
        }

        public override WalletAccount CreateAccount(Contract contract, KeyPair? key = null)
        {
            var wa = new DevWalletAccount(key, contract, ProtocolSettings);

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
            _walletAccounts.TryGetValue(scriptHash, out var wa);

            return wa;
        }

        public override IEnumerable<WalletAccount> GetAccounts() =>
            _walletAccounts.Values;

        public override void Delete() { }

        public override void Save() { }

        public override bool VerifyPassword(string password)
        {
            throw new NotImplementedException();
        }
    }
}
