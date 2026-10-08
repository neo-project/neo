// Copyright (C) 2015-2026 The Neo Project.
//
// AccountManagement.Execution.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

#pragma warning disable IDE0051
using Neo.Extensions;
using Neo.IO;
using Neo.Network.P2P.Payloads;
using Neo.VM.Types;
using System;
using System.Linq;
using System.Numerics;
using Array = Neo.VM.Types.Array;
using Boolean = Neo.VM.Types.Boolean;

namespace Neo.SmartContract.Native
{
    partial class AccountManagement
    {
        private static byte[] SnapshotOperation(Array operation)
        {
            _ = SmartAccountProtocol.SerializeUnsignedOperation(operation);
            return BinarySerializer.Serialize(operation, 8192, 8192);
        }
        private static Array ReadOperation(ReadOnlyMemory<byte> bytes)
        {
            MemoryReader reader = new(bytes);
            return (Array)BinarySerializer.Deserialize(ref reader, 8192, 8192);
        }
        private static void RequireLive(SmartAccountState state, Array operation, ulong now)
        {
            if (state.Status != SmartAccountStatus.Active || operation[4].GetInteger() < now)
                throw new InvalidOperationException("The account is Frozen or the operation has expired.");
        }
        private async ContractTask<StackItem> ModuleCall(ApplicationEngine engine, SmartAccountState state,
            SmartAccountModuleKind kind, SmartAccountModuleBinding module, string method, SmartAccountCallbackPhase phase,
            CallFlags flags, long budget, bool inheritBudget, params StackItem[] args)
        {
            _ = Inspect(engine, module.Contract, kind, module);
            var children = phase is SmartAccountCallbackPhase.Validation or SmartAccountCallbackPhase.PreExecute or SmartAccountCallbackPhase.PostExecute
                ? ActiveChildren(engine, state, kind) : System.Array.Empty<UInt160>();
            var task = inheritBudget
                ? engine.CallFromNativeContractRawAsync(Hash, module.Contract, method, flags, true, args)
                : engine.CallFromNativeContractWithGasLimitAsync(Hash, module.Contract, method, flags, budget, args);
            var anchor = engine.CurrentContext!.GetState<ExecutionContextState>();
            using var grant = Context(engine).EnterModule(state.AccountId, kind, module.Contract, phase, anchor, children);
            return (await task)!;
        }
        private async ContractTask Authorize(ApplicationEngine engine, SmartAccountState state, byte[] bytes)
        {
            Array operation = ReadOperation(bytes);
            if (state.Verifier is null)
            {
                if (operation[5].GetSpan().Length != 0 || !Witness(engine, state.CustodyAddress))
                    throw new InvalidOperationException("The native fallback requires an empty signature and the custody witness.");
            }
            else
            {
                var result = await ModuleCall(engine, state, SmartAccountModuleKind.Verifier, state.Verifier,
                    "validateSignature", SmartAccountCallbackPhase.Validation, CallFlags.ReadOnly, 100_000_000, false,
                    state.AccountId.ToArray(), operation);
                if (result is not Boolean boolean || !boolean.GetBoolean())
                    throw new InvalidOperationException("The verifier must return exactly Boolean true.");
            }
        }
        private async ContractTask<StackItem> ExecuteOne(ApplicationEngine engine, UInt160 id, byte[] bytes)
        {
            var state = RequireState(engine.SnapshotCache, id); Array operation = ReadOperation(bytes);
            RequireLive(state, operation, Now(engine));
            var nonce = operation[3].GetInteger(); var (channel, _) = SmartAccountProtocol.GetNonceParts(nonce);
            BigInteger next = SmartAccountProtocol.ConsumeNonce(nonce, GetNonce(engine.SnapshotCache, id, channel));
            if (state.Hook is not null) _ = Inspect(engine, state.Hook.Contract, SmartAccountModuleKind.Hook, state.Hook);
            await Authorize(engine, state, bytes);
            Put(engine, NonceKey(id, channel), next.ToByteArray());
            if (state.Hook is not null)
                _ = await ModuleCall(engine, state, SmartAccountModuleKind.Hook, state.Hook, "preExecute",
                    SmartAccountCallbackPhase.PreExecute, CallFlags.All, 250_000_000, false, id.ToArray(), ReadOperation(bytes));
            operation = ReadOperation(bytes);
            var target = new UInt160(operation[0].GetSpan()); string method = operation[1].GetString()!;
            var targetTask = engine.CallFromNativeContractRawAsync(Hash, target, method, CallFlags.All, false, ((Array)operation[2]).ToArray());
            StackItem result;
            using (Context(engine).EnterTarget(id, target, engine.CurrentContext!.GetState<ExecutionContextState>()))
                result = (await targetTask)!;
            if (state.Hook is not null)
                _ = await ModuleCall(engine, state, SmartAccountModuleKind.Hook, state.Hook, "postExecute",
                    SmartAccountCallbackPhase.PostExecute, CallFlags.All, 250_000_000, false, id.ToArray(), ReadOperation(bytes), result);
            if (state.Verifier is not null)
                _ = await ModuleCall(engine, state, SmartAccountModuleKind.Verifier, state.Verifier, "postExecute",
                    SmartAccountCallbackPhase.PostExecute, CallFlags.All, 100_000_000, false, id.ToArray(), ReadOperation(bytes), result);
            Notify(engine, "UserOpExecuted", id.ToArray(), target.ToArray(), method, nonce);
            return result;
        }
        [ContractMethod(Hardfork.HF_SmartAccountV1, CpuFee = BaseCpuFee, RequiredCallFlags = CallFlags.All)]
        private async ContractTask<StackItem> ExecuteUserOp(ApplicationEngine engine, UInt160 accountId, Array op)
        {
            RequireApplication(engine); using var locked = Context(engine).EnterAccount(accountId);
            byte[] bytes = SnapshotOperation(op);
            return (await ExecuteOne(engine, accountId, bytes))!;
        }
        [ContractMethod(Hardfork.HF_SmartAccountV1, CpuFee = BaseCpuFee, RequiredCallFlags = CallFlags.All)]
        private async ContractTask<Array> ExecuteUserOps(ApplicationEngine engine, UInt160 accountId, Array ops)
        {
            RequireApplication(engine); using var locked = Context(engine).EnterAccount(accountId);
            if (ops.Type != StackItemType.Array || ops.Count is 0 or > SmartAccountEnvelope.BatchMax)
                throw new FormatException("A batch must be an exact non-empty Array of at most 32 operations.");
            var owned = ops.Select(p => p is Array op ? SnapshotOperation(op) : throw new FormatException("A batch entry must be an operation Array.")).ToArray();
            Array results = new();
            foreach (var bytes in owned) results.Add((await ExecuteOne(engine, accountId, bytes))!);
            return results;
        }
        [ContractMethod(Hardfork.HF_SmartAccountV1, CpuFee = BaseCpuFee, RequiredCallFlags = CallFlags.ReadOnly)]
        private async ContractTask<bool> Verify(ApplicationEngine engine, UInt160 accountId)
        {
            if (engine.Trigger != TriggerType.Verification || engine.ScriptContainer is not Transaction tx)
                throw new InvalidOperationException("SmartAccount verification requires a transaction Verification trigger.");
            var state = RequireState(engine.SnapshotCache, accountId);
            if (engine.CallingScriptHash != state.AccountAddress)
                throw new InvalidOperationException("Verification must originate from the account's exact proxy script.");
            using var locked = Context(engine).EnterAccount(accountId);
            var envelope = SmartAccountEnvelope.Parse(accountId, tx.Script.Span);
            envelope.ValidateNonces(channel => GetNonce(engine.SnapshotCache, accountId, channel));
            for (int i = 0; i < envelope.Count; i++)
            {
                var operation = envelope.GetOperation(i); RequireLive(state, operation, Now(engine));
                if (state.Hook is not null) _ = Inspect(engine, state.Hook.Contract, SmartAccountModuleKind.Hook, state.Hook);
                await Authorize(engine, state, SnapshotOperation(operation));
            }
            return true;
        }
    }
}
