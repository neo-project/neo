// Copyright (C) 2015-2026 The Neo Project.
//
// UT_AccountManagement.cs file belongs to the neo project and is free
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
using System;
using System.Linq;
using System.Numerics;
using Array = Neo.VM.Types.Array;

namespace Neo.UnitTests.SmartContract.Native
{
    [TestClass]
    public partial class UT_AccountManagement
    {
        private static readonly UInt160 Custody = UInt160.Parse("0x0101010101010101010101010101010101010101");
        private static readonly UInt160 Recovery = UInt160.Parse("0x0202020202020202020202020202020202020202");
        private static readonly UInt160 Next = UInt160.Parse("0x0303030303030303030303030303030303030303");
        private static ProtocolSettings Settings => TestProtocolSettings.Default with
        {
            Hardforks = TestProtocolSettings.Default.Hardforks.SetItem(Hardfork.HF_SmartAccountV1, 0)
        };
        private static DataCache Snapshot()
        {
            var snapshot = TestBlockchain.GetTestSnapshotCache();
            snapshot.AddContract(NativeContract.AccountManagement.Hash, NativeContract.AccountManagement.GetContractState(Settings, 1));
            return snapshot;
        }
        private static Block Block(ulong time) => new()
        {
            Header = new Header
            {
                Index = 1,
                Timestamp = time,
                PrevHash = UInt256.Zero,
                MerkleRoot = UInt256.Zero,
                NextConsensus = UInt160.Zero,
                Witness = new Witness { InvocationScript = ReadOnlyMemory<byte>.Empty, VerificationScript = ReadOnlyMemory<byte>.Empty }
            },
            Transactions = []
        };
        private static void Push(ScriptBuilder builder, object value)
        {
            if (value is object[] items)
            {
                if (items.Length == 0) builder.Emit(OpCode.NEWARRAY0);
                else { for (int i = items.Length - 1; i >= 0; i--) Push(builder, items[i]); builder.EmitPush(items.Length).Emit(OpCode.PACK); }
            }
            else builder.EmitPush(value);
        }
        private static ApplicationEngine Invoke(DataCache snapshot, string method, object[] args, ulong time = 1000,
            UInt160[] signers = null, TriggerType trigger = TriggerType.Application, UInt160 target = null, Signer[] signerDetails = null)
        {
            using var builder = new ScriptBuilder();
            Push(builder, args);
            builder.EmitPush(CallFlags.All).EmitPush(method).EmitPush(target ?? NativeContract.AccountManagement.Hash).EmitSysCall(ApplicationEngine.System_Contract_Call);
            var tx = new Transaction { Version = 0, Script = builder.ToArray(), Signers = signerDetails ?? (signers ?? [Custody]).Select(a => new Signer { Account = a, Scopes = WitnessScope.Global }).ToArray(), Attributes = [], Witnesses = [] };
            var engine = ApplicationEngine.Create(trigger, tx, snapshot, Block(time), Settings, gas: 10_000_000_000);
            engine.LoadScript(tx.Script); engine.Execute(); return engine;
        }
        private static StackItem Success(DataCache snapshot, string method, object[] args, ulong time = 1000, UInt160[] signers = null)
        {
            using var engine = Invoke(snapshot, method, args, time, signers);
            Assert.AreEqual(VMState.HALT, engine.State, engine.FaultException?.ToString());
            var result = engine.ResultStack.Pop(); engine.SnapshotCache.Commit(); return result;
        }
        private static UInt160 Register(DataCache snapshot, UInt160 recovery = null) => new(Success(snapshot,
            "registerAccount", [Custody, new byte[32], UInt160.Zero, UInt160.Zero, recovery ?? UInt160.Zero]).GetSpan());

