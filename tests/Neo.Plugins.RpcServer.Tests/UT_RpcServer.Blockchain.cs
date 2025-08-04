// Copyright (C) 2015-2025 The Neo Project.
//
// UT_RpcServer.Blockchain.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Akka.Actor;
using Akka.Util.Internal;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Cryptography.ECC;
using Neo.Extensions;
using Neo.Json;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.Plugins.RpcServer.Model;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.UnitTests;
using Neo.UnitTests.Extensions;
using System;
using System.Linq;
using System.Threading;
using static Neo.SmartContract.Native.NeoToken;

namespace Neo.Plugins.RpcServer.Tests
{
    public partial class UT_RpcServer
    {

        [TestMethod]
        public void TestGetBestBlockHash()
        {
            var key = NativeContract.Ledger.CreateStorageKey(12);
            var expectedHash = UInt256.Zero;

            var snapshot = _neoSystem.GetSnapshotCache();
            var b = snapshot.GetAndChange(key, () => new(new HashIndexState())).GetInteroperable<HashIndexState>();
            b.Hash = UInt256.Zero;
            b.Index = 100;
            snapshot.Commit();

            var result = _rpcServer.GetBestBlockHash();
            // Assert
            Assert.AreEqual(expectedHash.ToString(), result.AsString());
        }

        [TestMethod]
        public void TestGetBlockByHash()
        {
            var snapshot = _neoSystem.GetSnapshotCache();
            var block = TestUtils.CreateBlockWithValidTransactions(snapshot, _wallet, _walletAccount, 3);
            TestUtils.BlocksAdd(snapshot, block.Hash, block);
            snapshot.Commit();

            var result = _rpcServer.GetBlock(new BlockHashOrIndex(block.Hash), false);
            var blockArr = Convert.FromBase64String(result.AsString());
            var block2 = blockArr.AsSerializable<Block>();
            block2.Transactions.ForEach(tx =>
            {
                Assert.AreEqual(VerifyResult.Succeed, tx.VerifyStateIndependent(TestProtocolSettings.Default));
            });

            result = _rpcServer.GetBlock(new BlockHashOrIndex(block.Hash), true);
            var block3 = block.ToJson(TestProtocolSettings.Default);
            block3["confirmations"] = NativeContract.Ledger.CurrentIndex(snapshot) - block.Index + 1;
            Assert.AreEqual(block3.ToString(), result.ToString());
        }

        [TestMethod]
        public void TestGetBlockByIndex()
        {
            var snapshot = _neoSystem.GetSnapshotCache();
            var block = TestUtils.CreateBlockWithValidTransactions(snapshot, _wallet, _walletAccount, 3);
            TestUtils.BlocksAdd(snapshot, block.Hash, block);
            snapshot.Commit();

            var result = _rpcServer.GetBlock(new BlockHashOrIndex(block.Index), false);
            var blockArr = Convert.FromBase64String(result.AsString());
            var block2 = blockArr.AsSerializable<Block>();
            block2.Transactions.ForEach(tx =>
            {
                Assert.AreEqual(VerifyResult.Succeed, tx.VerifyStateIndependent(TestProtocolSettings.Default));
            });

            result = _rpcServer.GetBlock(new BlockHashOrIndex(block.Index), true);
            var block3 = block.ToJson(TestProtocolSettings.Default);
            block3["confirmations"] = NativeContract.Ledger.CurrentIndex(snapshot) - block.Index + 1;
            Assert.AreEqual(block3.ToString(), result.ToString());
        }

        [TestMethod]
        public void TestGetBlock_Genesis()
        {
            var snapshot = _neoSystem.GetSnapshotCache();
            var genesisBlock = NativeContract.Ledger.GetBlock(snapshot, 0);

            // Test non-verbose
            var resultNonVerbose = _rpcServer.GetBlock(new BlockHashOrIndex(0), false);
            var blockArr = Convert.FromBase64String(resultNonVerbose.AsString());
            var deserializedBlock = blockArr.AsSerializable<Block>();
            Assert.AreEqual(genesisBlock.Hash, deserializedBlock.Hash);

            // Test verbose
            var resultVerbose = _rpcServer.GetBlock(new BlockHashOrIndex(0), true);
            var expectedJson = genesisBlock.ToJson(TestProtocolSettings.Default);
            expectedJson["confirmations"] = NativeContract.Ledger.CurrentIndex(snapshot) - genesisBlock.Index + 1;
            Assert.AreEqual(expectedJson["hash"].AsString(), resultVerbose["hash"].AsString());
            Assert.AreEqual(expectedJson["size"].AsNumber(), resultVerbose["size"].AsNumber());
            Assert.AreEqual(expectedJson["version"].AsNumber(), resultVerbose["version"].AsNumber());
            Assert.AreEqual(expectedJson["merkleroot"].AsString(), resultVerbose["merkleroot"].AsString());
            Assert.AreEqual(expectedJson["confirmations"].AsNumber(), resultVerbose["confirmations"].AsNumber());
            // Genesis block should have 0 transactions
            Assert.AreEqual(0, ((JArray)resultVerbose["tx"]).Count);
        }

