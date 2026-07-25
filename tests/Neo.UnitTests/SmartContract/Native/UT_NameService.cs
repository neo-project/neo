// Copyright (C) 2015-2026 The Neo Project.
//
// UT_NameService.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Extensions;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.UnitTests.Extensions;
using Neo.VM;
using Neo.VM.Types;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using Array = Neo.VM.Types.Array;
using Boolean = Neo.VM.Types.Boolean;

namespace Neo.UnitTests.SmartContract.Native
{
    [TestClass]
    public class UT_NameService
    {
        private DataCache _snapshot;

        [TestInitialize]
        public void Setup()
        {
            _snapshot = TestBlockchain.GetTestSnapshotCache().CloneCache();
        }

        private static Block PersistingBlock(uint index = 0, ulong timestamp = 1_000_000)
        {
            return new Block
            {
                Header = new Header
                {
                    Index = index,
                    Timestamp = timestamp,
                    Nonce = 0,
                    NextConsensus = UInt160.Zero,
                    PrevHash = UInt256.Zero,
                    MerkleRoot = UInt256.Zero,
                    Witness = Witness.Empty
                },
                Transactions = []
            };
        }

        [TestMethod]
        public void Symbol_And_Decimals()
        {
            Assert.AreEqual("NNS", NativeContract.NameService.Symbol);
            Assert.AreEqual((byte)0, NativeContract.NameService.Decimals);
            Assert.AreEqual(Hardfork.HF_Huyao, NativeContract.NameService.ActiveIn);
        }

        [TestMethod]
        public void TotalSupply_StartsAtZero_OrPositive()
        {
            var ret = NativeContract.NameService.Call(_snapshot, "totalSupply");
            Assert.IsInstanceOfType<Integer>(ret);
            Assert.IsTrue(ret.GetInteger() >= 0);
        }

        [TestMethod]
        public void GetPrice_DefaultLengths()
        {
            var p3 = NativeContract.NameService.Call(_snapshot, "getPrice",
                new ContractParameter(ContractParameterType.Integer) { Value = (BigInteger)3 });
            Assert.IsInstanceOfType<Integer>(p3);
            Assert.AreEqual(200_00000000, (long)p3.GetInteger());
        }

        [TestMethod]
        public void IsAvailable_OpenName_True()
        {
            var block = PersistingBlock();
            var ret = NativeContract.NameService.Call(_snapshot, null, block, "isAvailable",
                new ContractParameter(ContractParameterType.String) { Value = "alice.neo" });
            Assert.IsInstanceOfType<Boolean>(ret);
            Assert.IsTrue(ret.GetBoolean());
        }

        [TestMethod]
        public void Register_And_OwnerOf_Properties()
        {
            var owner = Contract.CreateSignatureRedeemScript(TestProtocolSettings.Default.StandbyCommittee[0]).ToScriptHash();
            var tx = new Transaction
            {
                Version = 0,
                Nonce = 2,
                SystemFee = 0,
                NetworkFee = 0,
                ValidUntilBlock = 100,
                Attributes = [],
                Signers = [new Signer { Account = owner, Scopes = WitnessScope.Global }],
                Script = new byte[] { (byte)OpCode.RET },
                Witnesses = []
            };
            var block = PersistingBlock(timestamp: 10_000_000);

            var ok = CallWithGas(_snapshot, tx, block, "register", 1000_00000000,
                new ContractParameter(ContractParameterType.String) { Value = "bob.neo" },
                new ContractParameter(ContractParameterType.Hash160) { Value = owner });
            Assert.IsTrue(ok.GetBoolean());

            var tokenId = Encoding.UTF8.GetBytes("bob.neo");
            var ownerOf = NativeContract.NameService.Call(_snapshot, "ownerOf",
                new ContractParameter(ContractParameterType.ByteArray) { Value = tokenId });
            Assert.AreSequenceEqual(owner.ToArray(), ownerOf.GetSpan().ToArray());

            var props = NativeContract.NameService.Call(_snapshot, "properties",
                new ContractParameter(ContractParameterType.ByteArray) { Value = tokenId });
            Assert.IsInstanceOfType<Map>(props);
            var map = (Map)props;
            Assert.AreEqual("bob.neo", map["name"].GetString());

            var supply = NativeContract.NameService.Call(_snapshot, "totalSupply");
            Assert.IsTrue(supply.GetInteger() >= 1);
        }

