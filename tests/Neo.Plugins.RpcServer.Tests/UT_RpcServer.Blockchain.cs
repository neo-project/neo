// Copyright (C) 2015-2024 The Neo Project.
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
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO;
using Neo.Json;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.UnitTests;
using Neo.UnitTests.Extensions;
using System;
using System.Linq;

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

            var result = _rpcServer.GetBestBlockHash([]);
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

            var parameters = new JArray(block.Hash.ToString(), false);
            var result = _rpcServer.GetBlock(parameters);
            var blockArr = Convert.FromBase64String(result.AsString());
            var block2 = blockArr.AsSerializable<Block>();
            block2.Transactions.ForEach(tx =>
            {
                Assert.AreEqual(VerifyResult.Succeed, tx.VerifyStateIndependent(UnitTests.TestProtocolSettings.Default));
            });

            parameters = new JArray(block.Hash.ToString(), true);
            result = _rpcServer.GetBlock(parameters);
            var block3 = block.ToJson(UnitTests.TestProtocolSettings.Default);
            block3["confirmations"] = NativeContract.Ledger.CurrentIndex(snapshot) - block.Index + 1;
            result.ToString().Should().Be(block3.ToString());
        }

        [TestMethod]
        public void TestGetBlockByIndex()
        {
            var snapshot = _neoSystem.GetSnapshotCache();
            var block = TestUtils.CreateBlockWithValidTransactions(snapshot, _wallet, _walletAccount, 3);
            TestUtils.BlocksAdd(snapshot, block.Hash, block);
            snapshot.Commit();

            var parameters = new JArray(block.Index, false);
            var result = _rpcServer.GetBlock(parameters);
            var blockArr = Convert.FromBase64String(result.AsString());
            var block2 = blockArr.AsSerializable<Block>();
            block2.Transactions.ForEach(tx =>
            {
                Assert.AreEqual(VerifyResult.Succeed, tx.VerifyStateIndependent(UnitTests.TestProtocolSettings.Default));
            });

            parameters = new JArray(block.Index, true);
            result = _rpcServer.GetBlock(parameters);
            var block3 = block.ToJson(UnitTests.TestProtocolSettings.Default);
            block3["confirmations"] = NativeContract.Ledger.CurrentIndex(snapshot) - block.Index + 1;
            result.ToString().Should().Be(block3.ToString());
        }

        [TestMethod]
        public void TestGetBlockCount()
        {
            var expectedCount = 1;
            var result = _rpcServer.GetBlockCount(new JArray());
            Assert.AreEqual(expectedCount, result.AsNumber());
        }

        [TestMethod]
        public void TestGetBlockHeaderCount()
        {
            var expectedCount = 1;
            var result = _rpcServer.GetBlockHeaderCount(new JArray());
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
            var result = _rpcServer.GetBlockHash(new JArray(block.Index));
            Assert.AreEqual(expectedHash, result.AsString());
        }

        [TestMethod]
        public void TestGetBlockHeader()
        {
            var snapshot = _neoSystem.GetSnapshotCache();
            var block = TestUtils.CreateBlockWithValidTransactions(snapshot, _wallet, _walletAccount, 3);
            TestUtils.BlocksAdd(snapshot, block.Hash, block);
            snapshot.Commit();
            var parameters = new JArray(block.Hash.ToString(), true);
            var result = _rpcServer.GetBlockHeader(parameters);
            var header = block.Header.ToJson(_neoSystem.Settings);
            header["confirmations"] = NativeContract.Ledger.CurrentIndex(snapshot) - block.Index + 1;
            Assert.AreEqual(header.ToString(), result.ToString());

            parameters = new JArray(block.Index, false);
            result = _rpcServer.GetBlockHeader(parameters);
            var headerArr = Convert.FromBase64String(result.AsString());
            var header2 = headerArr.AsSerializable<Header>();
            header2.ToJson(_neoSystem.Settings).ToString().Should().Be(block.Header.ToJson(_neoSystem.Settings).ToString());
        }

        [TestMethod]
        public void TestGetContractState()
        {
            var snapshot = _neoSystem.GetSnapshotCache();
            var contractState = TestUtils.GetContract();
            snapshot.AddContract(contractState.Hash, contractState);
            snapshot.Commit();
            var result = _rpcServer.GetContractState(new JArray(contractState.Hash.ToString()));
            Assert.AreEqual(contractState.ToJson().ToString(), result.ToString());

            //Test Faild
            //result = _rpcServer.GetContractState(new JArray(contractState.Id));
            //Assert.AreEqual(contractState.ToJson().ToString(), result.ToString());
        }

        [TestMethod]
        public void TestGetRawMemPool()
        {
            var snapshot = _neoSystem.GetSnapshotCache();
            var tx = TestUtils.CreateValidTx(snapshot, _wallet, _walletAccount);
            snapshot.Commit();
            _neoSystem.MemPool.TryAdd(tx, snapshot);

            var result = _rpcServer.GetRawMemPool(new JArray());
            Assert.IsTrue(((JArray)result).Any(p => p.AsString() == tx.Hash.ToString()));

            result = _rpcServer.GetRawMemPool(new JArray("true"));
            Assert.IsTrue(((JArray)result["verified"]).Any(p => p.AsString() == tx.Hash.ToString()));
        }

        [TestMethod]
        public void TestGetRawTransaction()
        {
            var snapshot = _neoSystem.GetSnapshotCache();
            var tx = TestUtils.CreateValidTx(snapshot, _wallet, _walletAccount);
            _neoSystem.MemPool.TryAdd(tx, snapshot);
            var parameters = new JArray(tx.Hash.ToString(), true);
            snapshot.Commit();
            var result = _rpcServer.GetRawTransaction(parameters);

            var json = Utility.TransactionToJson(tx, _neoSystem.Settings);
            Assert.AreEqual(json.ToString(), result.ToString());
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

            var result = _rpcServer.GetStorage(new JArray(contractState.Hash.ToString(), Convert.ToBase64String(key)));
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
            var result = _rpcServer.FindStorage(new JArray(contractState.Hash.ToString(), Convert.ToBase64String(key), 0));

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
        }

        [TestMethod]
        public void TestGetTransactionHeight()
        {
            var snapshot = _neoSystem.GetSnapshotCache();
            var block = TestUtils.CreateBlockWithValidTransactions(snapshot, _wallet, _walletAccount, 1);
            TestUtils.BlocksAdd(snapshot, block.Hash, block);
            snapshot.Commit();
            var tx = block.Transactions[0];
            var result = _rpcServer.GetTransactionHeight(new JArray(tx.Hash.ToString()));
            Assert.AreEqual(block.Index, result.AsNumber());
        }

        [TestMethod]
        public void TestGetNextBlockValidators()
        {
            var snapshot = _neoSystem.GetSnapshotCache();
            var result = _rpcServer.GetNextBlockValidators(new JArray());

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
            var result = _rpcServer.GetCandidates(new JArray());
            var json = new JArray();
            var validators = NativeContract.NEO.GetNextBlockValidators(snapshot, _neoSystem.Settings.ValidatorsCount);
            snapshot.Commit();
            var candidates = NativeContract.NEO.GetCandidates(_neoSystem.GetSnapshotCache());

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
            var result = _rpcServer.GetCommittee(new JArray());
            var committee = NativeContract.NEO.GetCommittee(snapshot);
            var expected = new JArray(committee.Select(p => (JToken)p.ToString()));
            Assert.AreEqual(expected.ToString(), result.ToString());
        }

        [TestMethod]
        public void TestGetNativeContracts()
        {
            var result = _rpcServer.GetNativeContracts(new JArray());
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

            var parameters = new JArray(int.MaxValue, false);
            try
            {
                _rpcServer.GetBlock(parameters);
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

            var parameters = new JArray(TestUtils.RandomUInt256().ToString(), false);
            try
            {
                _rpcServer.GetBlock(parameters);
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

            var parameters = new JArray(int.MaxValue, false);
            try
            {
                _rpcServer.GetBlock(parameters);
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

            var parameters = new JArray(TestUtils.RandomUInt256().ToString(), false);
            try
            {
                _rpcServer.GetBlock(parameters);
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
            Assert.ThrowsException<RpcException>(() => _rpcServer.GetBlockHash(new JArray(block.Index + 1)));
        }

        [TestMethod]
        public void TestGetContractStateUnknownContract()
        {
            var snapshot = _neoSystem.GetSnapshotCache();
            var randomHash = TestUtils.RandomUInt160();
            try
            {
                _rpcServer.GetContractState(new JArray(randomHash.ToString()));
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
                _rpcServer.GetStorage(new JArray(randomHash.ToString(), Convert.ToBase64String(key)));
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
                _rpcServer.GetStorage(new JArray(contractState.Hash.ToString(), Convert.ToBase64String(key)));
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
                _rpcServer.GetTransactionHeight(new JArray(randomHash.ToString()));
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
                _rpcServer.GetRawTransaction(new JArray(randomHash.ToString(), true));
                Assert.Fail("Expected RpcException was not thrown.");
            }
            catch (RpcException ex)
            {
                Assert.AreEqual(RpcError.UnknownTransaction.Code, ex.HResult);
            }
        }

        [TestMethod]
        public void TestGetBlockInvalidParams()
        {
            try
            {
                _rpcServer.GetBlock(new JArray("invalid_hash", false));
                Assert.Fail("Expected RpcException was not thrown.");
            }
            catch (RpcException ex)
            {
                Assert.AreEqual(RpcError.InvalidParams.Code, ex.HResult);
            }
            catch (FormatException)
            {
            }
            catch
            {
                Assert.Fail("Unexpected exception");
            }
        }

        [TestMethod]
        public void TestGetBlockHashInvalidParams()
        {
            try
            {
                _rpcServer.GetBlockHash(new JArray("invalid_index"));
                Assert.Fail("Expected RpcException was not thrown.");
            }
            catch (RpcException ex)
            {
                Assert.AreEqual(RpcError.InvalidParams.Code, ex.HResult);
            }
        }

        [TestMethod]
        public void TestGetBlockHeaderInvalidParams()
        {
            try
            {
                _rpcServer.GetBlockHeader(new JArray("invalid_hash", true));
                Assert.Fail("Expected RpcException was not thrown.");
            }
            catch (RpcException ex)
            {
                Assert.AreEqual(RpcError.InvalidParams.Code, ex.HResult);
            }
            catch (FormatException)
            {
            }
            catch
            {
                Assert.Fail("Unexpected exception");
            }
        }

        [TestMethod]
        public void TestGetContractStateInvalidParams()
        {
            try
            {
                _rpcServer.GetContractState(new JArray("invalid_hash"));
                Assert.Fail("Expected RpcException was not thrown.");
            }
            catch (RpcException ex)
            {
                Assert.AreEqual(RpcError.InvalidParams.Code, ex.HResult);
            }
            catch (FormatException)
            {
            }
            catch
            {
                Assert.Fail("Unexpected exception");
            }
        }

        [TestMethod]
        public void TestGetStorageInvalidParams()
        {
            try
            {
                _rpcServer.GetStorage(new JArray("invalid_hash", "invalid_key"));
                Assert.Fail("Expected RpcException was not thrown.");
            }
            catch (RpcException ex)
            {
                Assert.AreEqual(RpcError.InvalidParams.Code, ex.HResult);
            }
            catch (FormatException)
            {
            }
            catch
            {
                Assert.Fail("Unexpected exception");
            }
        }

        [TestMethod]
        public void TestFindStorageInvalidParams()
        {
            try
            {
                _rpcServer.FindStorage(new JArray("invalid_hash", "invalid_prefix", "invalid_start"));
                Assert.Fail("Expected RpcException was not thrown.");
            }
            catch (RpcException ex)
            {
                Assert.AreEqual(RpcError.InvalidParams.Code, ex.HResult);
            }
            catch (FormatException)
            {
            }
            catch
            {
                Assert.Fail("Unexpected exception");
            }
        }

        [TestMethod]
        public void TestGetTransactionHeightInvalidParams()
        {
            try
            {
                _rpcServer.GetTransactionHeight(new JArray("invalid_hash"));
                Assert.Fail("Expected RpcException was not thrown.");
            }
            catch (RpcException ex)
            {
                Assert.AreEqual(RpcError.InvalidParams.Code, ex.HResult);
            }
        }

        [TestMethod]
        public void TestGetRawTransactionInvalidParams()
        {
            try
            {
                _rpcServer.GetRawTransaction(new JArray("invalid_hash", true));
                Assert.Fail("Expected RpcException was not thrown.");
            }
            catch (RpcException ex)
            {
                Assert.AreEqual(RpcError.InvalidParams.Code, ex.HResult);
            }
        }

        [TestMethod]
        public void TestInternalServerError()
        {
            _memoryStore.Reset();
            try
            {
                _rpcServer.GetCandidates(new JArray());
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
                _rpcServer.GetBlockHash(new JArray(int.MaxValue));
                Assert.Fail("Expected RpcException was not thrown.");
            }
            catch (RpcException ex)
            {
                Assert.AreEqual(RpcError.UnknownHeight.Code, ex.HResult);
            }
        }
    }
}