        [TestMethod]
        public void TestGetBlock_NoTransactions()
        {
            var snapshot = _neoSystem.GetSnapshotCache();
            // Create a block with index 1 (after genesis) with no transactions
            var block = new Block
            {
                Header = new Header
                {
                    Version = 0,
                    PrevHash = NativeContract.Ledger.CurrentHash(snapshot),
                    MerkleRoot = UInt256.Zero, // No transactions
                    Timestamp = DateTime.UtcNow.ToTimestampMS(),
                    Index = NativeContract.Ledger.CurrentIndex(snapshot) + 1,
                    NextConsensus = UInt160.Zero, // Simplified for test
                    Witness = Witness.Empty
                },
                Transactions = []
            };

            TestUtils.BlocksAdd(snapshot, block.Hash, block);
            snapshot.Commit();

            // Test non-verbose
            var resultNonVerbose = _rpcServer.GetBlock(new BlockHashOrIndex(block.Index), false);
            var blockArr = Convert.FromBase64String(resultNonVerbose.AsString());
            var deserializedBlock = blockArr.AsSerializable<Block>();
            Assert.AreEqual(block.Hash, deserializedBlock.Hash);
            Assert.AreEqual(0, deserializedBlock.Transactions.Length);

            // Test verbose
            var resultVerbose = _rpcServer.GetBlock(new BlockHashOrIndex(block.Index), true);
            var expectedJson = block.ToJson(TestProtocolSettings.Default);
            expectedJson["confirmations"] = NativeContract.Ledger.CurrentIndex(snapshot) - block.Index + 1;
            Assert.AreEqual(expectedJson["hash"].AsString(), resultVerbose["hash"].AsString());
            Assert.AreEqual(0, ((JArray)resultVerbose["tx"]).Count);

            var ex = Assert.ThrowsExactly<RpcException>(() => _rpcServer.GetBlock(null, true));
            Assert.AreEqual(RpcError.InvalidParams.Code, ex.HResult);
        }

        [TestMethod]
        public void TestGetBlockCount()
        {
            var expectedCount = 1;
            var result = _rpcServer.GetBlockCount();
            Assert.AreEqual(expectedCount, result.AsNumber());
        }

        [TestMethod]
        public void TestGetBlockHeaderCount()
        {
            var expectedCount = 1;
            var result = _rpcServer.GetBlockHeaderCount();
            Assert.AreEqual(expectedCount, result.AsNumber());
        }

        [TestMethod]
        public void TestGetBlockHash()
        {
            var snapshot = _neoSystem.GetSnapshotCache();
            var block = TestUtils.CreateBlockWithValidTransactions(snapshot, _wallet, _walletAccount, 3);
            // TestUtils.BlocksAdd(snapshot, block.Hash, block);
            // snapshot.Commit();
            var reason = _neoSystem.Blockchain.Ask<Blockchain.RelayResult>(block, cancellationToken: CancellationToken.None).Result;
            var expectedHash = block.Hash.ToString();
            var result = _rpcServer.GetBlockHash(block.Index);
            Assert.AreEqual(expectedHash, result.AsString());
        }

        [TestMethod]
        public void TestGetBlockHeader()
        {
            var snapshot = _neoSystem.GetSnapshotCache();
            var block = TestUtils.CreateBlockWithValidTransactions(snapshot, _wallet, _walletAccount, 3);
            TestUtils.BlocksAdd(snapshot, block.Hash, block);
            snapshot.Commit();

            var result = _rpcServer.GetBlockHeader(new BlockHashOrIndex(block.Hash), true);
            var header = block.Header.ToJson(_neoSystem.Settings);
            header["confirmations"] = NativeContract.Ledger.CurrentIndex(snapshot) - block.Index + 1;
            Assert.AreEqual(header.ToString(), result.ToString());

            result = _rpcServer.GetBlockHeader(new BlockHashOrIndex(block.Hash), false);
            var headerArr = Convert.FromBase64String(result.AsString());
            var header2 = headerArr.AsSerializable<Header>();
            Assert.AreEqual(block.Header.ToJson(_neoSystem.Settings).ToString(), header2.ToJson(_neoSystem.Settings).ToString());

            var ex = Assert.ThrowsExactly<RpcException>(() => _rpcServer.GetBlockHeader(null, true));
            Assert.AreEqual(RpcError.InvalidParams.Code, ex.HResult);
        }

