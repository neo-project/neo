// Copyright (C) 2015-2025 The Neo Project.
//
// UT_Block.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Extensions;
using Neo.IO;
using Neo.Json;
using Neo.Network.P2P.Payloads;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using System;

namespace Neo.UnitTests.Network.P2P.Payloads
{
    [TestClass]
    public class UT_Block
    {
        Block uut;
        private static ApplicationEngine GetEngine(bool hasContainer = false, bool hasSnapshot = false, bool hasBlock = false, bool addScript = true, long gas = 20_00000000)
        {
            var tx = hasContainer ? TestUtils.GetTransaction(UInt160.Zero) : null;
            var snapshotCache = hasSnapshot ? TestBlockchain.GetTestSnapshotCache() : null;
            var block = hasBlock ? new Block { Header = new Header() } : null;
            var engine = ApplicationEngine.Create(TriggerType.Application, tx, snapshotCache, block, TestBlockchain.TheNeoSystem.Settings, gas: gas);
            if (addScript) engine.LoadScript(new byte[] { 0x01 });
            return engine;
        }

        [TestInitialize]
        public void TestSetup()
        {
            uut = new Block();
        }

        [TestMethod]
        public void Transactions_Get()
        {
            Assert.IsNull(uut.Transactions);
        }

        [TestMethod]
        public void Header_Get()
        {
            UInt256 val256 = UInt256.Zero;
            TestUtils.SetupBlockWithValues(null, uut, val256, out var merkRootVal, out _, out var timestampVal, out var nonceVal, out var indexVal, out var scriptVal, out _, 0);

            Assert.IsNotNull(uut.Header);
            Assert.AreEqual(val256, uut.Header.PrevHash);
            Assert.AreEqual(merkRootVal, uut.Header.MerkleRoot);
            Assert.AreEqual(timestampVal, uut.Header.Timestamp);
            Assert.AreEqual(indexVal, uut.Header.Index);
            Assert.AreEqual(nonceVal, uut.Header.Nonce);
            Assert.AreEqual(scriptVal, uut.Header.Witness);
        }

        [TestMethod]
        public void Size_Get()
        {
            UInt256 val256 = UInt256.Zero;
            TestUtils.SetupBlockWithValues(null, uut, val256, out var _, out var _, out var _, out var _, out var _, out var _, out var _, 0);
            // header 4 + 32 + 32 + 8 + 4 + 1 + 20 + 4
            // tx 1
            Assert.AreEqual(114, uut.Size); // 106 + nonce
        }

        [TestMethod]
        public void Size_Get_1_Transaction()
        {
            UInt256 val256 = UInt256.Zero;
            TestUtils.SetupBlockWithValues(null, uut, val256, out var _, out var _, out var _, out var _, out var _, out var _, out var _, 0);

            uut.Transactions = new[]
            {
                TestUtils.GetTransaction(UInt160.Zero)
            };

            Assert.AreEqual(167, uut.Size); // 159 + nonce
        }

        [TestMethod]
        public void Size_Get_3_Transaction()
        {
            UInt256 val256 = UInt256.Zero;
            TestUtils.SetupBlockWithValues(null, uut, val256, out var _, out var _, out var _, out var _, out var _, out var _, out var _, 0);

            uut.Transactions = new[]
            {
                TestUtils.GetTransaction(UInt160.Zero),
                TestUtils.GetTransaction(UInt160.Zero),
                TestUtils.GetTransaction(UInt160.Zero)
            };

            Assert.AreEqual(273, uut.Size); // 265 + nonce
        }

        [TestMethod]
        public void Serialize()
        {
            UInt256 val256 = UInt256.Zero;
            TestUtils.SetupBlockWithValues(null, uut, val256, out var _, out var _, out var _, out var _, out var _, out var _, out var _, 1);

            var hex = "0000000000000000000000000000000000000000000000000000000000000000000000006c23be5d32679baa9c5c2aa0d329fd2a2441d7875d0f34d42f58f70428fbbbb9493ed0e58f01000000000000000000000000000000000000000000000000000000000000000000000001000111010000000000000000000000000000000000000000000000000001000000000000000000000000000000000000000001000112010000";
            Assert.AreEqual(hex, uut.ToArray().ToHexString());
        }

        [TestMethod]
        public void Deserialize()
        {
            UInt256 val256 = UInt256.Zero;
            TestUtils.SetupBlockWithValues(null, new Block(), val256, out _, out var val160, out var timestampVal, out var indexVal, out var nonceVal, out var scriptVal, out var transactionsVal, 1);

            var hex = "0000000000000000000000000000000000000000000000000000000000000000000000006c23be5d32679baa9c5c2aa0d329fd2a2441d7875d0f34d42f58f70428fbbbb9493ed0e58f01000000000000000000000000000000000000000000000000000000000000000000000001000111010000000000000000000000000000000000000000000000000001000000000000000000000000000000000000000001000112010000";

            MemoryReader reader = new(hex.HexToBytes());
            uut.Deserialize(ref reader);
            UInt256 merkRoot = uut.MerkleRoot;

            AssertStandardBlockTestVals(val256, merkRoot, val160, timestampVal, indexVal, nonceVal, scriptVal, transactionsVal);
        }

