// Copyright (C) 2015-2024 The Neo Project.
//
// UT_RpcServer.Node.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO;
using Neo.Json;
using Neo.Network.P2P.Payloads;
using Neo.SmartContract.Native;
using Neo.UnitTests;
using System;

namespace Neo.Plugins.RpcServer.Tests
{
    partial class UT_RpcServer
    {
        [TestMethod]
        public void TestGetVersion()
        {
            var result = _rpcServer.GetVersion(new JArray());
            Assert.IsInstanceOfType(result, typeof(JObject));

            var json = (JObject)result;
            Assert.IsTrue(json.ContainsProperty("tcpport"));
            Assert.IsTrue(json.ContainsProperty("nonce"));
            Assert.IsTrue(json.ContainsProperty("useragent"));

            Assert.IsTrue(json.ContainsProperty("protocol"));
            var protocol = (JObject)json["protocol"];
            Assert.IsTrue(protocol.ContainsProperty("addressversion"));
            Assert.IsTrue(protocol.ContainsProperty("network"));
            Assert.IsTrue(protocol.ContainsProperty("validatorscount"));
            Assert.IsTrue(protocol.ContainsProperty("msperblock"));
            Assert.IsTrue(protocol.ContainsProperty("maxtraceableblocks"));
            Assert.IsTrue(protocol.ContainsProperty("maxvaliduntilblockincrement"));
            Assert.IsTrue(protocol.ContainsProperty("maxtransactionsperblock"));
            Assert.IsTrue(protocol.ContainsProperty("memorypoolmaxtransactions"));
        }

        #region SendRawTransaction Tests

        [TestMethod]
        public void TestSendRawTransaction_Normal()
        {
            var snapshot = _neoSystem.GetSnapshot();
            var tx = TestUtils.CreateValidTx(snapshot, _wallet, _walletAccount);
            var txString = Convert.ToBase64String(tx.ToArray());

            var result = _rpcServer.SendRawTransaction(new JArray(txString));
            Assert.IsInstanceOfType(result, typeof(JObject));
            Assert.IsTrue(((JObject)result).ContainsProperty("hash"));
        }

        [TestMethod]
        public void TestSendRawTransaction_InvalidTransactionFormat()
        {
            Assert.ThrowsException<RpcException>(() =>
                _rpcServer.SendRawTransaction(new JArray("invalid_transaction_string")),
                "Should throw RpcException for invalid transaction format");
        }

        [TestMethod]
        public void TestSendRawTransaction_InsufficientBalance()
        {
            var snapshot = _neoSystem.GetSnapshot();
            var tx = TestUtils.CreateInvalidTransaction(snapshot, _wallet, _walletAccount, TestUtils.InvalidTransactionType.InsufficientBalance);
            var txString = Convert.ToBase64String(tx.ToArray());

            var exception = Assert.ThrowsException<RpcException>(() =>
                _rpcServer.SendRawTransaction(new JArray(txString)),
                "Should throw RpcException for insufficient balance");
            Assert.AreEqual(RpcError.InsufficientFunds.Code, exception.HResult);
        }

        [TestMethod]
        public void TestSendRawTransaction_InvalidSignature()
        {
            var snapshot = _neoSystem.GetSnapshot();
            var tx = TestUtils.CreateInvalidTransaction(snapshot, _wallet, _walletAccount, TestUtils.InvalidTransactionType.InvalidSignature);
            var txString = Convert.ToBase64String(tx.ToArray());

            var exception = Assert.ThrowsException<RpcException>(() =>
                _rpcServer.SendRawTransaction(new JArray(txString)),
                "Should throw RpcException for invalid signature");
            Assert.AreEqual(RpcError.InvalidSignature.Code, exception.HResult);
        }

        [TestMethod]
        public void TestSendRawTransaction_InvalidScript()
        {
            var snapshot = _neoSystem.GetSnapshot();
            var tx = TestUtils.CreateInvalidTransaction(snapshot, _wallet, _walletAccount, TestUtils.InvalidTransactionType.InvalidScript);
            var txString = Convert.ToBase64String(tx.ToArray());

            var exception = Assert.ThrowsException<RpcException>(() =>
                _rpcServer.SendRawTransaction(new JArray(txString)),
                "Should throw RpcException for invalid script");
            Assert.AreEqual(RpcError.InvalidScript.Code, exception.HResult);
        }

