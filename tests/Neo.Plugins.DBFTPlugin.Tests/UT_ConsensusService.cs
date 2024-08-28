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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Neo.IO;
using Neo.Ledger;
using Neo.Persistence;
using Neo.Plugins.DBFTPlugin.Consensus;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.UnitTests;
using Neo.UnitTests;
using Neo.Wallets;
using Neo.Wallets.NEP6;
using System;
using System.Linq;
using System.Net;
using System.Numerics;
using System.Text;

namespace Neo.Plugins.DBFTPlugin.Tests
{
    [TestClass]
    public partial class UT_ConsensusService
    {
        private NeoSystem _neoSystem;
        private TestMemoryStoreProvider _memoryStoreProvider;
        private MemoryStore _memoryStore;
        private readonly NEP6Wallet _wallet = TestUtils.GenerateTestWallet("123");
        private WalletAccount _walletAccount;

        const byte NativePrefixAccount = 20;
        const byte NativePrefixTotalSupply = 11;

        [TestInitialize]
        public void TestSetup()
        {
            _memoryStore = new MemoryStore();
            _memoryStoreProvider = new TestMemoryStoreProvider(_memoryStore);
            _neoSystem = new NeoSystem(TestProtocolSettings.SoleNode, _memoryStoreProvider);
            _walletAccount = _wallet.Import("KxuRSsHgJMb3AMSN6B9P3JHNGMFtxmuimqgR9MmXPcv3CLLfusTd");
            var key = new KeyBuilder(NativeContract.GAS.Id, 20).Add(_walletAccount.ScriptHash);
            var snapshot = _neoSystem.GetSnapshotCache();
            var entry = snapshot.GetAndChange(key, () => new StorageItem(new AccountState()));
            entry.GetInteroperable<AccountState>().Balance = 100_000_000 * NativeContract.GAS.Factor;
            snapshot.Commit();



        }

        [TestCleanup]
        public void TestCleanup()
        {
            // Please build and test in debug mode
            _neoSystem.MemPool.Clear();
            _memoryStore.Reset();
            var snapshot = _neoSystem.GetSnapshotCache();
            var key = new KeyBuilder(NativeContract.GAS.Id, 20).Add(_walletAccount.ScriptHash);
            var entry = snapshot.GetAndChange(key, () => new StorageItem(new AccountState()));
            entry.GetInteroperable<AccountState>().Balance = 100_000_000 * NativeContract.GAS.Factor;
            snapshot.Commit();
        }

        [TestMethod]
        public void ConsensusService_SingleNodeActors_OnStart_PrepReq_PrepResponses_Commits()
        {
            var mockNeoSystem = new Mock<TestNeoSystem>();

            var mockWallet = new Mock<Wallet>();
            mockWallet.Setup(p => p.GetAccount(It.IsAny<UInt160>())).Returns<UInt160>(p => new TestWalletAccount(p));
            Console.WriteLine($"\n(UT-Consensus) Wallet is: {mockWallet.Object.GetAccount(UInt160.Zero).GetKey().PublicKey}");

            var mockContext = new Mock<ConsensusContext>(mockNeoSystem.Object, mockWallet.Object, ProtocolSettings.Default);

            var timeValues = new[] {
            new DateTime(1980, 06, 01, 0, 0, 1, 001, DateTimeKind.Utc),  // For tests, used below
            new DateTime(1980, 06, 01, 0, 0, 3, 001, DateTimeKind.Utc),  // For receiving block
            new DateTime(1980, 05, 01, 0, 0, 5, 001, DateTimeKind.Utc),  // For Initialize
            new DateTime(1980, 06, 01, 0, 0, 15, 001, DateTimeKind.Utc), // unused
                    };
            for (var i = 0; i < timeValues.Length; i++)
                Console.WriteLine($"time {i}: {timeValues[i]} ");



        }
    }


}
