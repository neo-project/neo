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
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using Boolean = Neo.VM.Types.Boolean;

namespace Neo.UnitTests.SmartContract.Native
{
    [TestClass]
    public class UT_NameService
    {
        private static readonly byte[] RetScript = [(byte)OpCode.RET];
        private const long TestGas = 1000_00000000;

        private DataCache _snapshotCache;
        private Block _persistingBlock;

        [TestInitialize]
        public void TestSetup()
        {
            _snapshotCache = TestBlockchain.GetTestSnapshotCache();
            _persistingBlock = new Block
            {
                Header = new Header
                {
                    Index = 0,
                    Timestamp = 1_000_000,
                    Nonce = 0,
                    NextConsensus = UInt160.Zero,
                    PrevHash = UInt256.Zero,
                    MerkleRoot = UInt256.Zero,
                    Witness = Witness.Empty
                },
                Transactions = []
            };
        }

        private static Block BlockAt(uint index, ulong timestamp = 1_000_000) =>
            new()
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

        private static UInt160 OwnerHash() =>
            Contract.CreateSignatureRedeemScript(TestProtocolSettings.Default.StandbyCommittee[0]).ToScriptHash();

        private static ProtocolSettings SettingsWithHuyaoAt(uint height)
        {
            // Omitted hardforks become 0; Huyao activates at `height`.
            var json = UT_ProtocolSettings.CreateHFSettings($"\"HF_Huyao\": {height}");
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
            return ProtocolSettings.Load(stream);
        }

        private static StackItem CallWithGas(
            DataCache snapshot,
            IVerifiable container,
            Block block,
            string method,
            long gas,
            ProtocolSettings settings = null,
            params ContractParameter[] args)
        {
            using var engine = ApplicationEngine.Create(TriggerType.Application, container, snapshot, block,
                settings: settings ?? TestProtocolSettings.Default, gas: gas);
            return NativeContract.NameService.Call(engine, method, args);
        }

        private static StackItem CallWithWitness(
            DataCache snapshot,
            Block block,
            UInt160[] witnesses,
            string method,
            long gas = TestGas,
            ProtocolSettings settings = null,
            params ContractParameter[] args)
        {
            return CallWithGas(snapshot, new Nep17NativeContractExtensions.ManualWitness(witnesses), block, method, gas, settings, args);
        }

        #region Basics

        [TestMethod]
        public void Check_Name() =>
            Assert.AreEqual(nameof(NameService), NativeContract.NameService.Name);

        [TestMethod]
        public void Check_Symbol() =>
            Assert.AreEqual("NNS", NativeContract.NameService.Symbol);

        [TestMethod]
        public void Check_Decimals() =>
            Assert.AreEqual((byte)0, NativeContract.NameService.Decimals);

        [TestMethod]
        public void Check_ActiveIn() =>
            Assert.AreEqual(Hardfork.HF_Huyao, NativeContract.NameService.ActiveIn);

        [TestMethod]
        public void Check_Id() =>
            Assert.AreEqual(-12, NativeContract.NameService.Id);

        [TestMethod]
        public void TotalSupply_NonNegative()
        {
            var snapshot = _snapshotCache.CloneCache();
            var ret = NativeContract.NameService.Call(snapshot, "totalSupply");
            Assert.IsInstanceOfType<Integer>(ret);
            Assert.IsTrue(ret.GetInteger() >= 0);
        }

        [TestMethod]
        public void GetPrice_Defaults()
        {
            var snapshot = _snapshotCache.CloneCache();
            var p3 = NativeContract.NameService.Call(snapshot, "getPrice",
                new ContractParameter(ContractParameterType.Integer) { Value = (BigInteger)3 });
            Assert.IsInstanceOfType<Integer>(p3);
            Assert.AreEqual(200_00000000, (long)p3.GetInteger());
        }

        #endregion

        #region Hardfork