        private void AssertStandardBlockTestVals(UInt256 val256, UInt256 merkRoot, UInt160 val160, ulong timestampVal, ulong nonceVal, uint indexVal, Witness scriptVal, Transaction[] transactionsVal, bool testTransactions = true)
        {
            Assert.AreEqual(val256, uut.PrevHash);
            Assert.AreEqual(merkRoot, uut.MerkleRoot);
            Assert.AreEqual(timestampVal, uut.Timestamp);
            Assert.AreEqual(indexVal, uut.Index);
            Assert.AreEqual(nonceVal, uut.Nonce);
            Assert.AreEqual(val160, uut.NextConsensus);
            Assert.AreEqual(0, uut.Witness.InvocationScript.Length);
            Assert.AreEqual(scriptVal.Size, uut.Witness.Size);
            Assert.AreEqual(scriptVal.VerificationScript.Span[0], uut.Witness.VerificationScript.Span[0]);
            if (testTransactions)
            {
                Assert.AreEqual(1, uut.Transactions.Length);
                Assert.AreEqual(transactionsVal[0], uut.Transactions[0]);
            }
        }

        [TestMethod]
        public void Equals_SameObj()
        {
            Assert.IsTrue(uut.Equals(uut));
            var obj = uut as object;
            Assert.IsTrue(uut.Equals(obj));
        }

        [TestMethod]
        public void TestGetHashCode()
        {
            var snapshot = GetEngine(true, true).SnapshotCache;
            Assert.AreEqual(-626492395, NativeContract.Ledger.GetBlock(snapshot, 0).GetHashCode());
        }

        [TestMethod]
        public void Equals_DiffObj()
        {
            Block newBlock = new();
            UInt256 val256 = UInt256.Zero;
            UInt256 prevHash = new(TestUtils.GetByteArray(32, 0x42));
            TestUtils.SetupBlockWithValues(null, newBlock, val256, out _, out _, out _, out ulong _, out uint _, out _, out _, 1);
            TestUtils.SetupBlockWithValues(null, uut, prevHash, out _, out _, out _, out _, out _, out _, out _, 0);

            Assert.IsFalse(uut.Equals(newBlock));
        }

        [TestMethod]
        public void Equals_Null()
        {
            Assert.IsFalse(uut.Equals(null));
        }

        [TestMethod]
        public void Equals_SameHash()
        {
            Block newBlock = new();
            UInt256 prevHash = new(TestUtils.GetByteArray(32, 0x42));
            TestUtils.SetupBlockWithValues(null, newBlock, prevHash, out _, out _, out _, out _, out _, out _, out _, 1);
            TestUtils.SetupBlockWithValues(null, uut, prevHash, out _, out _, out _, out _, out _, out _, out _, 1);

            Assert.IsTrue(uut.Equals(newBlock));
        }

        [TestMethod]
        public void ToJson()
        {
            UInt256 val256 = UInt256.Zero;
            TestUtils.SetupBlockWithValues(null, uut, val256, out _, out _, out var timeVal, out var indexVal, out var nonceVal, out _, out _, 1);

            JObject jObj = uut.ToJson(TestProtocolSettings.Default);
            Assert.IsNotNull(jObj);
            Assert.AreEqual("0x942065e93848732c2e7844061fa92d20c5d9dc0bc71d420a1ea71b3431fc21b4", jObj["hash"].AsString());
            Assert.AreEqual(167, jObj["size"].AsNumber()); // 159 + nonce
            Assert.AreEqual(0, jObj["version"].AsNumber());
            Assert.AreEqual("0x0000000000000000000000000000000000000000000000000000000000000000", jObj["previousblockhash"].AsString());
            Assert.AreEqual("0xb9bbfb2804f7582fd4340f5d87d741242afd29d3a02a5c9caa9b67325dbe236c", jObj["merkleroot"].AsString());
            Assert.AreEqual(timeVal, jObj["time"].AsNumber());
            Assert.AreEqual(nonceVal.ToString("X16"), jObj["nonce"].AsString());
            Assert.AreEqual(indexVal, jObj["index"].AsNumber());
            Assert.AreEqual("NKuyBkoGdZZSLyPbJEetheRhMjeznFZszf", jObj["nextconsensus"].AsString());

            JObject scObj = (JObject)jObj["witnesses"][0];
            Assert.AreEqual("", scObj["invocation"].AsString());
            Assert.AreEqual("EQ==", scObj["verification"].AsString());

            Assert.IsNotNull(jObj["tx"]);
            JObject txObj = (JObject)jObj["tx"][0];
            Assert.AreEqual("0xb9bbfb2804f7582fd4340f5d87d741242afd29d3a02a5c9caa9b67325dbe236c", txObj["hash"].AsString());
            Assert.AreEqual(53, txObj["size"].AsNumber());
            Assert.AreEqual(0, txObj["version"].AsNumber());
            Assert.AreEqual(0, ((JArray)txObj["attributes"]).Count);
            Assert.AreEqual("0", txObj["netfee"].AsString());
        }

        [TestMethod]
        public void Witness()
        {
            IVerifiable item = new Block { Header = new() };
            Assert.AreEqual(1, item.Witnesses.Length);

            Action action = () => item.Witnesses = null;
            Assert.ThrowsException<NotSupportedException>(action);
        }
    }
}