        [TestMethod]
        public void NativeIdentityActivationAndRequiredAbiAreStable()
        {
            var native = NativeContract.AccountManagement;
            Assert.AreEqual(SmartAccountProtocol.ServiceHash, native.Hash);
            Assert.AreEqual(NativeContract.TemporaryStorage.Id - 1, native.Id);
            Assert.IsFalse(native.IsActive(TestProtocolSettings.Default, uint.MaxValue));
            var disabled = native.GetContractState(TestProtocolSettings.Default, 1);
            Assert.AreEqual(0, disabled.Manifest.Abi.Methods.Length);
            Assert.AreEqual(0, disabled.Manifest.Abi.Events.Length);
            var state = native.GetContractState(Settings, 1);
            Assert.AreEqual(6, state.Manifest.Abi.Events.Single(e => e.Name == "AccountCreated").Parameters.Length);
            foreach (string method in new[] { "registerAccount", "verify", "executeUserOp", "executeUserOps", "getVersion", "getAccount", "getNonce",
                "callVerifier", "callHook", "callVerifierChild", "callHookChild", "cancelModuleCall", "getPendingModuleCall", "getModuleDependencies",
                "setVerifierDependencies", "setHookDependencies", "clearVerifierDependencies", "clearHookDependencies", "freeze", "unfreeze", "executeRecovery" })
                Assert.IsTrue(state.Manifest.Abi.Methods.Any(m => m.Name == method), method);
            Assert.AreEqual(1, state.Manifest.Extra["smartAccount"]["abiVersion"].GetInt32());
        }

        [TestMethod]
        public void RegistersActualNativeStateAndRejectsDuplicatesOrMissingWitness()
        {
            var snapshot = Snapshot(); var id = Register(snapshot, Recovery);
            Assert.AreEqual(SmartAccountProtocol.GetAccountId(Settings.Network, Custody, new byte[32]), id);
            var state = (Array)Success(snapshot, "getAccount", [id]);
            Assert.AreEqual(13, state.Count); Assert.AreEqual(BigInteger.Zero, state[8].GetInteger());
            Assert.AreSequenceEqual(SmartAccountProtocol.GetAccountAddress(id).ToArray(), Success(snapshot, "getAccountAddress", [id]).GetSpan().ToArray());
            Assert.AreEqual(BigInteger.Zero, Success(snapshot, "getNonce", [id, BigInteger.Zero]).GetInteger());
            using var duplicate = Invoke(snapshot, "registerAccount", [Custody, new byte[32], UInt160.Zero, UInt160.Zero, Recovery]);
            Assert.AreEqual(VMState.FAULT, duplicate.State);
            using var missing = Invoke(snapshot, "registerAccount", [Next, new byte[32], UInt160.Zero, UInt160.Zero, UInt160.Zero]);
            Assert.AreEqual(VMState.FAULT, missing.State);
        }

        private static object[] Operation(UInt160 target, string method, object[] args, long nonce = 0, long deadline = long.MaxValue) =>
            [target, method, args, new BigInteger(nonce), new BigInteger(deadline), System.Array.Empty<byte>()];

        [TestMethod]
        public void NativeServiceIdentityNeverBecomesAUserOperationWitness()
        {
            var snapshot = Snapshot(); var id = Register(snapshot);
            using var script = new ScriptBuilder();
            script.EmitSysCall(ApplicationEngine.System_Runtime_CheckWitness).Emit(OpCode.RET);
            var target = TestUtils.GetContract(script.ToArray(), TestUtils.CreateManifest("check", ContractParameterType.Boolean, ContractParameterType.Hash160));
            snapshot.AddContract(target.Hash, target);
            var result = Success(snapshot, "executeUserOp", [id, Operation(target.Hash, "check", [NativeContract.AccountManagement.Hash])]);
            Assert.IsFalse(result.GetBoolean(), "The permissionless native dispatcher is not an authorization identity.");
            using var invalidGuardian = Invoke(snapshot, "proposeRecoveryAddress", [id, NativeContract.AccountManagement.Hash]);
            Assert.AreEqual(VMState.FAULT, invalidGuardian.State);
        }

        [TestMethod]
        public void ServiceWitnessRejectionIsHardforkGated()
        {
            foreach (bool active in new[] { false, true })
                foreach (var trigger in new[] { TriggerType.Application, TriggerType.Verification })
                {
                    var settings = active ? Settings : TestProtocolSettings.Default;
                    var tx = new Transaction { Signers = [new Signer { Account = NativeContract.AccountManagement.Hash, Scopes = WitnessScope.Global }], Attributes = [], Witnesses = [] };
                    using var engine = ApplicationEngine.Create(trigger, tx, Snapshot(), Block(1000), settings);
                    engine.LoadScript(new byte[] { (byte)OpCode.RET }, configureState: state => state.NativeCallingScriptHash = NativeContract.AccountManagement.Hash);
                    Assert.AreEqual(!active, engine.CheckWitnessInternal(NativeContract.AccountManagement.Hash));
                }
        }