        [TestMethod]
        public void Test_HF_Huyao_IsActive()
        {
            var settings = SettingsWithHuyaoAt(10);

            Assert.IsFalse(NativeContract.NameService.IsActive(settings, 9));
            Assert.IsTrue(NativeContract.NameService.IsActive(settings, 10));
            Assert.IsTrue(NativeContract.NameService.IsActive(settings, 11));
        }

        [TestMethod]
        public void Test_HF_Huyao_InitializeBlock()
        {
            var settings = SettingsWithHuyaoAt(10);

            Assert.IsFalse(NativeContract.NameService.IsInitializeBlock(settings, 9, out var hfs));
            Assert.IsNull(hfs);

            Assert.IsTrue(NativeContract.NameService.IsInitializeBlock(settings, 10, out hfs));
            Assert.IsNotNull(hfs);
            Assert.Contains(Hardfork.HF_Huyao, hfs);
        }

        [TestMethod]
        public void Test_HF_Huyao_ContractMethods_ViaApplicationEngine()
        {
            var settings = SettingsWithHuyaoAt(10);
            var snapshot = _snapshotCache.CloneCache();
            var block = BlockAt(10);

            using var engine = ApplicationEngine.Create(TriggerType.Application,
                new Nep17NativeContractExtensions.ManualWitness(UInt160.Zero), snapshot, block, settings: settings);

            var methods = NativeContract.NameService.GetContractMethods(engine);
            var names = methods.Values.Select(m => m.Name).ToHashSet();

            string[] expectedMethods =
            [
                "symbol", "decimals", "totalSupply", "balanceOf", "ownerOf", "tokens", "tokensOf",
                "properties", "transfer", "register", "setPrice", "addRoot", "setAdmin", "setRecord",
                "onNEP11Payment", "addLegacyContract"
            ];
            foreach (var methodName in expectedMethods)
                Assert.Contains(methodName, names);

            var transfer = methods.Values.Single(m => m.Name == "transfer");
            Assert.AreEqual(CallFlags.States | CallFlags.AllowCall | CallFlags.AllowNotify, transfer.RequiredCallFlags);

            var setPrice = methods.Values.Single(m => m.Name == "setPrice");
            Assert.AreEqual(CallFlags.States, setPrice.RequiredCallFlags);
        }

        [TestMethod]
        public void Test_HF_Huyao_GetContractState_SupportedStandards()
        {
            var settings = SettingsWithHuyaoAt(10);
            var state = NativeContract.NameService.GetContractState(settings, 10);
            Assert.Contains("NEP-11", state.Manifest.SupportedStandards);
            Assert.AreEqual(nameof(NameService), state.Manifest.Name);
            var eventNames = state.Manifest.Abi.Events.Select(e => e.Name).ToHashSet();
            Assert.Contains("Transfer", eventNames);
            Assert.Contains("SetAdmin", eventNames);
            Assert.Contains("Renew", eventNames);
        }

        #endregion

        #region Committee / witness

        [TestMethod]
        public void SetPrice_WithoutCommittee_Throws()
        {
            var snapshot = _snapshotCache.CloneCache();
            List<ContractParameter> priceParams =
            [
                new(ContractParameterType.Integer) { Value = (BigInteger)1_00000000 }
            ];
            var prices = new ContractParameter(ContractParameterType.Array) { Value = priceParams };

            Assert.ThrowsExactly<InvalidOperationException>(() =>
                CallWithWitness(snapshot, _persistingBlock, [], "setPrice", args: prices));
        }

        [TestMethod]
        public void SetPrice_WithCommittee_Succeeds()
        {
            var snapshot = _snapshotCache.CloneCache();
            var committee = NativeContract.NEO.GetCommitteeAddress(snapshot);
            List<ContractParameter> priceParams =
            [
                new(ContractParameterType.Integer) { Value = (BigInteger)3_00000000 },
                new(ContractParameterType.Integer) { Value = (BigInteger)(-1) },
                new(ContractParameterType.Integer) { Value = (BigInteger)(-1) },
                new(ContractParameterType.Integer) { Value = (BigInteger)100_00000000 },
            ];
            var prices = new ContractParameter(ContractParameterType.Array) { Value = priceParams };

            var ret = CallWithWitness(snapshot, _persistingBlock, [committee], "setPrice", args: prices);
            Assert.IsNotNull(ret);
            Assert.IsTrue(ret.IsNull);

            var p0 = NativeContract.NameService.Call(snapshot, "getPrice",
                new ContractParameter(ContractParameterType.Integer) { Value = (BigInteger)3 });
            // length 3 uses index 3 when list long enough; our list has index 3 = 100_00000000
            // getPrice(3) uses prices[3] if length < prices.Length
            Assert.AreEqual(100_00000000, (long)p0.GetInteger());
        }