        [TestMethod]
        public void TestGetContractState()
        {
            var snapshot = _neoSystem.GetSnapshotCache();
            var contractState = TestUtils.GetContract();
            snapshot.AddContract(contractState.Hash, contractState);
            snapshot.Commit();

            var result = _rpcServer.GetContractState(new ContractNameOrHashOrId(contractState.Hash));
            Assert.AreEqual(contractState.ToJson().ToString(), result.ToString());

            result = _rpcServer.GetContractState(new ContractNameOrHashOrId(contractState.Id));
            Assert.AreEqual(contractState.ToJson().ToString(), result.ToString());

            var byId = _rpcServer.GetContractState(new ContractNameOrHashOrId(-1));
            var byName = _rpcServer.GetContractState(new ContractNameOrHashOrId("ContractManagement"));
            Assert.AreEqual(byId.ToString(), byName.ToString());

            snapshot.DeleteContract(contractState.Hash);
            snapshot.Commit();
            var ex1 = Assert.ThrowsExactly<RpcException>(() => _ = _rpcServer.GetContractState(new(contractState.Hash)));
            Assert.AreEqual(RpcError.UnknownContract.Message, ex1.Message);

            var ex2 = Assert.ThrowsExactly<RpcException>(() => _ = _rpcServer.GetContractState(new(contractState.Id)));
            Assert.AreEqual(RpcError.UnknownContract.Message, ex2.Message);

            var ex3 = Assert.ThrowsExactly<RpcException>(() => _ = _rpcServer.GetContractState(null));
            Assert.AreEqual(RpcError.InvalidParams.Code, ex3.HResult);
        }

        [TestMethod]
        public void TestGetContractState_Native_CaseInsensitive()
        {
            var gasTokenHash = NativeContract.GAS.Hash;
            var resultLower = _rpcServer.GetContractState(new ContractNameOrHashOrId("gastoken"));
            var resultUpper = _rpcServer.GetContractState(new ContractNameOrHashOrId("GASTOKEN"));
            var resultMixed = _rpcServer.GetContractState(new ContractNameOrHashOrId("GasToken"));

            Assert.AreEqual(gasTokenHash.ToString(), ((JObject)resultLower)["hash"].AsString());
            Assert.AreEqual(gasTokenHash.ToString(), ((JObject)resultUpper)["hash"].AsString());
            Assert.AreEqual(gasTokenHash.ToString(), ((JObject)resultMixed)["hash"].AsString());
        }

        [TestMethod]
        public void TestGetContractState_InvalidFormat()
        {
            // Invalid Hash format (not hex)
            var exHash = Assert.ThrowsExactly<FormatException>(
                () => _ = _rpcServer.GetContractState(new("0xInvalidHashString")));

            // Invalid ID format (not integer - although ContractNameOrHashOrId constructor might catch this)
            // Assuming the input could come as a JValue string that fails parsing later
            // For now, let's test with an invalid name that doesn't match natives or parse as hash/id
            var exName = Assert.ThrowsExactly<FormatException>(
                () => _ = _rpcServer.GetContractState(new("InvalidContractName")));
        }

        [TestMethod]
        public void TestGetRawMemPool()
        {
            var snapshot = _neoSystem.GetSnapshotCache();
            var tx = TestUtils.CreateValidTx(snapshot, _wallet, _walletAccount);
            snapshot.Commit();
            _neoSystem.MemPool.TryAdd(tx, snapshot);

            var result = _rpcServer.GetRawMemPool();
            Assert.IsTrue(((JArray)result).Any(p => p.AsString() == tx.Hash.ToString()));

            result = _rpcServer.GetRawMemPool(true);
            Assert.IsTrue(((JArray)result["verified"]).Any(p => p.AsString() == tx.Hash.ToString()));
        }

        [TestMethod]
        public void TestGetRawMemPool_Empty()
        {
            // Ensure mempool is clear (redundant with TestCleanup but good for clarity)
            _neoSystem.MemPool.Clear();

            // Test without unverified
            var result = _rpcServer.GetRawMemPool();
            Assert.IsInstanceOfType(result, typeof(JArray));
            Assert.AreEqual(0, ((JArray)result).Count);

            // Test with unverified
            result = _rpcServer.GetRawMemPool(true);
            Assert.IsInstanceOfType(result, typeof(JObject));
            Assert.AreEqual(0, ((JArray)((JObject)result)["verified"]).Count);
            Assert.AreEqual(0, ((JArray)((JObject)result)["unverified"]).Count);
            Assert.IsTrue(((JObject)result).ContainsProperty("height"));
        }

