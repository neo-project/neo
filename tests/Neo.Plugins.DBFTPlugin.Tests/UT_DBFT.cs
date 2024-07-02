// Copyright (C) 2015-2024 The Neo Project.
//
// UT_RpcServer.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Akka.Actor;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Network.P2P;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.Wallets;
using Neo.Wallets.NEP6;
using System.Net;

namespace Neo.Plugins.DBFTPlugin.Tests
{
    [TestClass]
    public class UT_DBFT
    {
        private NeoSystem[] _neoSystems;
        private TestMemoryStoreProvider[] _memoryStoreProviders;
        private MemoryStore[] _memoryStores;
        private TestWalletProvider[] _walletProviders;
        private NEP6Wallet[] _wallets;
        private WalletAccount[] _walletAccounts;
        private DBFTPlugin[] _dbftPlugins;

        private readonly string[] _testWifs =
        [
            "L4Bh3SH8btGSXDGwjhmpCsNhkYctahNgkuTF1uDJpKkeyZFJpsEi",
            "L2zWf8HxYmJCH4hXE97aLaDpppgEvyk19pwC4h8bP2z7gSSdRZy8",
            "L2zXV5idFea5x7j3LCNquUdWj2GkE9nG4TxbB2MbURRN6KtwhHzN",
            "KyyUEPaufpbFLLdHJNJhgx7X69ionpv8RG7MeNrQFQqQhD3agcXS",
            "L1XoPCmNhXAdGqfWKZ5Bo3k9DmJTa3wRaf2oYrcYE1tT6C2NJx5B",
            "L3HinHgao6N8YiXMxDDxbGsXQWu4x4v5kWcs5C7u8r1cLttdbQGj",
            "L5Sdps5JkWAAkpF2t8du5eNFYtEaBcJL5CWcvWBHuouuQP3PBEc9",
            "Kye4GjtsifdMyLN8KvWyEQgPgqvEwtwR6a2ePxZLacPcKJkBGpug",
            "L2SLACky8ZXQkm2bjS6fTVP5JipKzDLToAQK8dqQc3g2S3LLvLtB",
            "Kx6NX7mcCUG6VoTpV3SmojweHBoFwCUVJ3mVPjRy4fk9C3LP5pVg"
        ];

        [TestInitialize]
        public void TestSetup()
        {
            _neoSystems = new NeoSystem[7];
            _memoryStoreProviders = new TestMemoryStoreProvider[7];
            _memoryStores = new MemoryStore[7];
            _walletProviders = new TestWalletProvider[7];
            var p2PSettings = new[]
            {
                TestP2PSettings.Node1,
                TestP2PSettings.Node2,
                TestP2PSettings.Node3,
                TestP2PSettings.Node4,
                TestP2PSettings.Node5,
                TestP2PSettings.Node6,
                TestP2PSettings.Node7,
            };

            for (int i = 0; i < 7; i++)
            {
                _walletProviders[i] = new TestWalletProvider(_testWifs[i]);
                _wallets[i] = _walletProviders[i].GetWallet() as NEP6Wallet;
                _walletAccounts[i] = _wallets[i].CreateAccount();
            }

            for (var i = 0; i < 7; i++)
            {
                _memoryStores[i] = new MemoryStore();
                _memoryStoreProviders[i] = new TestMemoryStoreProvider(_memoryStores[i]);
                var protocolSettings = TestProtocolSettings.Default;
                _neoSystems[i] = new NeoSystem(protocolSettings, _memoryStoreProviders[i]);
                _ = _neoSystems[i].LocalNode.Ask<LocalNode>(new LocalNode.GetInstance()).Result;

                _dbftPlugins[i] = new DBFTPlugin(_neoSystems[i], _walletProviders[i]);
                _dbftPlugins[i].Start(_walletProviders[i].GetWallet());

                var channelConfig = new ChannelsConfig
                {
                    Tcp = new IPEndPoint(IPAddress.Any, p2PSettings[i].Port),
                    MinDesiredConnections = p2PSettings[i].MinDesiredConnections,
                    MaxConnections = p2PSettings[i].MaxConnections,
                    MaxConnectionsPerAddress = p2PSettings[i].MaxConnectionsPerAddress
                };
                _neoSystems[i].StartNode(channelConfig);

                var snapshot = _neoSystems[i].GetSnapshot();

                // Assign every single consensus node some GAS
                for (var j = 0; j < 7; j++)
                {
                    var key = new KeyBuilder(NativeContract.GAS.Id, 20).Add(_walletAccounts[j].ScriptHash);
                    var entry = snapshot.GetAndChange(key, () => new StorageItem(new AccountState()));
                    entry.GetInteroperable<AccountState>().Balance = 100_000_000 * NativeContract.GAS.Factor;
                }
                snapshot.Commit();
            }
        }

        [TestCleanup]
        public void TestCleanup()
        {
            for (var i = 0; i < 7; i++)
            {
                _memoryStores[i].Reset();
                var snapshot = _neoSystems[i].GetSnapshot();
                for (var j = 0; j < 7; j++)
                {
                    var key = new KeyBuilder(NativeContract.GAS.Id, 20).Add(_walletAccounts[i].ScriptHash);
                    var entry = snapshot.GetAndChange(key, () => new StorageItem(new AccountState()));
                    entry.GetInteroperable<AccountState>().Balance = 100_000_000 * NativeContract.GAS.Factor;
                }
                snapshot.Commit();
            }
        }
    }
}
