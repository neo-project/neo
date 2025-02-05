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
            var b = snapshot.GetAndChange(key, () => new StorageItem(new HashIndexState())).GetInteroperable<HashIndexState>();
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
                Assert.AreEqual(VerifyResult.Succeed, tx.VerifyStateIndependent(UnitTests.TestProtocolSettings.Default));
            });

            result = _rpcServer.GetBlock(new BlockHashOrIndex(block.Hash), true);
            var block3 = block.ToJson(UnitTests.TestProtocolSettings.Default);
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
                Assert.AreEqual(VerifyResult.Succeed, tx.VerifyStateIndependent(UnitTests.TestProtocolSettings.Default));
            });

            result = _rpcServer.GetBlock(new BlockHashOrIndex(block.Index), true);
            var block3 = block.ToJson(UnitTests.TestProtocolSettings.Default);
            block3["confirmations"] = NativeContract.Ledger.CurrentIndex(snapshot) - block.Index + 1;
            Assert.AreEqual(block3.ToString(), result.ToString());
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
            var reason = _neoSystem.Blockchain.Ask<Blockchain.RelayResult>(block).Result;
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
            var ex1 = Assert.ThrowsException<RpcException>(() =>
                _rpcServer.GetContractState(new ContractNameOrHashOrId(contractState.Hash)));
            Assert.AreEqual(RpcError.UnknownContract.Message, ex1.Message);

            var ex2 = Assert.ThrowsException<RpcException>(() =>
                _rpcServer.GetContractState(new ContractNameOrHashOrId(contractState.Id)));
            Assert.AreEqual(RpcError.UnknownContract.Message, ex2.Message);
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

            var result = _rpcServer.GetStorage(new ContractNameOrHashOrId(contractState.Hash), Convert.ToBase64String(key));
            Assert.AreEqual(Convert.ToBase64String(value), result.AsString());
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
            var result = _rpcServer.FindStorage(new ContractNameOrHashOrId(contractState.Hash), Convert.ToBase64String(key), 0);

            var json = new JObject();
            var jarr = new JArray();
            var j = new JObject();
            j["key"] = Convert.ToBase64String(key);
            j["value"] = Convert.ToBase64String(value);
            jarr.Add(j);
            json["truncated"] = false;
            json["next"] = 1;
            json["results"] = jarr;
            Assert.AreEqual(json.ToString(), result.ToString());

            var result2 = _rpcServer.FindStorage(new ContractNameOrHashOrId(contractState.Hash), Convert.ToBase64String(key));
            Assert.AreEqual(result.ToString(), result2.ToString());

            Enumerable.Range(0, 51).ToList().ForEach(i => TestUtils.StorageItemAdd(snapshot, contractState.Id, new byte[] { 0x01, (byte)i }, new byte[] { 0x02 }));
            snapshot.Commit();
            var result4 = _rpcServer.FindStorage(new ContractNameOrHashOrId(contractState.Hash), Convert.ToBase64String(new byte[] { 0x01 }), 0);
            Assert.AreEqual(RpcServerSettings.Default.FindStoragePageSize, result4["next"].AsNumber());
            Assert.IsTrue(result4["truncated"].AsBoolean());
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
        public void TestGetNextBlockValidators()
        {
            var snapshot = _neoSystem.GetSnapshotCache();
            var result = _rpcServer.GetNextBlockValidators();

            var validators = NativeContract.NEO.GetNextBlockValidators(snapshot, _neoSystem.Settings.ValidatorsCount);
            var expected = validators.Select(p =>
            {
                var validator = new JObject();
                validator["publickey"] = p.ToString();
                validator["votes"] = (int)NativeContract.NEO.GetCandidateVote(snapshot, p);
                return validator;
            }).ToArray();
            Assert.AreEqual(new JArray(expected).ToString(), result.ToString());
        }

        [TestMethod]
        public void TestGetCandidates()
        {
            var snapshot = _neoSystem.GetSnapshotCache();

            var result = _rpcServer.GetCandidates();
            var json = new JArray();
            var validators = NativeContract.NEO.GetNextBlockValidators(snapshot, _neoSystem.Settings.ValidatorsCount);

            var key = new KeyBuilder(NativeContract.NEO.Id, 33).Add(ECPoint.Parse("02237309a0633ff930d51856db01d17c829a5b2e5cc2638e9c03b4cfa8e9c9f971", ECCurve.Secp256r1));
            snapshot.Add(key, new StorageItem(new CandidateState() { Registered = true, Votes = 10000 }));
            snapshot.Commit();
            var candidates = NativeContract.NEO.GetCandidates(_neoSystem.GetSnapshotCache());
            result = _rpcServer.GetCandidates();
            foreach (var candidate in candidates)
            {
                var item = new JObject();
                item["publickey"] = candidate.PublicKey.ToString();
                item["votes"] = candidate.Votes.ToString();
                item["active"] = validators.Contains(candidate.PublicKey);
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
            var result = _rpcServer.GetNativeContracts();
            var contracts = new JArray(NativeContract.Contracts.Select(p => NativeContract.ContractManagement.GetContract(_neoSystem.GetSnapshotCache(), p.Hash).ToJson()));
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
            Assert.ThrowsException<RpcException>(() => _rpcServer.GetBlockHash(block.Index + 1));
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