        [TestMethod]
        public void TestGetRawMemPool_MixedVerifiedUnverified()
        {
            var snapshot = _neoSystem.GetSnapshotCache();
            _neoSystem.MemPool.Clear();

            // Add two distinct transactions
            var tx1 = TestUtils.CreateValidTx(snapshot, _wallet, _walletAccount.ScriptHash, nonce: 1);
            var tx2 = TestUtils.CreateValidTx(snapshot, _wallet, _walletAccount.ScriptHash, nonce: 2);
            _neoSystem.MemPool.TryAdd(tx1, snapshot);
            _neoSystem.MemPool.TryAdd(tx2, snapshot);
            snapshot.Commit();

            // Get the expected state directly from the mempool
            _neoSystem.MemPool.GetVerifiedAndUnverifiedTransactions(out var verified, out var unverified);
            int expectedVerifiedCount = verified.Count();
            int expectedUnverifiedCount = unverified.Count();
            var expectedVerifiedHashes = verified.Select(tx => tx.Hash.ToString()).ToHashSet();
            var expectedUnverifiedHashes = unverified.Select(tx => tx.Hash.ToString()).ToHashSet();

            Assert.IsTrue(expectedVerifiedCount + expectedUnverifiedCount > 0, "Test setup failed: No transactions in mempool");

            // Call the RPC method
            var result = _rpcServer.GetRawMemPool(true);
            Assert.IsInstanceOfType(result, typeof(JObject));
            var actualVerifiedHashes = ((JArray)((JObject)result)["verified"]).Select(p => p.AsString()).ToHashSet();
            var actualUnverifiedHashes = ((JArray)((JObject)result)["unverified"]).Select(p => p.AsString()).ToHashSet();

            // Assert counts and contents match the pool's state
            Assert.AreEqual(expectedVerifiedCount, actualVerifiedHashes.Count);
            Assert.AreEqual(expectedUnverifiedCount, actualUnverifiedHashes.Count);
            CollectionAssert.AreEquivalent(expectedVerifiedHashes.ToList(), actualVerifiedHashes.ToList());
            CollectionAssert.AreEquivalent(expectedUnverifiedHashes.ToList(), actualUnverifiedHashes.ToList());
        }

        [TestMethod]
        public void TestGetRawTransaction()
        {
            var snapshot = _neoSystem.GetSnapshotCache();
            var tx = TestUtils.CreateValidTx(snapshot, _wallet, _walletAccount);
            _neoSystem.MemPool.TryAdd(tx, snapshot);
            snapshot.Commit();

            var result = _rpcServer.GetRawTransaction(tx.Hash, true);
            var json = Utility.TransactionToJson(tx, _neoSystem.Settings);
            Assert.AreEqual(json.ToString(), result.ToString());

            result = _rpcServer.GetRawTransaction(tx.Hash, false);
            var tx2 = Convert.FromBase64String(result.AsString()).AsSerializable<Transaction>();
            Assert.AreEqual(tx.ToJson(_neoSystem.Settings).ToString(), tx2.ToJson(_neoSystem.Settings).ToString());

            var ex = Assert.ThrowsExactly<RpcException>(() => _ = _rpcServer.GetRawTransaction(null, true));
            Assert.AreEqual(RpcError.InvalidParams.Code, ex.HResult);
        }

        [TestMethod]
        public void TestGetRawTransaction_Confirmed()
        {
            var snapshot = _neoSystem.GetSnapshotCache();
            var block = TestUtils.CreateBlockWithValidTransactions(snapshot, _wallet, _walletAccount, 1);
            TestUtils.BlocksAdd(snapshot, block.Hash, block);
            snapshot.Commit();
            var tx = block.Transactions[0];

            // Test non-verbose
            var resultNonVerbose = _rpcServer.GetRawTransaction(tx.Hash, false);
            var txArr = Convert.FromBase64String(resultNonVerbose.AsString());
            var deserializedTx = txArr.AsSerializable<Transaction>();
            Assert.AreEqual(tx.Hash, deserializedTx.Hash);

            // Test verbose
            var resultVerbose = _rpcServer.GetRawTransaction(tx.Hash, true);
            var expectedJson = Utility.TransactionToJson(tx, _neoSystem.Settings);
            // Add expected block-related fields
            expectedJson["blockhash"] = block.Hash.ToString();
            expectedJson["confirmations"] = NativeContract.Ledger.CurrentIndex(_neoSystem.StoreView) - block.Index + 1;
            expectedJson["blocktime"] = block.Header.Timestamp;

            Assert.IsInstanceOfType(resultVerbose, typeof(JObject));
            Assert.AreEqual(expectedJson.ToString(), resultVerbose.ToString()); // Compare full JSON for simplicity here
            Assert.AreEqual(block.Hash.ToString(), ((JObject)resultVerbose)["blockhash"].AsString());
            Assert.AreEqual(expectedJson["confirmations"].AsNumber(), ((JObject)resultVerbose)["confirmations"].AsNumber());
            Assert.AreEqual(block.Header.Timestamp, ((JObject)resultVerbose)["blocktime"].AsNumber());
        }

