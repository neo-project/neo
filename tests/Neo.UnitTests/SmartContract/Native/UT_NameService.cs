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
using Neo.SmartContract.Iterators;
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
using Boolean = Neo.VM.Types.Boolean;

namespace Neo.UnitTests.SmartContract.Native
{
    [TestClass]
    public class UT_NameService
    {
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

            // Production defaults to register paused (public register closed).
            // Unit tests unpause registration unless a test re-pauses it.
            var committee = NativeContract.NEO.GetCommitteeAddress(_snapshotCache);
            CallWithWitness(_snapshotCache, _persistingBlock, [committee], "setRegisterPaused",
                args: new ContractParameter(ContractParameterType.Boolean) { Value = false });
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

        private static void AssertOwner(DataCache snapshot, Block block, byte[] tokenId, UInt160 expected)
        {
            var ownerOf = NativeContract.NameService.Call(snapshot, null, block, "ownerOf",
                new ContractParameter(ContractParameterType.ByteArray) { Value = tokenId });
            Assert.AreSequenceEqual(expected.ToArray(), ownerOf.GetSpan().ToArray());
        }

        private static ProtocolSettings SettingsWithHuyaoAt(uint height)
        {
            // Omitted hardforks become 0; Huyao activates at `height`.
            var json = UT_ProtocolSettings.CreateHFSettings($"\"HF_Huyao\": {height}");
            using var stream = new MemoryStream(Utility.StrictUTF8.GetBytes(json));
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

        /// <summary>
        /// Invoke a NameService method with a faked calling script hash (legacy NNS migration).
        /// </summary>
        private static StackItem CallWithCallingScript(
            DataCache snapshot,
            Block block,
            UInt160 callingScriptHash,
            UInt160[] witnesses,
            string method,
            params ContractParameter[] args)
        {
            using var engine = ApplicationEngine.Create(TriggerType.Application,
                new Nep17NativeContractExtensions.ManualWitness(witnesses), snapshot, block,
                settings: TestProtocolSettings.Default, gas: TestGas);
            using var script = new ScriptBuilder();
            script.EmitDynamicCall(NativeContract.NameService.Hash, method, args);
            engine.LoadScript(script.ToArray());
            engine.CurrentContext.GetState<ExecutionContextState>().NativeCallingScriptHash = callingScriptHash;
            engine.CurrentContext.GetState<ExecutionContextState>().ScriptHash = callingScriptHash;

            if (engine.Execute() != VMState.HALT)
            {
                var exception = engine.FaultException;
                while (exception?.InnerException != null) exception = exception.InnerException;
                throw exception ?? new InvalidOperationException();
            }
            return engine.ResultStack.Count > 0 ? engine.ResultStack.Pop() : StackItem.Null;
        }

        private static void RegisterName(DataCache snapshot, Block block, UInt160 owner, string name)
        {
            CallWithWitness(snapshot, block, [owner], "register",
                args:
                [
                    new ContractParameter(ContractParameterType.String) { Value = name },
                    new ContractParameter(ContractParameterType.Hash160) { Value = owner }
                ]);
        }

        private static ContractParameter RecordTypeParam(RecordType type) =>
            new(ContractParameterType.Integer) { Value = (BigInteger)(byte)type };

        private static List<StackItem> DrainIterator(StackItem item)
        {
            var iter = item.GetInterface<object>() as IIterator
                ?? throw new AssertFailedException("Expected IIterator");
            var values = new List<StackItem>();
            while (iter.Next())
                values.Add(iter.Value());
            return values;
        }

        /// <summary>
        /// Call a method returning an iterator and drain it before the engine is disposed.
        /// </summary>
        private static List<StackItem> CallAndDrain(
            DataCache snapshot,
            Block block,
            string method,
            params ContractParameter[] args)
        {
            using var engine = ApplicationEngine.Create(TriggerType.Application,
                new Nep17NativeContractExtensions.ManualWitness(), snapshot, block,
                settings: TestProtocolSettings.Default, gas: TestGas);
            using var script = new ScriptBuilder();
            script.EmitDynamicCall(NativeContract.NameService.Hash, method, args);
            engine.LoadScript(script.ToArray());
            Assert.AreEqual(VMState.HALT, engine.Execute());
            return DrainIterator(engine.ResultStack.Pop());
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
                "onNEP11Payment", "addLegacyContract", "setRegisterPaused", "isRegisterPaused"
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

        [TestMethod]
        public void SetRegisterPaused_WithoutCommittee_Throws()
        {
            var snapshot = _snapshotCache.CloneCache();
            Assert.ThrowsExactly<InvalidOperationException>(() =>
                CallWithWitness(snapshot, _persistingBlock, [], "setRegisterPaused",
                    args: new ContractParameter(ContractParameterType.Boolean) { Value = true }));
        }

        [TestMethod]
        public void RegisterPaused_BlocksPublicRegister_UntilCommitteeUnpauses()
        {
            // Fresh chain: register is paused after native init (TestSetup unpaused _snapshotCache only).
            var snapshot = TestBlockchain.GetTestSnapshotCache();
            var block = BlockAt(0, 10_000_000);
            var owner = OwnerHash();
            var committee = NativeContract.NEO.GetCommitteeAddress(snapshot);

            Assert.IsTrue(NativeContract.NameService.Call(snapshot, "isRegisterPaused").GetBoolean());

            Assert.IsFalse(NativeContract.NameService.Call(snapshot, null, block, "isAvailable",
                new ContractParameter(ContractParameterType.String) { Value = "paused.neo" }).GetBoolean());

            Assert.ThrowsExactly<InvalidOperationException>(() =>
                CallWithWitness(snapshot, block, [owner], "register",
                    args:
                    [
                        new ContractParameter(ContractParameterType.String) { Value = "paused.neo" },
                        new ContractParameter(ContractParameterType.Hash160) { Value = owner }
                    ]));

            CallWithWitness(snapshot, block, [committee], "setRegisterPaused",
                args: new ContractParameter(ContractParameterType.Boolean) { Value = false });

            Assert.IsFalse(NativeContract.NameService.Call(snapshot, "isRegisterPaused").GetBoolean());
            Assert.IsTrue(NativeContract.NameService.Call(snapshot, null, block, "isAvailable",
                new ContractParameter(ContractParameterType.String) { Value = "paused.neo" }).GetBoolean());

            var ok = CallWithWitness(snapshot, block, [owner], "register",
                args:
                [
                    new ContractParameter(ContractParameterType.String) { Value = "paused.neo" },
                    new ContractParameter(ContractParameterType.Hash160) { Value = owner }
                ]);
            Assert.IsTrue(ok.GetBoolean());

            // Committee can re-pause (blocks further public registrations).
            CallWithWitness(snapshot, block, [committee], "setRegisterPaused",
                args: new ContractParameter(ContractParameterType.Boolean) { Value = true });
            Assert.IsTrue(NativeContract.NameService.Call(snapshot, "isRegisterPaused").GetBoolean());
            Assert.ThrowsExactly<InvalidOperationException>(() =>
                CallWithWitness(snapshot, block, [owner], "register",
                    args:
                    [
                        new ContractParameter(ContractParameterType.String) { Value = "another.neo" },
                        new ContractParameter(ContractParameterType.Hash160) { Value = owner }
                    ]));
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

            var tokenId = Utility.StrictUTF8.GetBytes("bob.neo");
            AssertOwner(snapshot, block, tokenId, owner);

            var props = NativeContract.NameService.Call(snapshot, null, block, "properties",
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
            var admin = Contract.CreateSignatureRedeemScript(TestProtocolSettings.Default.StandbyCommittee[2]).ToScriptHash();
            var block = BlockAt(0, 10_000_000);
            var tokenId = Utility.StrictUTF8.GetBytes("xfer.neo");

            CallWithWitness(snapshot, block, [owner], "register",
                args:
                [
                    new ContractParameter(ContractParameterType.String) { Value = "xfer.neo" },
                    new ContractParameter(ContractParameterType.Hash160) { Value = owner }
                ]);

            // Ownership and balances after register
            AssertOwner(snapshot, block, tokenId, owner);
            Assert.AreEqual(1, (int)NativeContract.NameService.Call(snapshot, null, block, "balanceOf",
                new ContractParameter(ContractParameterType.Hash160) { Value = owner }).GetInteger());
            Assert.AreEqual(0, (int)NativeContract.NameService.Call(snapshot, null, block, "balanceOf",
                new ContractParameter(ContractParameterType.Hash160) { Value = other }).GetInteger());

            // Admin is cleared on ownership transfer (non-native NNS behavior)
            CallWithWitness(snapshot, block, [owner, admin], "setAdmin",
                args:
                [
                    new ContractParameter(ContractParameterType.String) { Value = "xfer.neo" },
                    new ContractParameter(ContractParameterType.Hash160) { Value = admin }
                ]);

            // No witness → transfer returns false (NEP-11), ownership unchanged
            var denied = CallWithWitness(snapshot, block, [], "transfer",
                args:
                [
                    new ContractParameter(ContractParameterType.Hash160) { Value = other },
                    new ContractParameter(ContractParameterType.ByteArray) { Value = tokenId },
                    new ContractParameter(ContractParameterType.Any) { Value = null }
                ]);
            Assert.IsFalse(denied.GetBoolean());
            AssertOwner(snapshot, block, tokenId, owner);

            var ok = CallWithWitness(snapshot, block, [owner], "transfer",
                args:
                [
                    new ContractParameter(ContractParameterType.Hash160) { Value = other },
                    new ContractParameter(ContractParameterType.ByteArray) { Value = tokenId },
                    new ContractParameter(ContractParameterType.Any) { Value = null }
                ]);
            Assert.IsTrue(ok.GetBoolean());

            // Ownership transferred: ownerOf, balances, admin cleared
            AssertOwner(snapshot, block, tokenId, other);
            Assert.AreEqual(0, (int)NativeContract.NameService.Call(snapshot, null, block, "balanceOf",
                new ContractParameter(ContractParameterType.Hash160) { Value = owner }).GetInteger());
            Assert.AreEqual(1, (int)NativeContract.NameService.Call(snapshot, null, block, "balanceOf",
                new ContractParameter(ContractParameterType.Hash160) { Value = other }).GetInteger());

            var props = (Map)NativeContract.NameService.Call(snapshot, null, block, "properties",
                new ContractParameter(ContractParameterType.ByteArray) { Value = tokenId });
            Assert.IsTrue(props["admin"].IsNull);
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

        [TestMethod]
        public void ExpiredName_RecordAndTransfer_Throw()
        {
            var snapshot = _snapshotCache.CloneCache();
            var owner = OwnerHash();
            var other = Contract.CreateSignatureRedeemScript(TestProtocolSettings.Default.StandbyCommittee[1]).ToScriptHash();
            // Register at t=1_000_000 → expires at 1_000_000 + OneYear
            var registerAt = 1_000_000ul;
            var oneYear = 365ul * (ulong)TimeSpan.MillisecondsPerDay;
            var registerBlock = BlockAt(0, registerAt);
            var expiredBlock = BlockAt(1, registerAt + oneYear);

            CallWithWitness(snapshot, registerBlock, [owner], "register",
                args:
                [
                    new ContractParameter(ContractParameterType.String) { Value = "old.neo" },
                    new ContractParameter(ContractParameterType.Hash160) { Value = owner }
                ]);

            Assert.ThrowsExactly<InvalidOperationException>(() =>
                CallWithWitness(snapshot, expiredBlock, [owner], "setRecord",
                    args:
                    [
                        new ContractParameter(ContractParameterType.String) { Value = "old.neo" },
                        new ContractParameter(ContractParameterType.Integer) { Value = (BigInteger)(byte)RecordType.TXT },
                        new ContractParameter(ContractParameterType.String) { Value = "stale" }
                    ]));

            Assert.ThrowsExactly<InvalidOperationException>(() =>
                CallWithWitness(snapshot, expiredBlock, [], "getRecord",
                    args:
                    [
                        new ContractParameter(ContractParameterType.String) { Value = "old.neo" },
                        new ContractParameter(ContractParameterType.Integer) { Value = (BigInteger)(byte)RecordType.TXT }
                    ]));

            var tokenId = Utility.StrictUTF8.GetBytes("old.neo");
            Assert.ThrowsExactly<InvalidOperationException>(() =>
                CallWithWitness(snapshot, expiredBlock, [owner], "transfer",
                    args:
                    [
                        new ContractParameter(ContractParameterType.Hash160) { Value = other },
                        new ContractParameter(ContractParameterType.ByteArray) { Value = tokenId },
                        new ContractParameter(ContractParameterType.Any) { Value = null }
                    ]));

            Assert.ThrowsExactly<InvalidOperationException>(() =>
                NativeContract.NameService.Call(snapshot, null, expiredBlock, "ownerOf",
                    new ContractParameter(ContractParameterType.ByteArray) { Value = tokenId }));
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
                        new ContractParameter(ContractParameterType.ByteArray) { Value = Utility.StrictUTF8.GetBytes("mig.neo") },
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

        #region Renew / records / resolve / iterators

        [TestMethod]
        public void Renew_ExtendsExpiration()
        {
            var snapshot = _snapshotCache.CloneCache();
            var owner = OwnerHash();
            var t0 = 10_000_000ul;
            var oneYear = 365ul * (ulong)TimeSpan.MillisecondsPerDay;
            var block = BlockAt(0, t0);

            RegisterName(snapshot, block, owner, "renew.neo");

            var props = (Map)NativeContract.NameService.Call(snapshot, null, block, "properties",
                new ContractParameter(ContractParameterType.ByteArray) { Value = Utility.StrictUTF8.GetBytes("renew.neo") });
            Assert.AreEqual(t0 + oneYear, (ulong)props["expiration"].GetInteger());

            var newExp = CallWithWitness(snapshot, block, [owner], "renew",
                args: new ContractParameter(ContractParameterType.String) { Value = "renew.neo" });
            Assert.AreEqual(t0 + oneYear * 2, (ulong)newExp.GetInteger());

            var newExp2 = CallWithWitness(snapshot, block, [owner], "renew",
                args:
                [
                    new ContractParameter(ContractParameterType.String) { Value = "renew.neo" },
                    new ContractParameter(ContractParameterType.Integer) { Value = (BigInteger)2 }
                ]);
            Assert.AreEqual(t0 + oneYear * 4, (ulong)newExp2.GetInteger());
        }

        [TestMethod]
        public void Renew_YearsOutOfRange_Throws()
        {
            var snapshot = _snapshotCache.CloneCache();
            var owner = OwnerHash();
            var block = BlockAt(0, 10_000_000);
            RegisterName(snapshot, block, owner, "yrange.neo");

            Assert.ThrowsExactly<ArgumentException>(() =>
                CallWithWitness(snapshot, block, [owner], "renew",
                    args:
                    [
                        new ContractParameter(ContractParameterType.String) { Value = "yrange.neo" },
                        new ContractParameter(ContractParameterType.Integer) { Value = (BigInteger)0 }
                    ]));

            Assert.ThrowsExactly<ArgumentException>(() =>
                CallWithWitness(snapshot, block, [owner], "renew",
                    args:
                    [
                        new ContractParameter(ContractParameterType.String) { Value = "yrange.neo" },
                        new ContractParameter(ContractParameterType.Integer) { Value = (BigInteger)11 }
                    ]));
        }

        [TestMethod]
        public void Renew_BeyondTenYearsTotal_Throws()
        {
            var snapshot = _snapshotCache.CloneCache();
            var owner = OwnerHash();
            var block = BlockAt(0, 10_000_000);
            RegisterName(snapshot, block, owner, "long.neo");

            // register = 1y; renew 9y => 10y from now OK; renew 1 more => exceeds 10y from now
            CallWithWitness(snapshot, block, [owner], "renew",
                args:
                [
                    new ContractParameter(ContractParameterType.String) { Value = "long.neo" },
                    new ContractParameter(ContractParameterType.Integer) { Value = (BigInteger)9 }
                ]);

            Assert.ThrowsExactly<ArgumentException>(() =>
                CallWithWitness(snapshot, block, [owner], "renew",
                    args: new ContractParameter(ContractParameterType.String) { Value = "long.neo" }));
        }

        [TestMethod]
        public void Records_A_AAAA_CNAME_Delete_Resolve()
        {
            var snapshot = _snapshotCache.CloneCache();
            var owner = OwnerHash();
            var block = BlockAt(0, 20_000_000);
            RegisterName(snapshot, block, owner, "host.neo");

            // A record (public IPv4)
            CallWithWitness(snapshot, block, [owner], "setRecord",
                args:
                [
                    new ContractParameter(ContractParameterType.String) { Value = "host.neo" },
                    RecordTypeParam(RecordType.A),
                    new ContractParameter(ContractParameterType.String) { Value = "1.1.1.1" }
                ]);
            Assert.AreEqual("1.1.1.1", CallWithWitness(snapshot, block, [], "getRecord",
                args:
                [
                    new ContractParameter(ContractParameterType.String) { Value = "host.neo" },
                    RecordTypeParam(RecordType.A)
                ]).GetString());

            // Invalid private A record
            Assert.ThrowsExactly<FormatException>(() =>
                CallWithWitness(snapshot, block, [owner], "setRecord",
                    args:
                    [
                        new ContractParameter(ContractParameterType.String) { Value = "host.neo" },
                        RecordTypeParam(RecordType.A),
                        new ContractParameter(ContractParameterType.String) { Value = "192.168.1.1" }
                    ]));

            // AAAA
            CallWithWitness(snapshot, block, [owner], "setRecord",
                args:
                [
                    new ContractParameter(ContractParameterType.String) { Value = "host.neo" },
                    RecordTypeParam(RecordType.AAAA),
                    new ContractParameter(ContractParameterType.String) { Value = "2001:4860:4860:0:0:0:0:8888" }
                ]);
            Assert.AreEqual("2001:4860:4860:0:0:0:0:8888", CallWithWitness(snapshot, block, [], "getRecord",
                args:
                [
                    new ContractParameter(ContractParameterType.String) { Value = "host.neo" },
                    RecordTypeParam(RecordType.AAAA)
                ]).GetString());

            // Invalid AAAA (link-local / out of allowed range)
            Assert.ThrowsExactly<FormatException>(() =>
                CallWithWitness(snapshot, block, [owner], "setRecord",
                    args:
                    [
                        new ContractParameter(ContractParameterType.String) { Value = "host.neo" },
                        RecordTypeParam(RecordType.AAAA),
                        new ContractParameter(ContractParameterType.String) { Value = "fe80::1" }
                    ]));

            // Subdomain CNAME -> resolve chain
            RegisterName(snapshot, block, owner, "alias.neo");
            CallWithWitness(snapshot, block, [owner], "setRecord",
                args:
                [
                    new ContractParameter(ContractParameterType.String) { Value = "www.host.neo" },
                    RecordTypeParam(RecordType.CNAME),
                    new ContractParameter(ContractParameterType.String) { Value = "alias.neo" }
                ]);
            CallWithWitness(snapshot, block, [owner], "setRecord",
                args:
                [
                    new ContractParameter(ContractParameterType.String) { Value = "alias.neo" },
                    RecordTypeParam(RecordType.A),
                    new ContractParameter(ContractParameterType.String) { Value = "8.8.8.8" }
                ]);

            var resolved = CallWithWitness(snapshot, block, [], "resolve",
                args:
                [
                    new ContractParameter(ContractParameterType.String) { Value = "www.host.neo" },
                    RecordTypeParam(RecordType.A)
                ]);
            Assert.AreEqual("8.8.8.8", resolved.GetString());

            // deleteRecord
            CallWithWitness(snapshot, block, [owner], "deleteRecord",
                args:
                [
                    new ContractParameter(ContractParameterType.String) { Value = "host.neo" },
                    RecordTypeParam(RecordType.TXT)
                ]);
            CallWithWitness(snapshot, block, [owner], "setRecord",
                args:
                [
                    new ContractParameter(ContractParameterType.String) { Value = "host.neo" },
                    RecordTypeParam(RecordType.TXT),
                    new ContractParameter(ContractParameterType.String) { Value = "bye" }
                ]);
            CallWithWitness(snapshot, block, [owner], "deleteRecord",
                args:
                [
                    new ContractParameter(ContractParameterType.String) { Value = "host.neo" },
                    RecordTypeParam(RecordType.TXT)
                ]);
            var missing = CallWithWitness(snapshot, block, [], "getRecord",
                args:
                [
                    new ContractParameter(ContractParameterType.String) { Value = "host.neo" },
                    RecordTypeParam(RecordType.TXT)
                ]);
            Assert.IsTrue(missing.IsNull);

            // getAllRecords returns at least A and AAAA for host.neo
            var allRecords = CallAndDrain(snapshot, block, "getAllRecords",
                new ContractParameter(ContractParameterType.String) { Value = "host.neo" });
            Assert.IsGreaterThanOrEqualTo(2, allRecords.Count);
        }

        [TestMethod]
        public void SetRecord_InvalidTxtTooLong_Throws()
        {
            var snapshot = _snapshotCache.CloneCache();
            var owner = OwnerHash();
            var block = BlockAt(0, 20_000_000);
            RegisterName(snapshot, block, owner, "txt.neo");

            Assert.ThrowsExactly<FormatException>(() =>
                CallWithWitness(snapshot, block, [owner], "setRecord",
                    args:
                    [
                        new ContractParameter(ContractParameterType.String) { Value = "txt.neo" },
                        RecordTypeParam(RecordType.TXT),
                        new ContractParameter(ContractParameterType.String) { Value = new string('x', 256) }
                    ]));
        }

        [TestMethod]
        public void Roots_IncludesNeo_AndTokensOf()
        {
            var snapshot = _snapshotCache.CloneCache();
            var owner = OwnerHash();
            var block = BlockAt(0, 15_000_000);

            var rootValues = CallAndDrain(snapshot, block, "roots");
            Assert.IsTrue(rootValues.Any(v => v.GetSpan().ToArray().AsSpan().SequenceEqual(Utility.StrictUTF8.GetBytes("neo"))
                || v.GetString() == "neo"));

            RegisterName(snapshot, block, owner, "tok.neo");
            var tokenIds = CallAndDrain(snapshot, block, "tokensOf",
                new ContractParameter(ContractParameterType.Hash160) { Value = owner });
            Assert.IsTrue(tokenIds.Any(v =>
                Utility.StrictUTF8.GetString(v.GetSpan()) == "tok.neo"));

            var tokens = CallAndDrain(snapshot, block, "tokens");
            Assert.IsGreaterThanOrEqualTo(1, tokens.Count);

            Assert.AreEqual(1, (int)NativeContract.NameService.Call(snapshot, null, block, "totalSupply").GetInteger());
        }

        [TestMethod]
        public void Register_AfterExpiry_AllowsReregister()
        {
            var snapshot = _snapshotCache.CloneCache();
            var owner = OwnerHash();
            var other = Contract.CreateSignatureRedeemScript(TestProtocolSettings.Default.StandbyCommittee[1]).ToScriptHash();
            var registerAt = 1_000_000ul;
            var oneYear = 365ul * (ulong)TimeSpan.MillisecondsPerDay;
            var registerBlock = BlockAt(0, registerAt);
            var expiredBlock = BlockAt(1, registerAt + oneYear);
            // First label length >= 3 so default open price applies (length 1/2 are closed).
            const string name = "rereg.neo";

            RegisterName(snapshot, registerBlock, owner, name);
            Assert.IsFalse(NativeContract.NameService.Call(snapshot, null, registerBlock, "isAvailable",
                new ContractParameter(ContractParameterType.String) { Value = name }).GetBoolean());
            Assert.IsTrue(NativeContract.NameService.Call(snapshot, null, expiredBlock, "isAvailable",
                new ContractParameter(ContractParameterType.String) { Value = name }).GetBoolean());

            var ok = CallWithWitness(snapshot, expiredBlock, [other], "register",
                args:
                [
                    new ContractParameter(ContractParameterType.String) { Value = name },
                    new ContractParameter(ContractParameterType.Hash160) { Value = other }
                ]);
            Assert.IsTrue(ok.GetBoolean());
            AssertOwner(snapshot, expiredBlock, Utility.StrictUTF8.GetBytes(name), other);
        }

        #endregion

        #region Migration success / legacy allowlist

        [TestMethod]
        public void RemoveLegacyContract_RoundTrip()
        {
            var snapshot = _snapshotCache.CloneCache();
            var committee = NativeContract.NEO.GetCommitteeAddress(snapshot);
            var legacy = UInt160.Parse("0xaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa");
            var block = _persistingBlock;

            CallWithWitness(snapshot, block, [committee], "addLegacyContract",
                args: new ContractParameter(ContractParameterType.Hash160) { Value = legacy });
            Assert.IsTrue(NativeContract.NameService.Call(snapshot, "isLegacyContract",
                new ContractParameter(ContractParameterType.Hash160) { Value = legacy }).GetBoolean());

            CallWithWitness(snapshot, block, [committee], "removeLegacyContract",
                args: new ContractParameter(ContractParameterType.Hash160) { Value = legacy });
            Assert.IsFalse(NativeContract.NameService.Call(snapshot, "isLegacyContract",
                new ContractParameter(ContractParameterType.Hash160) { Value = legacy }).GetBoolean());
        }

        [TestMethod]
        public void OnNEP11Payment_FromLegacy_MintsName()
        {
            var snapshot = _snapshotCache.CloneCache();
            var committee = NativeContract.NEO.GetCommitteeAddress(snapshot);
            var legacy = UInt160.Parse("0x1111111111111111111111111111111111111111");
            var from = OwnerHash();
            var block = BlockAt(0, 50_000_000);
            var tokenId = Utility.StrictUTF8.GetBytes("mig.neo");

            CallWithWitness(snapshot, block, [committee], "addLegacyContract",
                args: new ContractParameter(ContractParameterType.Hash160) { Value = legacy });

            CallWithCallingScript(snapshot, block, legacy, [from], "onNEP11Payment",
                new ContractParameter(ContractParameterType.Hash160) { Value = from },
                new ContractParameter(ContractParameterType.Integer) { Value = (BigInteger)1 },
                new ContractParameter(ContractParameterType.ByteArray) { Value = tokenId },
                new ContractParameter(ContractParameterType.Any) { Value = null });

            AssertOwner(snapshot, block, tokenId, from);
            Assert.AreEqual(1, (int)NativeContract.NameService.Call(snapshot, null, block, "balanceOf",
                new ContractParameter(ContractParameterType.Hash160) { Value = from }).GetInteger());

            // Live native name: second migrate throws
            Assert.ThrowsExactly<InvalidOperationException>(() =>
                CallWithCallingScript(snapshot, block, legacy, [from], "onNEP11Payment",
                    new ContractParameter(ContractParameterType.Hash160) { Value = from },
                    new ContractParameter(ContractParameterType.Integer) { Value = (BigInteger)1 },
                    new ContractParameter(ContractParameterType.ByteArray) { Value = tokenId },
                    new ContractParameter(ContractParameterType.Any) { Value = null }));
        }

        [TestMethod]
        public void OnNEP11Payment_ReclaimsExpiredNativeName()
        {
            var snapshot = _snapshotCache.CloneCache();
            var committee = NativeContract.NEO.GetCommitteeAddress(snapshot);
            var legacy = UInt160.Parse("0x2222222222222222222222222222222222222222");
            var owner = OwnerHash();
            var migrator = Contract.CreateSignatureRedeemScript(TestProtocolSettings.Default.StandbyCommittee[1]).ToScriptHash();
            var registerAt = 1_000_000ul;
            var oneYear = 365ul * (ulong)TimeSpan.MillisecondsPerDay;
            var registerBlock = BlockAt(0, registerAt);
            var expiredBlock = BlockAt(1, registerAt + oneYear);
            var tokenId = Utility.StrictUTF8.GetBytes("take.neo");

            RegisterName(snapshot, registerBlock, owner, "take.neo");
            CallWithWitness(snapshot, registerBlock, [owner], "setRecord",
                args:
                [
                    new ContractParameter(ContractParameterType.String) { Value = "take.neo" },
                    RecordTypeParam(RecordType.TXT),
                    new ContractParameter(ContractParameterType.String) { Value = "clear-me" }
                ]);

            CallWithWitness(snapshot, expiredBlock, [committee], "addLegacyContract",
                args: new ContractParameter(ContractParameterType.Hash160) { Value = legacy });

            CallWithCallingScript(snapshot, expiredBlock, legacy, [migrator], "onNEP11Payment",
                new ContractParameter(ContractParameterType.Hash160) { Value = migrator },
                new ContractParameter(ContractParameterType.Integer) { Value = (BigInteger)1 },
                new ContractParameter(ContractParameterType.ByteArray) { Value = tokenId },
                new ContractParameter(ContractParameterType.Any) { Value = null });

            AssertOwner(snapshot, expiredBlock, tokenId, migrator);
            // Records cleared on reclaim
            var rec = CallWithWitness(snapshot, expiredBlock, [], "getRecord",
                args:
                [
                    new ContractParameter(ContractParameterType.String) { Value = "take.neo" },
                    RecordTypeParam(RecordType.TXT)
                ]);
            Assert.IsTrue(rec.IsNull);
        }

        [TestMethod]
        public void RecordState_RoundTrip()
        {
            var state = new RecordState
            {
                Name = "x.neo",
                Type = RecordType.A,
                Data = "1.2.3.4"
            };
            var restored = new RecordState();
            restored.FromStackItem(state.ToStackItem());
            Assert.AreEqual(state.Name, restored.Name);
            Assert.AreEqual(state.Type, restored.Type);
            Assert.AreEqual(state.Data, restored.Data);
        }

        [TestMethod]
        public void Nep11TokenState_RoundTrip()
        {
            var state = new Nep11TokenState
            {
                Owner = UInt160.Parse("0xcccccccccccccccccccccccccccccccccccccccc"),
                Name = "nft"
            };
            var restored = new Nep11TokenState();
            restored.FromStackItem(state.ToStackItem());
            Assert.AreEqual(state.Owner, restored.Owner);
            Assert.AreEqual(state.Name, restored.Name);
        }

        #endregion
    }
}