        [TestMethod]
        public void TestSendRawTransaction_InvalidAttribute()
        {
            var snapshot = _neoSystem.GetSnapshot();
            var tx = TestUtils.CreateInvalidTransaction(snapshot, _wallet, _walletAccount, TestUtils.InvalidTransactionType.InvalidAttribute);
            var txString = Convert.ToBase64String(tx.ToArray());

            var exception = Assert.ThrowsException<RpcException>(() =>
                _rpcServer.SendRawTransaction(new JArray(txString)),
                "Should throw RpcException for invalid attribute");
            // Transaction with invalid attribute can not pass the Transaction deserialization
            // and will throw invalid params exception.
            Assert.AreEqual(RpcError.InvalidParams.Code, exception.HResult);
        }

        [TestMethod]
        public void TestSendRawTransaction_Oversized()
        {
            var snapshot = _neoSystem.GetSnapshot();
            var tx = TestUtils.CreateInvalidTransaction(snapshot, _wallet, _walletAccount, TestUtils.InvalidTransactionType.Oversized);
            var txString = Convert.ToBase64String(tx.ToArray());

            var exception = Assert.ThrowsException<RpcException>(() =>
                _rpcServer.SendRawTransaction(new JArray(txString)),
                "Should throw RpcException for invalid format transaction");
            // Oversized transaction will not pass the deserialization.
            Assert.AreEqual(RpcError.InvalidParams.Code, exception.HResult);
        }

        [TestMethod]
        public void TestSendRawTransaction_Expired()
        {
            var snapshot = _neoSystem.GetSnapshot();
            var tx = TestUtils.CreateInvalidTransaction(snapshot, _wallet, _walletAccount, TestUtils.InvalidTransactionType.Expired);
            var txString = Convert.ToBase64String(tx.ToArray());

            var exception = Assert.ThrowsException<RpcException>(() =>
                _rpcServer.SendRawTransaction(new JArray(txString)),
                "Should throw RpcException for expired transaction");
            Assert.AreEqual(RpcError.ExpiredTransaction.Code, exception.HResult);
        }

        [TestMethod]
        public void TestSendRawTransaction_PolicyFailed()
        {
            var snapshot = _neoSystem.GetSnapshot();
            var tx = TestUtils.CreateValidTx(snapshot, _wallet, _walletAccount);
            var txString = Convert.ToBase64String(tx.ToArray());
            NativeContract.Policy.BlockAccount(snapshot, _walletAccount.ScriptHash);
            snapshot.Commit();

            var exception = Assert.ThrowsException<RpcException>(() =>
                _rpcServer.SendRawTransaction(new JArray(txString)),
                "Should throw RpcException for conflicting transaction");
            Assert.AreEqual(RpcError.PolicyFailed.Code, exception.HResult);
        }

        [TestMethod]
        public void TestSendRawTransaction_AlreadyInPool()
        {
            var snapshot = _neoSystem.GetSnapshot();
            var tx = TestUtils.CreateValidTx(snapshot, _wallet, _walletAccount);
            _neoSystem.MemPool.TryAdd(tx, snapshot);
            var txString = Convert.ToBase64String(tx.ToArray());

            var exception = Assert.ThrowsException<RpcException>(() =>
                _rpcServer.SendRawTransaction(new JArray(txString)),
                "Should throw RpcException for transaction already in memory pool");
            Assert.AreEqual(RpcError.AlreadyInPool.Code, exception.HResult);
        }

        [TestMethod]
        public void TestSendRawTransaction_AlreadyInBlockchain()
        {
            var snapshot = _neoSystem.GetSnapshot();
            var tx = TestUtils.CreateValidTx(snapshot, _wallet, _walletAccount);
            TestUtils.AddTransactionToBlockchain(snapshot, tx);
            snapshot.Commit();
            var txString = Convert.ToBase64String(tx.ToArray());
            var exception = Assert.ThrowsException<RpcException>(() => _rpcServer.SendRawTransaction(new JArray(txString)));
            Assert.AreEqual(RpcError.AlreadyExists.Code, exception.HResult);
        }