        [TestMethod]
        public void AddRoot_WithoutCommittee_Throws()
        {
            var snapshot = _snapshotCache.CloneCache();
            Assert.ThrowsExactly<InvalidOperationException>(() =>
                CallWithWitness(snapshot, _persistingBlock, [], "addRoot",
                    args: new ContractParameter(ContractParameterType.String) { Value = "test" }));
        }

        [TestMethod]
        public void AddRoot_WithCommittee_Succeeds()
        {
            var snapshot = _snapshotCache.CloneCache();
            var committee = NativeContract.NEO.GetCommitteeAddress(snapshot);
            var ret = CallWithWitness(snapshot, _persistingBlock, [committee], "addRoot",
                args: new ContractParameter(ContractParameterType.String) { Value = "test" });
            Assert.IsNotNull(ret);
            Assert.IsTrue(ret.IsNull);
        }

        [TestMethod]
        public void AddLegacyContract_WithoutCommittee_Throws()
        {
            var snapshot = _snapshotCache.CloneCache();
            var legacy = UInt160.Parse("0x0102030405060708090a0b0c0d0e0f1011121314");
            Assert.ThrowsExactly<InvalidOperationException>(() =>
                CallWithWitness(snapshot, _persistingBlock, [], "addLegacyContract",
                    args: new ContractParameter(ContractParameterType.Hash160) { Value = legacy }));
        }

        [TestMethod]
        public void AddLegacyContract_WithCommittee_ThenIsLegacy()
        {
            var snapshot = _snapshotCache.CloneCache();
            var committee = NativeContract.NEO.GetCommitteeAddress(snapshot);
            var legacy = UInt160.Parse("0x0102030405060708090a0b0c0d0e0f1011121314");

            CallWithWitness(snapshot, _persistingBlock, [committee], "addLegacyContract",
                args: new ContractParameter(ContractParameterType.Hash160) { Value = legacy });

            var isLegacy = NativeContract.NameService.Call(snapshot, "isLegacyContract",
                new ContractParameter(ContractParameterType.Hash160) { Value = legacy });
            Assert.IsTrue(isLegacy.GetBoolean());
        }

        #endregion

        #region Register / transfer / admin (witness)

        [TestMethod]
        public void Register_WithoutWitness_Throws()
        {
            var snapshot = _snapshotCache.CloneCache();
            var owner = OwnerHash();
            Assert.ThrowsExactly<InvalidOperationException>(() =>
                CallWithWitness(snapshot, BlockAt(0, 10_000_000), [], "register",
                    args:
                    [
                        new ContractParameter(ContractParameterType.String) { Value = "nowit.neo" },
                        new ContractParameter(ContractParameterType.Hash160) { Value = owner }
                    ]));
        }

        [TestMethod]
        public void Register_WithOwnerWitness_Succeeds()
        {
            var snapshot = _snapshotCache.CloneCache();
            var owner = OwnerHash();
            var block = BlockAt(0, 10_000_000);

            var ok = CallWithWitness(snapshot, block, [owner], "register",
                args:
                [
                    new ContractParameter(ContractParameterType.String) { Value = "bob.neo" },
                    new ContractParameter(ContractParameterType.Hash160) { Value = owner }
                ]);
            Assert.IsTrue(ok.GetBoolean());

            var tokenId = Encoding.UTF8.GetBytes("bob.neo");
            var ownerOf = NativeContract.NameService.Call(snapshot, "ownerOf",
                new ContractParameter(ContractParameterType.ByteArray) { Value = tokenId });
            Assert.AreSequenceEqual(owner.ToArray(), ownerOf.GetSpan().ToArray());

            var props = NativeContract.NameService.Call(snapshot, "properties",
                new ContractParameter(ContractParameterType.ByteArray) { Value = tokenId });
            Assert.IsInstanceOfType<Map>(props);
            Assert.AreEqual("bob.neo", ((Map)props)["name"].GetString());
        }