        [TestMethod]
        public void TestGetStorage()
        {
            var snapshot = _neoSystem.GetSnapshotCache();
            var contractState = TestUtils.GetContract();
            snapshot.AddContract(contractState.Hash, contractState);
            var key = new byte[] { 0x01 };
            var value = new byte[] { 0x02 };
            TestUtils.StorageItemAdd(snapshot, contractState.Id, key, value);
            snapshot.Commit();

            var result = _rpcServer.GetStorage(new(contractState.Hash), Convert.ToBase64String(key));
            Assert.AreEqual(Convert.ToBase64String(value), result.AsString());

            var ex = Assert.ThrowsExactly<RpcException>(() => _ = _rpcServer.GetStorage(null, Convert.ToBase64String(key)));
            Assert.AreEqual(RpcError.InvalidParams.Code, ex.HResult);

            var ex2 = Assert.ThrowsExactly<RpcException>(() => _ = _rpcServer.GetStorage(new(contractState.Hash), null));
            Assert.AreEqual(RpcError.InvalidParams.Code, ex2.HResult);
        }

        [TestMethod]
        public void TestFindStorage()
        {
            var snapshot = _neoSystem.GetSnapshotCache();
            var contractState = TestUtils.GetContract();
            snapshot.AddContract(contractState.Hash, contractState);
            var key = new byte[] { 0x01 };
            var value = new byte[] { 0x02 };
            TestUtils.StorageItemAdd(snapshot, contractState.Id, key, value);
            snapshot.Commit();
            var result = _rpcServer.FindStorage(new(contractState.Hash), Convert.ToBase64String(key), 0);

            var jarr = new JArray();
            var j = new JObject()
            {
                ["key"] = Convert.ToBase64String(key),
                ["value"] = Convert.ToBase64String(value),
            };
            jarr.Add(j);

            var json = new JObject()
            {
                ["truncated"] = false,
                ["next"] = 1,
                ["results"] = jarr,
            };
            Assert.AreEqual(json.ToString(), result.ToString());

            var result2 = _rpcServer.FindStorage(new(contractState.Hash), Convert.ToBase64String(key));
            Assert.AreEqual(result.ToString(), result2.ToString());

            Enumerable.Range(0, 51)
                .ToList()
                .ForEach(i => TestUtils.StorageItemAdd(snapshot, contractState.Id, [0x01, (byte)i], [0x02]));
            snapshot.Commit();
            var result4 = _rpcServer.FindStorage(new(contractState.Hash), Convert.ToBase64String(new byte[] { 0x01 }), 0);
            Assert.AreEqual(RpcServersSettings.Default.FindStoragePageSize, result4["next"].AsNumber());
            Assert.IsTrue(result4["truncated"].AsBoolean());
        }

        [TestMethod]
        public void TestStorage_NativeContractName()
        {
            var snapshot = _neoSystem.GetSnapshotCache();
            var key = new byte[] { 0x01 };
            var value = new byte[] { 0x02 };
            TestUtils.StorageItemAdd(snapshot, NativeContract.GAS.Id, key, value);
            snapshot.Commit();

            // GetStorage
            var result = _rpcServer.GetStorage(new("GasToken"), Convert.ToBase64String(key));
            Assert.AreEqual(Convert.ToBase64String(value), result.AsString());

            var ex = Assert.ThrowsExactly<RpcException>(() => _ = _rpcServer.GetStorage(null, Convert.ToBase64String(key)));
            Assert.AreEqual(RpcError.InvalidParams.Code, ex.HResult);

            ex = Assert.ThrowsExactly<RpcException>(() => _ = _rpcServer.GetStorage(new("GasToken"), null));
            Assert.AreEqual(RpcError.InvalidParams.Code, ex.HResult);

            // FindStorage
            var result2 = _rpcServer.FindStorage(new("GasToken"), Convert.ToBase64String(key), 0);
            Assert.AreEqual(Convert.ToBase64String(value), result2["results"][0]["value"].AsString());

            ex = Assert.ThrowsExactly<RpcException>(() => _ = _rpcServer.FindStorage(null, Convert.ToBase64String(key), 0));
            Assert.AreEqual(RpcError.InvalidParams.Code, ex.HResult);

            ex = Assert.ThrowsExactly<RpcException>(() => _ = _rpcServer.FindStorage(new("GasToken"), null, 0));
            Assert.AreEqual(RpcError.InvalidParams.Code, ex.HResult);
        }

