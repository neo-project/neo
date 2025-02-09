// Copyright (C) 2015-2025 The Neo Project.
//
// UT_RpcServer.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.AspNetCore.Http;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.UnitTests;
using Neo.Wallets;
using Neo.Wallets.NEP6;
using System;
using System.Text;

namespace Neo.Plugins.RpcServer.Tests
{
    [TestClass]
    public partial class UT_RpcServer
    {
        private NeoSystem _neoSystem;
        private RpcServerSettings _rpcServerSettings;
        private RpcServer _rpcServer;
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
            _rpcServerSettings = RpcServerSettings.Default with
            {
                SessionEnabled = true,
                SessionExpirationTime = TimeSpan.FromSeconds(0.3),
                MaxGasInvoke = 1500_0000_0000,
                Network = TestProtocolSettings.SoleNode.Network,
            };
            _rpcServer = new RpcServer(_neoSystem, _rpcServerSettings);
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
        public void TestCheckAuth_ValidCredentials_ReturnsTrue()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Request.Headers["Authorization"] = "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes("testuser:testpass"));
            // Act
            var result = _rpcServer.CheckAuth(context);
            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public void TestCheckAuth()
        {
            var memoryStoreProvider = new TestMemoryStoreProvider(new MemoryStore());
            var neoSystem = new NeoSystem(TestProtocolSettings.SoleNode, memoryStoreProvider);
            var rpcServerSettings = RpcServerSettings.Default with
            {
                SessionEnabled = true,
                SessionExpirationTime = TimeSpan.FromSeconds(0.3),
                MaxGasInvoke = 1500_0000_0000,
                Network = TestProtocolSettings.SoleNode.Network,
                RpcUser = "testuser",
                RpcPass = "testpass",
            };
            var rpcServer = new RpcServer(neoSystem, rpcServerSettings);

            var context = new DefaultHttpContext();
            context.Request.Headers["Authorization"] = "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes("testuser:testpass"));
            var result = rpcServer.CheckAuth(context);
            Assert.IsTrue(result);

            context.Request.Headers["Authorization"] = "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes("testuser:wrongpass"));
            result = rpcServer.CheckAuth(context);
            Assert.IsFalse(result);

            context.Request.Headers["Authorization"] = "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes("wronguser:testpass"));
            result = rpcServer.CheckAuth(context);
            Assert.IsFalse(result);

            context.Request.Headers["Authorization"] = "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes("testuser:"));
            result = rpcServer.CheckAuth(context);
            Assert.IsFalse(result);

            context.Request.Headers["Authorization"] = "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes(":testpass"));
            result = rpcServer.CheckAuth(context);
            Assert.IsFalse(result);

            context.Request.Headers["Authorization"] = "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes(""));
            result = rpcServer.CheckAuth(context);
            Assert.IsFalse(result);
        }
    }
}