        [TestMethod]
        public void SetPrice_WithoutCommittee_Throws()
        {
            var prices = new ContractParameter(ContractParameterType.Array)
            {
                Value = new List<ContractParameter>
                {
                    new(ContractParameterType.Integer) { Value = (BigInteger)1_00000000 }
                }
            };
            Assert.ThrowsExactly<InvalidOperationException>(() =>
                NativeContract.NameService.Call(_snapshot, "setPrice", prices));
        }

        [TestMethod]
        public void IsLegacyContract_DefaultFalse()
        {
            var legacy = UInt160.Parse("0x0102030405060708090a0b0c0d0e0f1011121314");
            var isLegacy = NativeContract.NameService.Call(_snapshot, "isLegacyContract",
                new ContractParameter(ContractParameterType.Hash160) { Value = legacy });
            Assert.IsFalse(isLegacy.GetBoolean());
        }

        [TestMethod]
        public void RecordType_Values_MatchDns()
        {
            Assert.IsTrue(RecordType.A == (RecordType)1);
            Assert.IsTrue(RecordType.CNAME == (RecordType)5);
            Assert.IsTrue(RecordType.TXT == (RecordType)16);
            Assert.IsTrue(RecordType.AAAA == (RecordType)28);
        }

        [TestMethod]
        public void NameState_RoundTrip()
        {
            var state = new NameState
            {
                Owner = UInt160.Parse("0xaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"),
                Name = "test.neo",
                Expiration = 123456789,
                Admin = UInt160.Parse("0xbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb")
            };
            var item = state.ToStackItem();
            var restored = new NameState();
            restored.FromStackItem(item);
            Assert.AreEqual(state.Owner, restored.Owner);
            Assert.AreEqual(state.Name, restored.Name);
            Assert.AreEqual(state.Expiration, restored.Expiration);
            Assert.AreEqual(state.Admin, restored.Admin);
        }

        [TestMethod]
        public void SetRecord_And_GetRecord_AfterRegister()
        {
            var owner = Contract.CreateSignatureRedeemScript(TestProtocolSettings.Default.StandbyCommittee[0]).ToScriptHash();
            var tx = new Transaction
            {
                Version = 0,
                Nonce = 3,
                SystemFee = 0,
                NetworkFee = 0,
                ValidUntilBlock = 100,
                Attributes = [],
                Signers = [new Signer { Account = owner, Scopes = WitnessScope.Global }],
                Script = new byte[] { (byte)OpCode.RET },
                Witnesses = []
            };
            var block = PersistingBlock(timestamp: 20_000_000);

            CallWithGas(_snapshot, tx, block, "register", 1000_00000000,
                new ContractParameter(ContractParameterType.String) { Value = "dns.neo" },
                new ContractParameter(ContractParameterType.Hash160) { Value = owner });

            CallWithGas(_snapshot, tx, block, "setRecord", 1000_00000000,
                new ContractParameter(ContractParameterType.String) { Value = "dns.neo" },
                new ContractParameter(ContractParameterType.Integer) { Value = (BigInteger)(byte)RecordType.TXT },
                new ContractParameter(ContractParameterType.String) { Value = "hello" });

            var rec = CallWithGas(_snapshot, tx, block, "getRecord", 1000_00000000,
                new ContractParameter(ContractParameterType.String) { Value = "dns.neo" },
                new ContractParameter(ContractParameterType.Integer) { Value = (BigInteger)(byte)RecordType.TXT });
            Assert.AreEqual("hello", rec.GetString());
        }

        private static StackItem CallWithGas(DataCache snapshot, IVerifiable container, Block block, string method, long gas, params ContractParameter[] args)
        {
            using var engine = ApplicationEngine.Create(TriggerType.Application, container, snapshot, block,
                settings: TestProtocolSettings.Default, gas: gas);
            return NativeContract.NameService.Call(engine, method, args);
        }
    }
}