        [TestMethod]
        public void TestFindStorage_Pagination()
        {
            var snapshot = _neoSystem.GetSnapshotCache();
            var contractState = TestUtils.GetContract();
            snapshot.AddContract(contractState.Hash, contractState);
            var prefix = new byte[] { 0xAA };
            int totalItems = RpcServersSettings.Default.FindStoragePageSize + 5;

            for (int i = 0; i < totalItems; i++)
            {
                var key = prefix.Concat(BitConverter.GetBytes(i)).ToArray();
                var value = BitConverter.GetBytes(i);
                TestUtils.StorageItemAdd(snapshot, contractState.Id, key, value);
            }
            snapshot.Commit();

            // Get first page
            var resultPage1 = _rpcServer.FindStorage(new(contractState.Hash), Convert.ToBase64String(prefix), 0);
            Assert.IsTrue(resultPage1["truncated"].AsBoolean());
            Assert.AreEqual(RpcServersSettings.Default.FindStoragePageSize, ((JArray)resultPage1["results"]).Count);
            int nextIndex = (int)resultPage1["next"].AsNumber();
            Assert.AreEqual(RpcServersSettings.Default.FindStoragePageSize, nextIndex);

            // Get second page
            var resultPage2 = _rpcServer.FindStorage(new(contractState.Hash), Convert.ToBase64String(prefix), nextIndex);
            Assert.IsFalse(resultPage2["truncated"].AsBoolean());
            Assert.AreEqual(5, ((JArray)resultPage2["results"]).Count);
            Assert.AreEqual(totalItems, (int)resultPage2["next"].AsNumber()); // Next should be total count
        }

        [TestMethod]
        public void TestFindStorage_Pagination_End()
        {
            var snapshot = _neoSystem.GetSnapshotCache();
            var contractState = TestUtils.GetContract();
            snapshot.AddContract(contractState.Hash, contractState);
            var prefix = new byte[] { 0xBB };
            int totalItems = 3;

            for (int i = 0; i < totalItems; i++)
            {
                var key = prefix.Concat(BitConverter.GetBytes(i)).ToArray();
                var value = BitConverter.GetBytes(i);
                TestUtils.StorageItemAdd(snapshot, contractState.Id, key, value);
            }
            snapshot.Commit();

            // Get all items (assuming page size is larger than 3)
            var resultPage1 = _rpcServer.FindStorage(new(contractState.Hash), Convert.ToBase64String(prefix), 0);
            Assert.IsFalse(resultPage1["truncated"].AsBoolean());
            Assert.AreEqual(totalItems, ((JArray)resultPage1["results"]).Count);
            int nextIndex = (int)resultPage1["next"].AsNumber();
            Assert.AreEqual(totalItems, nextIndex);

            // Try to get next page (should be empty)
            var resultPage2 = _rpcServer.FindStorage(new(contractState.Hash), Convert.ToBase64String(prefix), nextIndex);
            Assert.IsFalse(resultPage2["truncated"].AsBoolean());
            Assert.AreEqual(0, ((JArray)resultPage2["results"]).Count);
            Assert.AreEqual(nextIndex, (int)resultPage2["next"].AsNumber()); // Next index should remain the same

            var ex = Assert.ThrowsExactly<RpcException>(
                () => _ = _rpcServer.FindStorage(null, Convert.ToBase64String(prefix), 0));
            Assert.AreEqual(RpcError.InvalidParams.Code, ex.HResult);

            var ex2 = Assert.ThrowsExactly<RpcException>(
                () => _ = _rpcServer.FindStorage(new(contractState.Hash), null, 0));
            Assert.AreEqual(RpcError.InvalidParams.Code, ex2.HResult);
        }

        [TestMethod]
        public void TestGetTransactionHeight()
        {
            var snapshot = _neoSystem.GetSnapshotCache();
            var block = TestUtils.CreateBlockWithValidTransactions(snapshot, _wallet, _walletAccount, 1);
            TestUtils.BlocksAdd(snapshot, block.Hash, block);
            snapshot.Commit();
            var tx = block.Transactions[0];
            var result = _rpcServer.GetTransactionHeight(tx.Hash);
            Assert.AreEqual(block.Index, result.AsNumber());
        }

        [TestMethod]
        public void TestGetTransactionHeight_MempoolOnly()
        {
            var snapshot = _neoSystem.GetSnapshotCache();
            var tx = TestUtils.CreateValidTx(snapshot, _wallet, _walletAccount.ScriptHash, nonce: 100);
            _neoSystem.MemPool.TryAdd(tx, snapshot);
            snapshot.Commit();

            // Transaction is in mempool but not ledger, should throw UnknownTransaction
            var ex = Assert.ThrowsExactly<RpcException>(() => _rpcServer.GetTransactionHeight(tx.Hash));
            Assert.AreEqual(RpcError.UnknownTransaction.Code, ex.HResult);
        }