        [TestMethod]
        public void Register_Then_Transfer_WithWitness()
        {
            var snapshot = _snapshotCache.CloneCache();
            var owner = OwnerHash();
            var other = Contract.CreateSignatureRedeemScript(TestProtocolSettings.Default.StandbyCommittee[1]).ToScriptHash();
            var block = BlockAt(0, 10_000_000);
            var tokenId = Encoding.UTF8.GetBytes("xfer.neo");

            CallWithWitness(snapshot, block, [owner], "register",
                args:
                [
                    new ContractParameter(ContractParameterType.String) { Value = "xfer.neo" },
                    new ContractParameter(ContractParameterType.Hash160) { Value = owner }
                ]);

            // No witness → transfer returns false (NEP-11), does not throw
            var denied = CallWithWitness(snapshot, block, [], "transfer",
                args:
                [
                    new ContractParameter(ContractParameterType.Hash160) { Value = other },
                    new ContractParameter(ContractParameterType.ByteArray) { Value = tokenId },
                    new ContractParameter(ContractParameterType.Any) { Value = null }
                ]);
            Assert.IsFalse(denied.GetBoolean());

            var ok = CallWithWitness(snapshot, block, [owner], "transfer",
                args:
                [
                    new ContractParameter(ContractParameterType.Hash160) { Value = other },
                    new ContractParameter(ContractParameterType.ByteArray) { Value = tokenId },
                    new ContractParameter(ContractParameterType.Any) { Value = null }
                ]);
            Assert.IsTrue(ok.GetBoolean());

            var ownerOf = NativeContract.NameService.Call(snapshot, "ownerOf",
                new ContractParameter(ContractParameterType.ByteArray) { Value = tokenId });
            Assert.AreSequenceEqual(other.ToArray(), ownerOf.GetSpan().ToArray());
        }

        [TestMethod]
        public void SetAdmin_RequiresOwnerWitness()
        {
            var snapshot = _snapshotCache.CloneCache();
            var owner = OwnerHash();
            var admin = Contract.CreateSignatureRedeemScript(TestProtocolSettings.Default.StandbyCommittee[2]).ToScriptHash();
            var block = BlockAt(0, 10_000_000);

            CallWithWitness(snapshot, block, [owner], "register",
                args:
                [
                    new ContractParameter(ContractParameterType.String) { Value = "adm.neo" },
                    new ContractParameter(ContractParameterType.Hash160) { Value = owner }
                ]);

            Assert.ThrowsExactly<InvalidOperationException>(() =>
                CallWithWitness(snapshot, block, [admin], "setAdmin",
                    args:
                    [
                        new ContractParameter(ContractParameterType.String) { Value = "adm.neo" },
                        new ContractParameter(ContractParameterType.Hash160) { Value = admin }
                    ]));

            // Owner + admin both witness (admin must witness appointment)
            CallWithWitness(snapshot, block, [owner, admin], "setAdmin",
                args:
                [
                    new ContractParameter(ContractParameterType.String) { Value = "adm.neo" },
                    new ContractParameter(ContractParameterType.Hash160) { Value = admin }
                ]);
        }

