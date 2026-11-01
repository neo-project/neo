// Copyright (C) 2015-2026 The Neo Project.
//
// UT_ContractManagement_Queries.cs file belongs to the neo project and is free
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
using Neo.SmartContract.Manifest;
using Neo.SmartContract.Native;
using Neo.UnitTests.Extensions;
using Neo.VM;
using Neo.VM.Types;
using System.Numerics;

namespace Neo.UnitTests.SmartContract.Native
{
    [TestClass]
    public class UT_ContractManagement_Queries
    {
        private DataCache _snapshot;

        [TestMethod]
        [DataRow(false, false, 100)]
        [DataRow(false, true, 100)]
        [DataRow(true, false, 100)]
        [DataRow(true, true, 100)]
        [DataRow(true, true, 0)]
        [DataRow(true, true, -1)]
        public void Destroy_DiscardsGasFromHuyao(bool huyao, bool abortCallback, int balance)
        {
            using var script = new ScriptBuilder();
            script.EmitDynamicCall(NativeContract.ContractManagement.Hash, "destroy");
            script.Emit(OpCode.DROP);
            script.Emit(OpCode.RET);
            var callbackOffset = script.Length;
            script.Emit(abortCallback ? OpCode.ABORT : OpCode.CLEAR);
            script.Emit(OpCode.RET);
            var manifest = TestUtils.CreateManifest("destroySelf", ContractParameterType.Void);
            manifest.Abi.Methods = [manifest.Abi.Methods[0], new ContractMethodDescriptor
            {
                Name = "onNEP17Payment",
                Offset = callbackOffset,
                ReturnType = ContractParameterType.Void,
                Parameters = [
                    new ContractParameterDefinition { Name = "from", Type = ContractParameterType.Hash160 },
                    new ContractParameterDefinition { Name = "amount", Type = ContractParameterType.Integer },
                    new ContractParameterDefinition { Name = "data", Type = ContractParameterType.Any }
                ]
            }];
            var contract = TestUtils.GetContract(script.ToArray(), manifest);
            _snapshot.AddContract(contract.Hash, contract);
            var candidate = NativeContract.NEO.GetCommittee(_snapshot)[0];
            var accountKey = NativeContract.NEO.CreateStorageKey(20, contract.Hash);
            var candidateKey = NativeContract.NEO.CreateStorageKey(33, candidate);
            if (balance >= 0)
                _snapshot.Add(accountKey, new StorageItem(new NeoToken.NeoAccountState
                {
                    Balance = balance,
                    BalanceHeight = 0,
                    VoteTo = balance > 0 ? candidate : null,
                    LastGasPerVote = 100_00000000L
                }));
            _snapshot.GetAndChange(candidateKey, () => new StorageItem(new NeoToken.CandidateState()))
                .FromReplica(new StorageItem(new NeoToken.CandidateState { Registered = true, Votes = balance > 0 ? balance : 0 }));
            if (balance > 0) _snapshot.GetAndChange(NativeContract.NEO.CreateStorageKey(1)).Add(balance);
            _snapshot.GetAndChange(NativeContract.NEO.CreateStorageKey(23, candidate), () => new StorageItem(BigInteger.Zero))
                .Set(new BigInteger(103_00000000L));
            var deferredKey = NativeContract.NEO.CreateStorageKey(24, contract.Hash);
            if (huyao) _snapshot.Add(deferredKey, new StorageItem(new BigInteger(123)));
            var settings = TestProtocolSettings.Default with
            {
                Hardforks = TestProtocolSettings.Default.Hardforks.SetItem(Hardfork.HF_Huyao, huyao ? 1000u : 1001u)
            };
            using var engine = ApplicationEngine.Create(TriggerType.Application, null, _snapshot,
                new Block
                {
                    Header = new Header
                    {
                        Index = 1000,
                        Timestamp = 1_000_000,
                        PrevHash = UInt256.Zero,
                        MerkleRoot = UInt256.Zero,
                        NextConsensus = UInt160.Zero,
                        Witness = Witness.Empty
                    },
                    Transactions = []
                }, settings);
            using var invocation = new ScriptBuilder();
            invocation.EmitDynamicCall(contract.Hash, "destroySelf");
            engine.LoadScript(invocation.ToArray());
            if (!huyao && abortCallback)
            {
                Assert.AreEqual(VMState.FAULT, engine.Execute());
                Assert.IsNotNull(NativeContract.ContractManagement.GetContract(_snapshot, contract.Hash));
                Assert.IsFalse(NativeContract.Policy.IsBlocked(_snapshot, contract.Hash));
                return;
            }
            Assert.AreEqual(VMState.HALT, engine.Execute(), engine.FaultException?.ToString());
            Assert.IsNull(NativeContract.ContractManagement.GetContract(_snapshot, contract.Hash));
            Assert.IsTrue(NativeContract.Policy.IsBlocked(_snapshot, contract.Hash));
            Assert.IsFalse(_snapshot.Contains(deferredKey));
            Assert.AreEqual(huyao, NativeContract.GAS.BalanceOf(_snapshot, contract.Hash).IsZero);
            Assert.AreEqual(BigInteger.Zero, _snapshot[candidateKey].GetInteroperable<NeoToken.CandidateState>().Votes);
            if (balance > 0)
            {
                var account = _snapshot[accountKey].GetInteroperable<NeoToken.NeoAccountState>();
                Assert.IsNull(account.VoteTo);
                Assert.AreEqual(1000u, account.BalanceHeight);
                Assert.AreEqual(BigInteger.Zero, NativeContract.NEO.UnclaimedGas(engine, contract.Hash, 1000));
            }
        }

