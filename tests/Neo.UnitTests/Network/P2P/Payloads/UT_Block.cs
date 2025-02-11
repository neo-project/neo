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
        private static readonly string s_blockHex =
                "0000000000000000000000000000000000000000000000000000000000000000000000006c23be5d326" +
                "79baa9c5c2aa0d329fd2a2441d7875d0f34d42f58f70428fbbbb9493ed0e58f01000000000000000000" +
                "00000000000000000000000000000000000000000000000000000100011101000000000000000000000" +
                "0000000000000000000000000000001000000000000000000000000000000000000000001000112010000";

        private static ApplicationEngine GetEngine(bool hasContainer = false, bool hasSnapshot = false,
            bool hasBlock = false, bool addScript = true, long gas = 20_00000000)
        {
            var tx = hasContainer ? TestUtils.GetTransaction(UInt160.Zero) : null;
            var snapshotCache = hasSnapshot ? TestBlockchain.GetTestSnapshotCache() : null;
            var block = hasBlock ? new Block { Header = new Header() } : null;
            var engine = ApplicationEngine.Create(TriggerType.Application,
                tx, snapshotCache, block, TestBlockchain.TheNeoSystem.Settings, gas: gas);
            if (addScript) engine.LoadScript(new byte[] { 0x01 });
            return engine;
        }

        [TestMethod]
        public void Transactions_Get()
        {
            var uut = new Block();
            Assert.IsNull(uut.Transactions);
        }

        [TestMethod]
        public void Header_Get()
        {
            var uut = TestUtils.MakeBlock(null, UInt256.Zero, 0);
            Assert.IsNotNull(uut.Header);
            Assert.AreEqual(UInt256.Zero, uut.Header.PrevHash);
        }

        [TestMethod]
        public void Size_Get()
        {
            var uut = TestUtils.MakeBlock(null, UInt256.Zero, 0);
            // header 4 + 32 + 32 + 8 + 4 + 1 + 20 + 4
            // tx 1
            Assert.AreEqual(114, uut.Size); // 106 + nonce
        }

        [TestMethod]
        public void Size_Get_1_Transaction()
        {
            var uut = TestUtils.MakeBlock(null, UInt256.Zero, 1);
            uut.Transactions =
            [
                TestUtils.GetTransaction(UInt160.Zero)
            ];

            Assert.AreEqual(167, uut.Size); // 159 + nonce
        }

        [TestMethod]
        public void Size_Get_3_Transaction()
        {
            var uut = TestUtils.MakeBlock(null, UInt256.Zero, 3);
            uut.Transactions =
            [
                TestUtils.GetTransaction(UInt160.Zero),
                TestUtils.GetTransaction(UInt160.Zero),
                TestUtils.GetTransaction(UInt160.Zero)
            ];

            Assert.AreEqual(273, uut.Size); // 265 + nonce
        }

        [TestMethod]
        public void Serialize()
        {
            var uut = TestUtils.MakeBlock(null, UInt256.Zero, 1);
            Assert.AreEqual(s_blockHex, uut.ToArray().ToHexString());
        }

        [TestMethod]
        public void Deserialize()
        {
            var uut = TestUtils.MakeBlock(null, UInt256.Zero, 1);
            MemoryReader reader = new(s_blockHex.HexToBytes());
            uut.Deserialize(ref reader);
            var merkRoot = uut.MerkleRoot;

            Assert.AreEqual(merkRoot, uut.MerkleRoot);
        }

        [TestMethod]
        public void Equals_SameObj()
        {
            var uut = new Block();
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
            var prevHash = new UInt256(TestUtils.GetByteArray(32, 0x42));
            var block = TestUtils.MakeBlock(null, UInt256.Zero, 1);
            var uut = TestUtils.MakeBlock(null, prevHash, 0);

            Assert.IsFalse(uut.Equals(block));
        }

        [TestMethod]
        public void Equals_Null()
        {
            var uut = new Block();
            Assert.IsFalse(uut.Equals(null));
        }

        [TestMethod]
        public void Equals_SameHash()
        {
            var prevHash = new UInt256(TestUtils.GetByteArray(32, 0x42));
            var block = TestUtils.MakeBlock(null, prevHash, 1);
            var uut = TestUtils.MakeBlock(null, prevHash, 1);
            Assert.IsTrue(uut.Equals(block));
        }

        [TestMethod]
        public void ToJson()
        {
            var uut = TestUtils.MakeBlock(null, UInt256.Zero, 1);
            var jObj = uut.ToJson(TestProtocolSettings.Default);
            Assert.IsNotNull(jObj);
            Assert.AreEqual("0x942065e93848732c2e7844061fa92d20c5d9dc0bc71d420a1ea71b3431fc21b4", jObj["hash"].AsString());
            Assert.AreEqual(167, jObj["size"].AsNumber()); // 159 + nonce
            Assert.AreEqual(0, jObj["version"].AsNumber());
            Assert.AreEqual("0x0000000000000000000000000000000000000000000000000000000000000000", jObj["previousblockhash"].AsString());
            Assert.AreEqual("0xb9bbfb2804f7582fd4340f5d87d741242afd29d3a02a5c9caa9b67325dbe236c", jObj["merkleroot"].AsString());
            Assert.AreEqual(uut.Header.Timestamp, jObj["time"].AsNumber());
            Assert.AreEqual(uut.Header.Nonce.ToString("X16"), jObj["nonce"].AsString());
            Assert.AreEqual(uut.Header.Index, jObj["index"].AsNumber());
            Assert.AreEqual("NKuyBkoGdZZSLyPbJEetheRhMjeznFZszf", jObj["nextconsensus"].AsString());

            var scObj = (JObject)jObj["witnesses"][0];
            Assert.AreEqual("", scObj["invocation"].AsString());
            Assert.AreEqual("EQ==", scObj["verification"].AsString());

            Assert.IsNotNull(jObj["tx"]);
            var txObj = (JObject)jObj["tx"][0];
            Assert.AreEqual("0xb9bbfb2804f7582fd4340f5d87d741242afd29d3a02a5c9caa9b67325dbe236c", txObj["hash"].AsString());
            Assert.AreEqual(53, txObj["size"].AsNumber());
            Assert.AreEqual(0, txObj["version"].AsNumber());
            Assert.AreEqual(0, ((JArray)txObj["attributes"]).Count);
            Assert.AreEqual("0", txObj["netfee"].AsString());
        }

        [TestMethod]
        public void Witness()
        {
            IVerifiable item = new Block() { Header = new(), };
            Assert.AreEqual(1, item.Witnesses.Length);
            void Actual() => item.Witnesses = null;
            Assert.ThrowsException<NotSupportedException>(Actual);
        }
    }
}
