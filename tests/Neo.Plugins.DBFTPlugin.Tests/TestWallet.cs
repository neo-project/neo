// Copyright (C) 2015-2025 The Neo Project.
//
// TestWallet.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Cryptography.ECC;
using Neo.Extensions;
using Neo.SmartContract;
using Neo.Wallets;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Neo.Plugins.DBFTPlugin.Tests
{
    public class TestWallet : Wallet
    {
        private readonly Dictionary<UInt160, TestWalletAccount> accounts = new();

        public TestWallet(ProtocolSettings settings) : base(null, settings)
        {
        }

        public override string Name => "TestWallet";
        public override Version Version => new Version(1, 0, 0);

        public override bool ChangePassword(string oldPassword, string newPassword)
        {
            return true;
        }

        public override void Delete()
        {
            // No-op for test wallet
        }

        public override void Save()
        {
            // No-op for test wallet
        }

        public void AddAccount(ECPoint publicKey)
        {
            var scriptHash = Contract.CreateSignatureRedeemScript(publicKey).ToScriptHash();
            var account = new TestWalletAccount(scriptHash, publicKey, ProtocolSettings);
            accounts[scriptHash] = account;
        }

        public override bool Contains(UInt160 scriptHash)
        {
            return accounts.ContainsKey(scriptHash);
        }

        public override WalletAccount CreateAccount(byte[] privateKey)
        {
            throw new NotImplementedException();
        }

        public override WalletAccount CreateAccount(Contract contract, KeyPair key)
        {
            throw new NotImplementedException();
        }

        public override WalletAccount CreateAccount(UInt160 scriptHash)
        {
            throw new NotImplementedException();
        }

        public override bool DeleteAccount(UInt160 scriptHash)
        {
            return accounts.Remove(scriptHash);
        }

        public override WalletAccount GetAccount(UInt160 scriptHash)
        {
            return accounts.TryGetValue(scriptHash, out var account) ? account : null;
        }

        public override IEnumerable<WalletAccount> GetAccounts()
        {
            return accounts.Values;
        }

        public override bool VerifyPassword(string password)
        {
            return true;
        }
    }

    public class TestWalletAccount : WalletAccount
    {
        private readonly ECPoint publicKey;
        private readonly KeyPair keyPair;

        public TestWalletAccount(UInt160 scriptHash, ECPoint publicKey, ProtocolSettings settings)
            : base(scriptHash, settings)
        {
            this.publicKey = publicKey;

            // Create a unique private key based on the script hash for testing
            var fakePrivateKey = new byte[32];
            var hashBytes = scriptHash.ToArray();
            for (int i = 0; i < 32; i++)
                fakePrivateKey[i] = (byte)(hashBytes[i % 20] + i + 1);

            keyPair = new KeyPair(fakePrivateKey);
        }

        public override bool HasKey => true;

        public override KeyPair GetKey()
        {
            return keyPair;
        }
    }
}