        [TestMethod]
        public void TestGetNextBlockValidators()
        {
            var snapshot = _neoSystem.GetSnapshotCache();
            var result = _rpcServer.GetNextBlockValidators();

            var validators = NativeContract.NEO.GetNextBlockValidators(snapshot, _neoSystem.Settings.ValidatorsCount);
            var expected = validators.Select(p =>
            {
                return new JObject()
                {
                    ["publickey"] = p.ToString(),
                    ["votes"] = (int)NativeContract.NEO.GetCandidateVote(snapshot, p),
                };
            }).ToArray();
            Assert.AreEqual(new JArray(expected).ToString(), result.ToString());
        }

        [TestMethod]
        public void TestGetCandidates()
        {
            var snapshot = _neoSystem.GetSnapshotCache();
            var json = new JArray();
            var validators = NativeContract.NEO.GetNextBlockValidators(snapshot, _neoSystem.Settings.ValidatorsCount);

            var key = new KeyBuilder(NativeContract.NEO.Id, 33)
                .Add(ECPoint.Parse("02237309a0633ff930d51856db01d17c829a5b2e5cc2638e9c03b4cfa8e9c9f971", ECCurve.Secp256r1));
            snapshot.Add(key, new StorageItem(new CandidateState() { Registered = true, Votes = 10000 }));
            snapshot.Commit();

            var candidates = NativeContract.NEO.GetCandidates(_neoSystem.GetSnapshotCache());
            var result = _rpcServer.GetCandidates();
            foreach (var candidate in candidates)
            {
                var item = new JObject()
                {
                    ["publickey"] = candidate.PublicKey.ToString(),
                    ["votes"] = candidate.Votes.ToString(),
                    ["active"] = validators.Contains(candidate.PublicKey),
                };
                json.Add(item);
            }
            Assert.AreEqual(json.ToString(), result.ToString());
        }

        [TestMethod]
        public void TestGetCommittee()
        {
            var snapshot = _neoSystem.GetSnapshotCache();
            var result = _rpcServer.GetCommittee();
            var committee = NativeContract.NEO.GetCommittee(snapshot);
            var expected = new JArray(committee.Select(p => (JToken)p.ToString()));
            Assert.AreEqual(expected.ToString(), result.ToString());
        }

        [TestMethod]
        public void TestGetNativeContracts()
        {
            var snapshot = _neoSystem.GetSnapshotCache();
            var result = _rpcServer.GetNativeContracts();
            var states = NativeContract.Contracts
                .Select(p => NativeContract.ContractManagement.GetContract(snapshot, p.Hash).ToJson());
            var contracts = new JArray(states);
            Assert.AreEqual(contracts.ToString(), result.ToString());
        }

        [TestMethod]
        public void TestGetBlockByUnknownIndex()
        {
            var snapshot = _neoSystem.GetSnapshotCache();
            var block = TestUtils.CreateBlockWithValidTransactions(snapshot, _wallet, _walletAccount, 3);
            TestUtils.BlocksAdd(snapshot, block.Hash, block);
            snapshot.Commit();

            try
            {
                _rpcServer.GetBlock(new BlockHashOrIndex(int.MaxValue), false);
                Assert.Fail("Expected RpcException was not thrown.");
            }
            catch (RpcException ex)
            {
                Assert.AreEqual(RpcError.UnknownBlock.Code, ex.HResult);
            }
        }

        [TestMethod]
        public void TestGetBlockByUnknownHash()
        {
            var snapshot = _neoSystem.GetSnapshotCache();
            var block = TestUtils.CreateBlockWithValidTransactions(snapshot, _wallet, _walletAccount, 3);
            TestUtils.BlocksAdd(snapshot, block.Hash, block);
            snapshot.Commit();

            try
            {
                _rpcServer.GetBlock(new BlockHashOrIndex(TestUtils.RandomUInt256()), false);
                Assert.Fail("Expected RpcException was not thrown.");
            }
            catch (RpcException ex)
            {
                Assert.AreEqual(RpcError.UnknownBlock.Code, ex.HResult);
            }
        }

        [TestMethod]
        public void TestGetBlockByUnKnownIndex()
        {
            var snapshot = _neoSystem.GetSnapshotCache();
            var block = TestUtils.CreateBlockWithValidTransactions(snapshot, _wallet, _walletAccount, 3);
            TestUtils.BlocksAdd(snapshot, block.Hash, block);
            snapshot.Commit();

            try
            {
                _rpcServer.GetBlock(new BlockHashOrIndex(int.MaxValue), false);
                Assert.Fail("Expected RpcException was not thrown.");
            }
            catch (RpcException ex)
            {
                Assert.AreEqual(RpcError.UnknownBlock.Code, ex.HResult);
            }
        }

