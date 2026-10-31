// Copyright (C) 2015-2026 The Neo Project.
//
// UT_ApplicationEngine_Contract_Coverage.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Cryptography.ECC;
using Neo.Extensions;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.VM;
using System;
using Array = Neo.VM.Types.Array;

namespace Neo.UnitTests.SmartContract
{
    [TestClass]
    public class UT_ApplicationEngine_Contract_Coverage
    {
        private DataCache _snapshot;

        [TestInitialize]
        public void Setup()
        {
            _snapshot = TestBlockchain.GetTestSnapshotCache().CloneCache();
        }

        [TestMethod]
        public void CallContract_UnderscoreMethod_Throws()
        {
            using var engine = TestEngineRunner.CreateWithScript(_snapshot, new byte[] { (byte)OpCode.NOP });
            Assert.ThrowsExactly<ArgumentException>(() =>
                engine.CallContract(NativeContract.NEO.Hash, "_hidden", CallFlags.All, new Array()));
        }

        [TestMethod]
        public void CallContract_InvalidCallFlags_Throws()
        {
            using var engine = TestEngineRunner.CreateWithScript(_snapshot, new byte[] { (byte)OpCode.NOP });
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
                engine.CallContract(NativeContract.NEO.Hash, "symbol", (CallFlags)0xFF, new Array()));
        }

        [TestMethod]
        public void CallContract_MissingContract_Throws()
        {
            using var engine = TestEngineRunner.CreateWithScript(_snapshot, new byte[] { (byte)OpCode.NOP });
            Assert.ThrowsExactly<InvalidOperationException>(() =>
                engine.CallContract(UInt160.Zero, "symbol", CallFlags.ReadOnly, new Array()));
        }

        [TestMethod]
        public void CallContract_MissingMethod_Throws()
        {
            using var engine = TestEngineRunner.CreateWithScript(_snapshot, new byte[] { (byte)OpCode.NOP });
            Assert.ThrowsExactly<InvalidOperationException>(() =>
                engine.CallContract(NativeContract.NEO.Hash, "methodThatDoesNotExist", CallFlags.ReadOnly, new Array()));
        }

        [TestMethod]
        public void CallNativeContract_FromNonNative_Throws()
        {
            using var engine = TestEngineRunner.CreateWithScript(_snapshot, new byte[] { (byte)OpCode.NOP });
            Assert.ThrowsExactly<InvalidOperationException>(() => engine.CallNativeContract(0));
        }

        [TestMethod]
        public void CreateStandardAccount_MatchesContractHelper()
        {
            using var engine = TestEngineRunner.CreateWithScript(_snapshot, new byte[] { (byte)OpCode.NOP }, gas: 100_0000_0000);
            var pub = TestProtocolSettings.Default.StandbyCommittee[0];
            var hash = engine.CreateStandardAccount(pub);
            Assert.AreEqual(Contract.CreateSignatureRedeemScript(pub).ToScriptHash(), hash);
        }

        [TestMethod]
        public void CreateMultisigAccount_MatchesContractHelper()
        {
            using var engine = TestEngineRunner.CreateWithScript(_snapshot, new byte[] { (byte)OpCode.NOP }, gas: 100_0000_0000);
            ECPoint[] keys =
            [
                TestProtocolSettings.Default.StandbyCommittee[0],
                TestProtocolSettings.Default.StandbyCommittee[1],
                TestProtocolSettings.Default.StandbyCommittee[2]
            ];
            var hash = engine.CreateMultisigAccount(2, keys);
            Assert.AreEqual(Contract.CreateMultiSigRedeemScript(2, keys).ToScriptHash(), hash);
        }

        [TestMethod]
        public void NativeOnPersist_WrongTrigger_Faults()
        {
            using var engine = TestEngineRunner.Create(_snapshot, trigger: TriggerType.Application);
            engine.LoadScript(new byte[] { (byte)OpCode.NOP });
            engine.NativeOnPersistAsync();
            // async fault is queued; execute to observe FAULT
            Assert.AreEqual(VMState.FAULT, engine.Execute());
        }
    }
}
