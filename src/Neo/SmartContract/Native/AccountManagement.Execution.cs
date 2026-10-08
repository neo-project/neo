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
        private static byte[] SnapshotResult(ApplicationEngine engine, StackItem result)
        {
            // Charge the same CPU price and enforce the same data limits as StdLib.serialize.
            engine.AddNativeCpuFee(1 << 12);
            return BinarySerializer.Serialize(result, engine.Limits);
        }
        private static StackItem ReadResult(ApplicationEngine engine, byte[] snapshot)
        {
            engine.AddNativeCpuFee(1 << 14);
            // ByteString and Buffer backing memory must not alias the stored result snapshot.
            return BinarySerializer.Deserialize(snapshot.AsSpan().ToArray(), engine.Limits);
        }
        // Validate before BigInteger's ordinary interop conversion can coerce Boolean/ByteString.
        private sealed class ExecutionCounterAttribute : ValidatorAttribute
        {
            public override void Validate(StackItem item)
            {
                if (item is not Integer integer)
                    throw new FormatException("SmartAccount execution counters must be UInt64 Integers.");
                _ = SmartAccountEnvelope.ReadCounter(integer);
            }
        }
        private static void RequireExecutionCounters(SmartAccountState state, ulong expectedAuthorityEpoch, ulong expectedConfigurationNonce)
        {
            if (state.AuthorityEpoch != expectedAuthorityEpoch || state.ConfigurationNonce != expectedConfigurationNonce)
                throw new InvalidOperationException("The SmartAccount execution authority epoch or configuration nonce is stale.");
        }
        private static void RequireLive(SmartAccountState state, Array operation, ulong now)
        {
            if (state.Status != SmartAccountStatus.Active || operation[4].GetInteger() < now)
                throw new InvalidOperationException("The account is Frozen or the operation has expired.");
        }
        // Host-owned, bounded and operation-local. No VM graph is retained across callbacks.
        private sealed record CompositeApproval(UInt160[] Children, byte[] Commitment)
        {
            internal Array ToStackItem() => new([Boolean.True,
                new Array(Children.Select(child => (StackItem)child.ToArray())), new ByteString(Commitment.AsSpan().ToArray())]);
        }
        private static CompositeApproval ReadCompositeApproval(StackItem value, UInt160[] active)
        {
            if (value is not Array receipt || value.Type != StackItemType.Array || receipt.Count != 3 ||
                receipt[0] is not Boolean approved || !approved.GetBoolean() ||
                receipt[1] is not Array children || receipt[1].Type != StackItemType.Array || children.Count is 0 or > 2 ||
                receipt[2] is not ByteString commitment || commitment.Size != UInt256.Length)
                throw new InvalidOperationException("Invalid composite receipt shape, approval or commitment.");
            UInt160[] owned = new UInt160[children.Count];
            int next = 0;
            for (int i = 0; i < children.Count; i++)
            {
                if (children[i] is not ByteString bytes || bytes.Size != UInt160.Length)
                    throw new InvalidOperationException("Invalid composite receipt child identity.");
                var child = new UInt160(bytes.GetSpan());
                while (next < active.Length && active[next] != child) next++;
                if (next == active.Length) throw new InvalidOperationException("Invalid composite receipt child order or membership.");
                owned[i] = child; next++;
            }
            return new(owned, commitment.GetSpan().ToArray());
        }
        private ContractTask<StackItem> ModuleCall(ApplicationEngine engine, SmartAccountState state,
            SmartAccountModuleKind kind, SmartAccountModuleBinding module, string method, SmartAccountCallbackPhase phase,
            CallFlags flags, long budget, bool inheritBudget, params StackItem[] args) =>
            ModuleCallWithApproval(engine, state, kind, module, method, phase, flags, budget, inheritBudget, null, args);

        private async ContractTask<StackItem> ModuleCallWithApproval(ApplicationEngine engine, SmartAccountState state,
            SmartAccountModuleKind kind, SmartAccountModuleBinding module, string method, SmartAccountCallbackPhase phase,
            CallFlags flags, long budget, bool inheritBudget, CompositeApproval? approval, params StackItem[] args)
        {
            _ = Inspect(engine, module.Contract, kind, module);
            var children = phase is SmartAccountCallbackPhase.Validation or SmartAccountCallbackPhase.PreExecute or SmartAccountCallbackPhase.PostExecute
                ? ActiveChildren(engine, state, kind) : System.Array.Empty<UInt160>();
            if (kind == SmartAccountModuleKind.Verifier && children.Length != 0 &&
                !SmartAccountModulePolicy.IsCompositeVerifier(engine.SnapshotCache, module.Contract))
                throw new InvalidOperationException("A scalar verifier cannot grant composite child authority.");
            if (approval is not null)
            {
                // Check all active pins above, including children outside the approved subset.
                // Reparse our owned receipt against the current roster before granting any child.
                children = ReadCompositeApproval(approval.ToStackItem(), children).Children;
            }
            var task = inheritBudget
                ? engine.CallFromNativeContractRawAsync(Hash, module.Contract, method, flags, true, args)
                : engine.CallFromNativeContractWithGasLimitAsync(Hash, module.Contract, method, flags, budget, args);
            var anchor = engine.CurrentContext!.GetState<ExecutionContextState>();
            StackItem result;
            using (Context(engine).EnterModule(state.AccountId, kind, module.Contract, phase, anchor, children))
                result = (await task)!;
            if (phase == SmartAccountCallbackPhase.PostExecute)
            {
                _ = Inspect(engine, module.Contract, kind, module);
                _ = ActiveChildren(engine, state, kind);
            }
            return result;
        }
        private async ContractTask<CompositeApproval?> Authorize(ApplicationEngine engine, SmartAccountState state, byte[] bytes)
        {
            Array operation = ReadOperation(bytes);
            if (state.Verifier is null)
            {
                if (operation[5].GetSpan().Length != 0 || !Witness(engine, state.CustodyAddress))
                    throw new InvalidOperationException("The native fallback requires an empty signature and the custody witness.");
            }
            else
            {
                _ = Inspect(engine, state.Verifier.Contract, SmartAccountModuleKind.Verifier, state.Verifier);
                bool composite = SmartAccountModulePolicy.IsCompositeVerifier(engine.SnapshotCache, state.Verifier.Contract);
                var result = await ModuleCall(engine, state, SmartAccountModuleKind.Verifier, state.Verifier,
                    composite ? "validateCompositeSignature" : "validateSignature", SmartAccountCallbackPhase.Validation,
                    CallFlags.ReadOnly, 100_000_000, false, state.AccountId.ToArray(), operation);
                if (composite) return ReadCompositeApproval(result!, ActiveChildren(engine, state, SmartAccountModuleKind.Verifier));
                if (result is not Boolean boolean || !boolean.GetBoolean())
                    throw new InvalidOperationException("The verifier must return exactly Boolean true.");
            }
            return null;
        }
        private async ContractTask<StackItem> ExecuteOne(ApplicationEngine engine, UInt160 id, byte[] bytes)
        {
            var state = RequireState(engine.SnapshotCache, id); Array operation = ReadOperation(bytes);
            RequireLive(state, operation, Now(engine));
            var nonce = operation[3].GetInteger(); var (channel, _) = SmartAccountProtocol.GetNonceParts(nonce);
            BigInteger next = SmartAccountProtocol.ConsumeNonce(nonce, GetNonce(engine.SnapshotCache, id, channel));
            if (state.Hook is not null) _ = Inspect(engine, state.Hook.Contract, SmartAccountModuleKind.Hook, state.Hook);
            CompositeApproval? approval = await Authorize(engine, state, bytes);
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
            // Each root callback receives its own result graph. Neither one can rewrite
            // the next callback's policy input or the value returned to the transaction.
            byte[] resultSnapshot = SnapshotResult(engine, result);
            if (state.Hook is not null)
                _ = await ModuleCall(engine, state, SmartAccountModuleKind.Hook, state.Hook, "postExecute",
                    SmartAccountCallbackPhase.PostExecute, CallFlags.All, 250_000_000, false, id.ToArray(), ReadOperation(bytes), ReadResult(engine, resultSnapshot));
            if (state.Verifier is not null)
            {
                var arguments = new StackItem[] { id.ToArray(), ReadOperation(bytes), ReadResult(engine, resultSnapshot) };
                if (approval is not null) arguments = [.. arguments, approval.ToStackItem()];
                _ = await ModuleCallWithApproval(engine, state, SmartAccountModuleKind.Verifier, state.Verifier,
                    approval is null ? "postExecute" : "postExecuteComposite", SmartAccountCallbackPhase.PostExecute,
                    CallFlags.All, 100_000_000, false, approval, arguments);
            }
            Notify(engine, "UserOpExecuted", id.ToArray(), target.ToArray(), method, nonce);
            return ReadResult(engine, resultSnapshot);
        }
        [ContractMethod(Hardfork.HF_SmartAccountV1, CpuFee = BaseCpuFee, RequiredCallFlags = CallFlags.All)]
        private async ContractTask<StackItem> ExecuteUserOp(ApplicationEngine engine, UInt160 accountId, Array op,
            [ExecutionCounter] BigInteger expectedAuthorityEpoch, [ExecutionCounter] BigInteger expectedConfigurationNonce)
        {
            RequireApplication(engine); using var locked = Context(engine).EnterAccount(accountId);
            RequireExecutionCounters(RequireState(engine.SnapshotCache, accountId),
                (ulong)expectedAuthorityEpoch, (ulong)expectedConfigurationNonce);
            byte[] bytes = SnapshotOperation(op);
            return (await ExecuteOne(engine, accountId, bytes))!;
        }
        [ContractMethod(Hardfork.HF_SmartAccountV1, CpuFee = BaseCpuFee, RequiredCallFlags = CallFlags.All)]
        private async ContractTask<Array> ExecuteUserOps(ApplicationEngine engine, UInt160 accountId, Array ops,
            [ExecutionCounter] BigInteger expectedAuthorityEpoch, [ExecutionCounter] BigInteger expectedConfigurationNonce)
        {
            RequireApplication(engine); using var locked = Context(engine).EnterAccount(accountId);
            RequireExecutionCounters(RequireState(engine.SnapshotCache, accountId),
                (ulong)expectedAuthorityEpoch, (ulong)expectedConfigurationNonce);
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
            // UserOperation signatures authorize actions, not the transaction's fee fields.
            if (tx.Sender == state.AccountAddress)
                throw new InvalidOperationException("The SmartAccount proxy cannot be the transaction fee payer.");
            using var locked = Context(engine).EnterAccount(accountId);
            var envelope = SmartAccountEnvelope.Parse(accountId, tx.Script.Span);
            RequireExecutionCounters(state, envelope.ExpectedAuthorityEpoch, envelope.ExpectedConfigurationNonce);
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