        #endregion

        #region SubmitBlock Tests

        [TestMethod]
        public void TestSubmitBlock_Normal()
        {
            var snapshot = _neoSystem.GetSnapshot();
            var block = TestUtils.CreateBlockWithValidTransactions(snapshot, _wallet, _walletAccount, 1);
            var blockString = Convert.ToBase64String(block.ToArray());

            var result = _rpcServer.SubmitBlock(new JArray(blockString));
            Assert.IsInstanceOfType(result, typeof(JObject));
            Assert.IsTrue(((JObject)result).ContainsProperty("hash"));
        }

        [TestMethod]
        public void TestSubmitBlock_InvalidBlockFormat()
        {
            string invalidBlockString = TestUtils.CreateInvalidBlockFormat();

            var exception = Assert.ThrowsException<RpcException>(() =>
                _rpcServer.SubmitBlock(new JArray(invalidBlockString)),
                "Should throw RpcException for invalid block format");

            Assert.AreEqual(RpcError.InvalidParams.Code, exception.HResult);
            StringAssert.Contains(exception.Message, "Invalid Block Format");
        }

        [TestMethod]
        public void TestSubmitBlock_AlreadyExists()
        {
            var snapshot = _neoSystem.GetSnapshot();
            var block = TestUtils.CreateBlockWithValidTransactions(snapshot, _wallet, _walletAccount, 1);
            TestUtils.BlocksAdd(snapshot, block.Hash, block);
            snapshot.Commit();
            var blockString = Convert.ToBase64String(block.ToArray());

            var exception = Assert.ThrowsException<RpcException>(() =>
                _rpcServer.SubmitBlock(new JArray(blockString)),
                "Should throw RpcException when block already exists");
            Assert.AreEqual(RpcError.AlreadyExists.Code, exception.HResult);
        }

        [TestMethod]
        public void TestSubmitBlock_InvalidBlock()
        {
            var snapshot = _neoSystem.GetSnapshot();
            var block = TestUtils.CreateBlockWithValidTransactions(snapshot, _wallet, _walletAccount, 1);
            block.Header.Witness = new Witness();
            var blockString = Convert.ToBase64String(block.ToArray());

            var exception = Assert.ThrowsException<RpcException>(() =>
                _rpcServer.SubmitBlock(new JArray(blockString)),
                "Should throw RpcException for invalid block");
            Assert.AreEqual(RpcError.VerificationFailed.Code, exception.HResult);
        }

        #endregion

        #region Edge Cases and Error Handling

        [TestMethod]
        public void TestSendRawTransaction_NullInput()
        {
            var exception = Assert.ThrowsException<RpcException>(() =>
                _rpcServer.SendRawTransaction(new JArray((string)null)),
                "Should throw RpcException for null input");
            Assert.AreEqual(RpcError.InvalidParams.Code, exception.HResult);
        }

        [TestMethod]
        public void TestSendRawTransaction_EmptyInput()
        {
            var exception = Assert.ThrowsException<RpcException>(() =>
                _rpcServer.SendRawTransaction(new JArray(string.Empty)),
                "Should throw RpcException for empty input");
            Assert.AreEqual(RpcError.InvalidParams.Code, exception.HResult);
        }

        [TestMethod]
        public void TestSubmitBlock_NullInput()
        {
            var exception = Assert.ThrowsException<RpcException>(() =>
                _rpcServer.SubmitBlock(new JArray((string)null)),
                "Should throw RpcException for null input");
            Assert.AreEqual(RpcError.InvalidParams.Code, exception.HResult);
        }

        [TestMethod]
        public void TestSubmitBlock_EmptyInput()
        {
            var exception = Assert.ThrowsException<RpcException>(() =>
                _rpcServer.SubmitBlock(new JArray(string.Empty)),
                "Should throw RpcException for empty input");
            Assert.AreEqual(RpcError.InvalidParams.Code, exception.HResult);
        }

        #endregion
    }
}