        [TestMethod]
        public void TestGetBlockByUnKnownHash()
        {
            var snapshot = _neoSystem.GetSnapshotCache();
            var block = TestUtils.CreateBlockWithValidTransactions(snapshot, _wallet, _walletAccount, 3);
            TestUtils.BlocksAdd(snapshot, block.Hash, block);
            snapshot.Commit();

            try
            {
                _rpcServer.GetBlock(new BlockHashOrIndex(TestUtils.RandomUInt256()), false);
                Assert.Fail("Expected RpcException was not thrown.");
            }
            catch (RpcException ex)
            {
                Assert.AreEqual(RpcError.UnknownBlock.Code, ex.HResult);
            }
        }

        [TestMethod]
        public void TestGetBlockHashInvalidIndex()
        {
            var snapshot = _neoSystem.GetSnapshotCache();
            var block = TestUtils.CreateBlockWithValidTransactions(snapshot, _wallet, _walletAccount, 3);
            TestUtils.BlocksAdd(snapshot, block.Hash, block);
            snapshot.Commit();
            Assert.ThrowsExactly<RpcException>(() => _ = _rpcServer.GetBlockHash(block.Index + 1));
        }

        [TestMethod]
        public void TestGetContractStateUnknownContract()
        {
            var snapshot = _neoSystem.GetSnapshotCache();
            var randomHash = TestUtils.RandomUInt160();
            try
            {
                _rpcServer.GetContractState(new ContractNameOrHashOrId(randomHash));
                Assert.Fail("Expected RpcException was not thrown.");
            }
            catch (RpcException ex)
            {
                Assert.AreEqual(RpcError.UnknownContract.Code, ex.HResult);
            }
        }

        [TestMethod]
        public void TestGetStorageUnknownContract()
        {
            var snapshot = _neoSystem.GetSnapshotCache();
            var randomHash = TestUtils.RandomUInt160();
            var key = new byte[] { 0x01 };
            try
            {
                _rpcServer.GetStorage(new ContractNameOrHashOrId(randomHash), Convert.ToBase64String(key));
                Assert.Fail("Expected RpcException was not thrown.");
            }
            catch (RpcException ex)
            {
                Assert.AreEqual(RpcError.UnknownContract.Code, ex.HResult);
            }
        }

        [TestMethod]
        public void TestGetStorageUnknownStorageItem()
        {
            var snapshot = _neoSystem.GetSnapshotCache();
            var contractState = TestUtils.GetContract();
            snapshot.AddContract(contractState.Hash, contractState);
            snapshot.Commit();

            var key = new byte[] { 0x01 };
            try
            {
                _rpcServer.GetStorage(new ContractNameOrHashOrId(contractState.Hash), Convert.ToBase64String(key));
                Assert.Fail("Expected RpcException was not thrown.");
            }
            catch (RpcException ex)
            {
                Assert.AreEqual(RpcError.UnknownStorageItem.Code, ex.HResult);
            }
        }

        [TestMethod]
        public void TestGetTransactionHeightUnknownTransaction()
        {
            var randomHash = TestUtils.RandomUInt256();
            try
            {
                _rpcServer.GetTransactionHeight(randomHash);
                Assert.Fail("Expected RpcException was not thrown.");
            }
            catch (RpcException ex)
            {
                Assert.AreEqual(RpcError.UnknownTransaction.Code, ex.HResult);
            }

            var ex2 = Assert.ThrowsExactly<RpcException>(() => _ = _rpcServer.GetTransactionHeight(null));
            Assert.AreEqual(RpcError.InvalidParams.Code, ex2.HResult);
        }

        [TestMethod]
        public void TestGetRawTransactionUnknownTransaction()
        {
            var randomHash = TestUtils.RandomUInt256();
            try
            {
                _rpcServer.GetRawTransaction(randomHash, true);
                Assert.Fail("Expected RpcException was not thrown.");
            }
            catch (RpcException ex)
            {
                Assert.AreEqual(RpcError.UnknownTransaction.Code, ex.HResult);
            }
        }

        [TestMethod]
        public void TestInternalServerError()
        {
            _memoryStore.Reset();
            try
            {
                _rpcServer.GetCandidates();
                Assert.Fail("Expected RpcException was not thrown.");
            }
            catch (RpcException ex)
            {
                Assert.AreEqual(RpcError.InternalServerError.Code, ex.HResult);
            }
        }

        [TestMethod]
        public void TestUnknownHeight()
        {
            try
            {
                _rpcServer.GetBlockHash(int.MaxValue);
                Assert.Fail("Expected RpcException was not thrown.");
            }
            catch (RpcException ex)
            {
                Assert.AreEqual(RpcError.UnknownHeight.Code, ex.HResult);
            }
        }
    }
}