        [TestMethod]
        public void NativeExecutionConsumesNoncesAndBatchFailureRollsBack()
        {
            var snapshot = Snapshot(); var id = Register(snapshot);
            var op = Operation(NativeContract.StdLib.Hash, "serialize", [7]);
            Assert.IsInstanceOfType<ByteString>(Success(snapshot, "executeUserOp", [id, op]));
            Assert.AreEqual(BigInteger.One, Success(snapshot, "getNonce", [id, BigInteger.Zero]).GetInteger());
            using (var replay = Invoke(snapshot, "executeUserOp", [id, op])) Assert.AreEqual(VMState.FAULT, replay.State);
            using (var expired = Invoke(snapshot, "executeUserOp", [id, Operation(NativeContract.StdLib.Hash, "serialize", [7], 1, 999)])) Assert.AreEqual(VMState.FAULT, expired.State);
            var batch = new object[] { Operation(NativeContract.StdLib.Hash, "serialize", [7], 1), Operation(NativeContract.StdLib.Hash, "serialize", [8], 1) };
            using (var failed = Invoke(snapshot, "executeUserOps", [id, batch])) Assert.AreEqual(VMState.FAULT, failed.State);
            Assert.AreEqual(BigInteger.One, Success(snapshot, "getNonce", [id, BigInteger.Zero]).GetInteger());
            var result = (Array)Success(snapshot, "executeUserOps", [id, new object[] {
                Operation(NativeContract.StdLib.Hash, "serialize", [7], 1), Operation(NativeContract.StdLib.Hash, "serialize", [8], 2) }]);
            Assert.AreEqual(2, result.Count);
            Assert.AreEqual(new BigInteger(3), Success(snapshot, "getNonce", [id, BigInteger.Zero]).GetInteger());
        }

        [TestMethod]
        public void VerifyCannotBeCalledAsApplicationAndUnknownQueriesFault()
        {
            var snapshot = Snapshot(); var id = Register(snapshot);
            using (var verify = Invoke(snapshot, "verify", [id])) Assert.AreEqual(VMState.FAULT, verify.State);
            foreach (var (method, args) in new[] { ("getAccountAddress", new object[] { Next }), ("getNonce", new object[] { Next, 0 }),
                ("getOperationDigest", new object[] { Next, Operation(NativeContract.StdLib.Hash, "serialize", [7]) }) })
            {
                using var unknown = Invoke(snapshot, method, args); Assert.AreEqual(VMState.FAULT, unknown.State, method);
            }
            Assert.IsInstanceOfType<Null>(Success(snapshot, "getAccount", [Next]));
            Assert.IsInstanceOfType<ByteString>(Success(snapshot, "getAuthorizationDomain", [Next]));
        }

        [TestMethod]
        public void RecoveryRotationFreezeAndMaturityUseRealWitnessesAndStorage()
        {
            var snapshot = Snapshot(); var id = Register(snapshot, Recovery);
            Success(snapshot, "proposeRecoveryAddress", [id, Next]);
            using (var early = Invoke(snapshot, "activateRecoveryAddress", [id], 1001)) Assert.AreEqual(VMState.FAULT, early.State);
            Success(snapshot, "activateRecoveryAddress", [id], 1000 + SmartAccountState.ModuleChangeDelayMs, []);
            var state = (Array)Success(snapshot, "getAccount", [id]); Assert.AreEqual(BigInteger.One, state[8].GetInteger());
            Success(snapshot, "freeze", [id], signers: [Next]);
            using (var unauthorized = Invoke(snapshot, "unfreeze", [id])) Assert.AreEqual(VMState.FAULT, unauthorized.State);
            Success(snapshot, "unfreeze", [id], signers: [Custody, Next]);
            Success(snapshot, "proposeRecovery", [id, Recovery], signers: [Next]);
            using (var late = Invoke(snapshot, "cancelRecovery", [id], 1000 + SmartAccountState.CustodyRecoveryDelayMs)) Assert.AreEqual(VMState.FAULT, late.State);
            Success(snapshot, "executeRecovery", [id], 1000 + SmartAccountState.CustodyRecoveryDelayMs, []);
            state = (Array)Success(snapshot, "getAccount", [id]); Assert.AreSequenceEqual(Recovery.ToArray(), state[3].GetSpan().ToArray());
        }
    }
}