        [TestMethod]
        public void SetRecord_RequiresOwnerOrAdmin()
        {
            var snapshot = _snapshotCache.CloneCache();
            var owner = OwnerHash();
            var stranger = Contract.CreateSignatureRedeemScript(TestProtocolSettings.Default.StandbyCommittee[3]).ToScriptHash();
            var block = BlockAt(0, 20_000_000);

            CallWithWitness(snapshot, block, [owner], "register",
                args:
                [
                    new ContractParameter(ContractParameterType.String) { Value = "dns.neo" },
                    new ContractParameter(ContractParameterType.Hash160) { Value = owner }
                ]);

            Assert.ThrowsExactly<InvalidOperationException>(() =>
                CallWithWitness(snapshot, block, [stranger], "setRecord",
                    args:
                    [
                        new ContractParameter(ContractParameterType.String) { Value = "dns.neo" },
                        new ContractParameter(ContractParameterType.Integer) { Value = (BigInteger)(byte)RecordType.TXT },
                        new ContractParameter(ContractParameterType.String) { Value = "nope" }
                    ]));

            CallWithWitness(snapshot, block, [owner], "setRecord",
                args:
                [
                    new ContractParameter(ContractParameterType.String) { Value = "dns.neo" },
                    new ContractParameter(ContractParameterType.Integer) { Value = (BigInteger)(byte)RecordType.TXT },
                    new ContractParameter(ContractParameterType.String) { Value = "hello" }
                ]);

            var rec = CallWithWitness(snapshot, block, [], "getRecord",
                args:
                [
                    new ContractParameter(ContractParameterType.String) { Value = "dns.neo" },
                    new ContractParameter(ContractParameterType.Integer) { Value = (BigInteger)(byte)RecordType.TXT }
                ]);
            Assert.AreEqual("hello", rec.GetString());
        }

        #endregion

        #region Availability / migration / state

        [TestMethod]
        public void IsAvailable_OpenName_True()
        {
            var snapshot = _snapshotCache.CloneCache();
            var ret = NativeContract.NameService.Call(snapshot, null, BlockAt(0), "isAvailable",
                new ContractParameter(ContractParameterType.String) { Value = "alice.neo" });
            Assert.IsInstanceOfType<Boolean>(ret);
            Assert.IsTrue(ret.GetBoolean());
        }

        [TestMethod]
        public void IsLegacyContract_DefaultFalse()
        {
            var snapshot = _snapshotCache.CloneCache();
            var legacy = UInt160.Parse("0x0102030405060708090a0b0c0d0e0f1011121314");
            var isLegacy = NativeContract.NameService.Call(snapshot, "isLegacyContract",
                new ContractParameter(ContractParameterType.Hash160) { Value = legacy });
            Assert.IsFalse(isLegacy.GetBoolean());
        }

        [TestMethod]
        public void OnNEP11Payment_WithoutLegacy_Throws()
        {
            var snapshot = _snapshotCache.CloneCache();
            var from = OwnerHash();
            // CallingScriptHash will be the dynamic call script hash, not a legacy contract
            Assert.ThrowsExactly<InvalidOperationException>(() =>
                CallWithWitness(snapshot, BlockAt(0, 10_000_000), [from], "onNEP11Payment",
                    args:
                    [
                        new ContractParameter(ContractParameterType.Hash160) { Value = from },
                        new ContractParameter(ContractParameterType.Integer) { Value = (BigInteger)1 },
                        new ContractParameter(ContractParameterType.ByteArray) { Value = Encoding.UTF8.GetBytes("mig.neo") },
                        new ContractParameter(ContractParameterType.Any) { Value = null }
                    ]));
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
        public void ApplicationEngine_Direct_SymbolViaLoadScript()
        {
            var snapshot = _snapshotCache.CloneCache();
            using var engine = ApplicationEngine.Create(TriggerType.Application,
                new Nep17NativeContractExtensions.ManualWitness(), snapshot, _persistingBlock,
                settings: TestProtocolSettings.Default, gas: TestGas);

            using var script = new ScriptBuilder();
            script.EmitDynamicCall(NativeContract.NameService.Hash, "symbol");
            engine.LoadScript(script.ToArray());
            Assert.AreEqual(VMState.HALT, engine.Execute());
            Assert.AreEqual("NNS", engine.ResultStack.Pop().GetString());
        }

        #endregion
    }
}