        [TestInitialize]
        public void Setup()
        {
            _snapshot = TestBlockchain.GetTestSnapshotCache().CloneCache();
        }

        [TestMethod]
        public void GetContract_NativeNeo_Exists()
        {
            var ret = NativeContract.ContractManagement.Call(_snapshot, "getContract",
                new ContractParameter(ContractParameterType.Hash160) { Value = NativeContract.NEO.Hash });
            Assert.IsFalse(ret.IsNull);
        }

        [TestMethod]
        public void GetContract_Missing_ReturnsNull()
        {
            var ret = NativeContract.ContractManagement.Call(_snapshot, "getContract",
                new ContractParameter(ContractParameterType.Hash160) { Value = UInt160.Zero });
            Assert.IsTrue(ret.IsNull);
        }

        [TestMethod]
        public void GetMinimumDeploymentFee_IsPositive()
        {
            var ret = NativeContract.ContractManagement.Call(_snapshot, "getMinimumDeploymentFee");
            Assert.IsInstanceOfType<Integer>(ret);
            Assert.IsTrue(ret.GetInteger() > 0);
        }

        [TestMethod]
        public void HasMethod_NativeSymbol_True()
        {
            var ret = NativeContract.ContractManagement.Call(_snapshot, "hasMethod",
                new ContractParameter(ContractParameterType.Hash160) { Value = NativeContract.NEO.Hash },
                new ContractParameter(ContractParameterType.String) { Value = "symbol" },
                new ContractParameter(ContractParameterType.Integer) { Value = (BigInteger)0 });
            Assert.IsInstanceOfType<Boolean>(ret);
            Assert.IsTrue(ret.GetBoolean());
        }

        [TestMethod]
        public void HasMethod_MissingMethod_False()
        {
            var ret = NativeContract.ContractManagement.Call(_snapshot, "hasMethod",
                new ContractParameter(ContractParameterType.Hash160) { Value = NativeContract.NEO.Hash },
                new ContractParameter(ContractParameterType.String) { Value = "noSuchMethod" },
                new ContractParameter(ContractParameterType.Integer) { Value = (BigInteger)0 });
            Assert.IsFalse(ret.GetBoolean());
        }

        [TestMethod]
        public void GetContractById_NativeIds()
        {
            // Native contracts use negative ids; NEO is typically -5 or similar depending on order.
            var neo = NativeContract.ContractManagement.GetContract(_snapshot, NativeContract.NEO.Hash);
            Assert.IsNotNull(neo);
            var byId = NativeContract.ContractManagement.Call(_snapshot, "getContractById",
                new ContractParameter(ContractParameterType.Integer) { Value = neo.Id });
            Assert.IsFalse(byId.IsNull);
        }

        [TestMethod]
        public void GetContractId()
        {
            var iter = NativeContract.ContractManagement.GetContractHashes(_snapshot);
            while (iter.Next())
            {
                var hash = new UInt160(iter.Value().GetSpan());
                var cs = NativeContract.ContractManagement.GetContract(_snapshot, hash);
                var id = NativeContract.ContractManagement.GetContractId(_snapshot, hash);
                Assert.AreEqual(cs.Id, id);
            }
        }
    }
}
