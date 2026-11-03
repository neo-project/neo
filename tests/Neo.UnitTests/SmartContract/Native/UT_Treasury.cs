// Copyright (C) 2015-2026 The Neo Project.
//
// UT_Treasury.cs file belongs to the neo project and is free
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
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.UnitTests.Extensions;
using Neo.VM;
using System;
using System.Numerics;

namespace Neo.UnitTests.SmartContract.Native
{
    [TestClass]
    public class UT_Treasury
    {
        [TestMethod]
        [DataRow("onNEP17Payment", 99u, true)]
        [DataRow("onNEP17Payment", 100u, true)]
        [DataRow("onNEP17Payment", 99u, false)]
        [DataRow("onNEP17Payment", 100u, false)]
        [DataRow("onNEP11Payment", 99u, true)]
        [DataRow("onNEP11Payment", 100u, true)]
        [DataRow("onNEP11Payment", 99u, false)]
        [DataRow("onNEP11Payment", 100u, false)]
        public void PaymentCallbacks_AllowMintStartingAtHuyao(string method, uint index, bool mint)
        {
            using var snapshot = TestBlockchain.GetTestSnapshotCache();
            var settings = TestProtocolSettings.Default with
            {
                Hardforks = TestProtocolSettings.Default.Hardforks.SetItem(Hardfork.HF_Huyao, 100)
            };
            var parameterCount = method == "onNEP17Payment" ? 3 : 4;
            var descriptor = NativeContract.Treasury.GetContractState(settings, index).Manifest.Abi.GetMethod(method, parameterCount);
            Assert.IsNotNull(descriptor);
            Assert.AreEqual("from", descriptor.Parameters[0].Name);
            Assert.AreEqual(ContractParameterType.Hash160, descriptor.Parameters[0].Type);
            using var engine = ApplicationEngine.Create(TriggerType.Application, null, snapshot,
                CreateBlock(index), settings);
            using var script = new ScriptBuilder();
            object from = mint ? null : UInt160.Zero;
            if (method == "onNEP17Payment")
                script.EmitDynamicCall(NativeContract.Treasury.Hash, method, from, 1, null);
            else
                script.EmitDynamicCall(NativeContract.Treasury.Hash, method, from, 1, new byte[] { 1 }, null);
            engine.LoadScript(script.ToArray());
            Assert.AreEqual(mint && index < 100 ? VMState.FAULT : VMState.HALT, engine.Execute());
            if (mint && index < 100)
                Assert.Contains("The argument `from` can't be null.", engine.FaultException.Message);
        }

        [TestMethod]
        public void RecoverNeoAndWithdrawAfterTreasuryAccruesGas()
        {
            using var snapshot = TestBlockchain.GetTestSnapshotCache();
            var owner = Contract.GetBFTAddress(TestProtocolSettings.Default.StandbyValidators);
            var committee = NativeContract.NEO.GetCommitteeAddress(snapshot);
            var members = NativeContract.NEO.GetCommittee(snapshot);
            var threshold = Math.Max(1, members.Length - (members.Length - 1) / 2);
            var recoveryCommittee = Contract.CreateMultiSigRedeemScript(Math.Max(threshold, members.Length - 2), members).ToScriptHash();
            var blockedAccount = UInt160.Parse("0xa400ff00ff00ff00ff00ff00ff00ff00ff00ff01");
            var start = CreateBlock(10, 1_000_000);
            var finish = CreateBlock(2000, start.Timestamp + 365UL * 24 * 60 * 60 * 1000 + 1000);

            Assert.IsTrue(NativeContract.NEO.TransferWithTransaction(snapshot, owner.ToArray(), blockedAccount.ToArray(), 50, true, start, null));
            Assert.IsTrue(NativeContract.NEO.TransferWithTransaction(snapshot, owner.ToArray(), NativeContract.Treasury.Hash.ToArray(), 1, true, start, null));
            NativeContract.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(committee), start,
                "blockAccount", new ContractParameter(ContractParameterType.Hash160) { Value = blockedAccount });

            NativeContract.Policy.Call(snapshot, new Nep17NativeContractExtensions.ManualWitness(recoveryCommittee), finish,
                "recoverFund", new ContractParameter(ContractParameterType.Hash160) { Value = blockedAccount },
                new ContractParameter(ContractParameterType.Hash160) { Value = NativeContract.NEO.Hash });
            Assert.AreEqual(BigInteger.Zero, NativeContract.NEO.BalanceOf(snapshot, blockedAccount));
            Assert.AreEqual(new BigInteger(51), NativeContract.NEO.BalanceOf(snapshot, NativeContract.Treasury.Hash));
            Assert.IsTrue(NativeContract.GAS.BalanceOf(snapshot, NativeContract.Treasury.Hash) > 0);

            Assert.IsTrue(NativeContract.NEO.TransferWithTransaction(snapshot, NativeContract.Treasury.Hash.ToArray(), owner.ToArray(), 51, true,
                CreateBlock(2002, finish.Timestamp + 30_000), null));
            Assert.AreEqual(BigInteger.Zero, NativeContract.NEO.BalanceOf(snapshot, NativeContract.Treasury.Hash));
        }

        private static Block CreateBlock(uint index, ulong timestamp = 0) => new()
        {
            Header = new Header
            {
                Index = index,
                Timestamp = timestamp,
                PrevHash = UInt256.Zero,
                MerkleRoot = UInt256.Zero,
                NextConsensus = UInt160.Zero,
                Witness = Witness.Empty
            },
            Transactions = []
        };

        [TestMethod]
        public void Activations_RequireFaun()
        {
            Assert.IsTrue(NativeContract.Treasury.Activations.Contains(Hardfork.HF_Faun));
        }

        [TestMethod]
        public void Hash_And_Name_AreStable()
        {
            Assert.IsNotNull(NativeContract.Treasury.Hash);
            Assert.AreNotEqual(UInt160.Zero, NativeContract.Treasury.Hash);
            Assert.AreEqual(nameof(Treasury), NativeContract.Treasury.Name);
        }

        [TestMethod]
        public void IsNative_RecognizesTreasury()
        {
            Assert.IsTrue(NativeContract.IsNative(NativeContract.Treasury.Hash));
            Assert.AreSame(NativeContract.Treasury, NativeContract.GetContract(NativeContract.Treasury.Hash));
        }
    }
}
