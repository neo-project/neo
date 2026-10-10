// Copyright (C) 2015-2026 The Neo Project.
//
// AccountManagement.Modules.cs file belongs to the neo project and is free
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
using Neo.Json;
using Neo.Persistence;
using Neo.SmartContract.Manifest;
using Neo.VM.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Array = Neo.VM.Types.Array;
using Boolean = Neo.VM.Types.Boolean;

namespace Neo.SmartContract.Native
{
    partial class AccountManagement
    {
        private sealed record Dependencies(SmartAccountModuleBinding? Root, SmartAccountModuleBinding[] Enrolled, UInt160[] Active)
        {
            internal Array ToArray() => new([Root?.ToStackItem() ?? StackItem.Null,
                new Array(Enrolled.Select(p => p.ToStackItem())), new Array(Active.Select(p => (StackItem)p.ToArray()))]);
        }
        private static Array ExactArray(StackItem item, int? count = null)
        {
            if (item is not Array array || item.Type != StackItemType.Array || (count.HasValue && array.Count != count))
                throw new FormatException("Invalid SmartAccount module record Array.");
            return array;
        }
        private static byte[] Bytes(StackItem item, int size)
        {
            if (item is not ByteString bytes || bytes.Size != size) throw new FormatException("Invalid SmartAccount module record ByteString.");
            return bytes.GetSpan().ToArray();
        }
        private static SmartAccountModuleBinding DecodeBinding(StackItem value)
        {
            var array = ExactArray(value, 2);
            return new(new UInt160(Bytes(array[0], UInt160.Length)), new UInt256(Bytes(array[1], UInt256.Length)));
        }
        private static bool SameBinding(SmartAccountModuleBinding a, SmartAccountModuleBinding b) => a.Contract == b.Contract && a.CodeHash == b.CodeHash;
        private static Array DecodeRecord(ReadOnlyMemory<byte> bytes)
        {
            MemoryReader reader = new(bytes);
            var record = ExactArray(BinarySerializer.Deserialize(ref reader, 8192, 8192));
            if (reader.Position != bytes.Length || !bytes.Span.SequenceEqual(BinarySerializer.Serialize(record, 8192, 8192)))
                throw new FormatException("A SmartAccount module record must be canonical.");
            return record;
        }
        private static ulong IntentInteger(StackItem item)
        {
            if (item is not Integer integer || integer.GetInteger() < 0 || integer.GetInteger() > ulong.MaxValue)
                throw new FormatException("A pending call requires exact UInt64 Integers.");
            return (ulong)integer.GetInteger();
        }
        private static Array ReadIntent(ReadOnlyMemory<byte> bytes, SmartAccountState state, SmartAccountModuleKind kind)
        {
            var record = ExactArray(DecodeRecord(bytes), 10);
            var root = Binding(state, kind);
            ulong version = IntentInteger(record[0]), role = IntentInteger(record[2]);
            ulong proposed = IntentInteger(record[7]), mature = IntentInteger(record[8]), epoch = IntentInteger(record[9]);
            var storedRoot = DecodeBinding(record[3]); var selected = DecodeBinding(record[4]);
            if (version != 1 || role != (ulong)kind || new UInt160(Bytes(record[1], UInt160.Length)) != state.AccountId ||
                root is null || !SameBinding(root, storedRoot) || epoch != state.ConfigurationNonce || epoch == ulong.MaxValue ||
                proposed > ulong.MaxValue - SmartAccountState.ModuleChangeDelayMs || mature != proposed + SmartAccountState.ModuleChangeDelayMs)
                throw new FormatException("The pending call identity, root, epoch or fixed delay is invalid.");
            var args = ExactArray(record[6]);
            if (record[5] is not ByteString || args.Count == 0 || new UInt160(Bytes(args[0], UInt160.Length)) != state.AccountId)
                throw new FormatException("The pending call requires an exact method and account-prefixed arguments.");
            // Reuse operation canonicalization for exact method, type, depth and
            // byte limits without trusting the stored serializer representation.
            _ = SnapshotOperation(new Array([selected.Contract.ToArray(), record[5], args, BigInteger.Zero, BigInteger.Zero, ByteString.Empty]));
            return record;
        }
        private Dependencies ReadDependencies(IReadOnlyStore snapshot, SmartAccountState state, SmartAccountModuleKind kind)
        {
            var root = Binding(state, kind);
            if (!snapshot.TryGet(RoleKey(DependencyPrefix, state.AccountId, kind), out var item)) return new(root, [], []);
            var record = ExactArray(DecodeRecord(item.Value), 3);
            var storedRoot = DecodeBinding(record[0]);
            if (root is null || !SameBinding(root, storedRoot)) throw new FormatException("The dependency registry has a stale root.");
            var enrolled = ExactArray(record[1]).Select(DecodeBinding).ToArray();
            var active = ExactArray(record[2]).Select(p => new UInt160(Bytes(p, UInt160.Length))).ToArray();
            int maximum = kind == SmartAccountModuleKind.Verifier ? 3 : 8;
            if (enrolled.Length > maximum || active.Length > maximum || enrolled.Select(p => p.Contract).Distinct().Count() != enrolled.Length ||
                active.Distinct().Count() != active.Length || enrolled.Any(p => p.Contract == root.Contract) || active.Any(p => !enrolled.Any(e => e.Contract == p)))
                throw new FormatException("The dependency registry has an invalid leaf roster.");
            return new(storedRoot, enrolled, active);
        }
        private void WriteDependencies(ApplicationEngine engine, UInt160 id, SmartAccountModuleKind kind, Dependencies value)
        {
            var key = RoleKey(DependencyPrefix, id, kind);
            if (value.Enrolled.Length == 0 && value.Active.Length == 0) engine.SnapshotCache.Delete(key);
            else Put(engine, key, BinarySerializer.Serialize(value.ToArray(), 8192, 8192));
        }
        private UInt160[] ActiveChildren(ApplicationEngine engine, SmartAccountState state, SmartAccountModuleKind kind)
        {
            var registry = ReadDependencies(engine.SnapshotCache, state, kind);
            foreach (var child in registry.Active)
                _ = Inspect(engine, child, kind, registry.Enrolled.Single(p => p.Contract == child));
            return registry.Active;
        }
        private void RequireCompositeProfile(ApplicationEngine engine, SmartAccountModuleBinding root, SmartAccountModuleKind kind)
        {
            var contract = ContractManagement.GetContract(engine.SnapshotCache, root.Contract)!;
            string name = kind == SmartAccountModuleKind.Verifier ? "MultiSigVerifier" : "MultiHook";
            if (contract.Manifest.Name != name ||
                (kind == SmartAccountModuleKind.Verifier && !SmartAccountModulePolicy.IsCompositeVerifier(engine.SnapshotCache, root.Contract)))
                throw new InvalidOperationException("The declared composite profile is not supported.");
        }
        private async ContractTask<bool> Discover(ApplicationEngine engine, SmartAccountModuleBinding binding, SmartAccountModuleKind kind,
            bool leaf, bool inherit)
        {
            _ = Inspect(engine, binding.Contract, kind, binding);
            if (!inherit) return await SmartAccountModulePolicy.DiscoverAsync(engine, binding, kind, leaf);
            var result = await engine.CallFromNativeContractRawAsync(Hash, binding.Contract, "supportsComposition", CallFlags.ReadOnly, true);
            if (result is not Boolean flag || (leaf && flag.GetBoolean())) throw new InvalidOperationException("Invalid or nested module composition marker.");
            if (kind == SmartAccountModuleKind.Verifier && flag.GetBoolean() != SmartAccountModulePolicy.IsCompositeVerifier(engine.SnapshotCache, binding.Contract))
                throw new InvalidOperationException("Invalid verifier composition marker for the declared profile capability.");
            if (leaf && kind == SmartAccountModuleKind.Verifier)
            {
                var method = ContractManagement.GetContract(engine.SnapshotCache, binding.Contract)!.Manifest.Abi.GetMethod("getSignerDomains", 1);
                if (method is null || !method.Safe || method.ReturnType != ContractParameterType.Array || method.Parameters[0].Type != ContractParameterType.Hash160)
                    throw new InvalidOperationException("Leaf verifier signer-domain ABI is missing.");
            }
            return flag.GetBoolean();
        }
        private async ContractTask<IReadOnlyList<UInt256>> Domains(ApplicationEngine engine, UInt160 id, SmartAccountModuleBinding child, bool inherit)
        {
            if (!inherit) return (await SmartAccountModulePolicy.ReadSignerDomainsAsync(engine, id, child))!;
            var result = await engine.CallFromNativeContractRawAsync(Hash, child.Contract, "getSignerDomains", CallFlags.ReadOnly, true, id.ToArray());
            if (result is not Array values || result.Type != StackItemType.Array || values.Count == 0)
                throw new InvalidOperationException("A verifier child needs a non-empty exact signer-domain Array.");
            HashSet<UInt256> unique = new(); List<UInt256> domains = new();
            foreach (var item in values)
            {
                var domain = new UInt256(Bytes(item, UInt256.Length));
                if (!unique.Add(domain)) throw new InvalidOperationException("Duplicate signer domain within one child.");
                domains.Add(domain);
            }
            return domains;
        }
        private async ContractTask CheckDomains(ApplicationEngine engine, UInt160 id, Dependencies registry, bool inherit)
        {
            HashSet<UInt256> seen = new();
            foreach (var child in registry.Active)
            {
                var binding = registry.Enrolled.Single(p => p.Contract == child);
                _ = Inspect(engine, child, SmartAccountModuleKind.Verifier, binding);
                _ = await Discover(engine, binding, SmartAccountModuleKind.Verifier, true, inherit);
                foreach (var domain in (await Domains(engine, id, binding, inherit))!)
                    if (!seen.Add(domain)) throw new InvalidOperationException("Verifier children share a signer domain.");
            }
            if (seen.Count > 3) throw new InvalidOperationException("The composite verifier exceeds three aggregate signer domains.");
        }
        private async ContractTask CheckRootDomains(ApplicationEngine engine, UInt160 id, Dependencies registry)
        {
            if (registry.Active.Length == 0) return;
            var root = registry.Root!;
            var result = await engine.CallFromNativeContractWithGasLimitAsync(Hash, root.Contract, "getSignerDomains", CallFlags.ReadOnly,
                SmartAccountModulePolicy.MaintenanceBudget, id.ToArray());
            _ = Inspect(engine, root.Contract, SmartAccountModuleKind.Verifier, root);
            var declared = ExactArray(result!);
            if (declared.Count is 0 or > 3) throw new InvalidOperationException("The composite verifier signer-domain declaration exceeds its bound.");
            HashSet<UInt256> unique = new();
            foreach (var value in declared)
                if (!unique.Add(new UInt256(Bytes(value, UInt256.Length)))) throw new InvalidOperationException("Duplicate root signer domain.");
        }
        private async ContractTask CleanupRoot(ApplicationEngine engine, SmartAccountState state, SmartAccountModuleKind kind)
        {
            var registry = ReadDependencies(engine.SnapshotCache, state, kind); var root = Binding(state, kind);
            if (root is null) return;
            foreach (var child in registry.Enrolled)
                _ = await ModuleCall(engine, state, kind, child, "clearAccount", SmartAccountCallbackPhase.Cleanup,
                    CallFlags.All, SmartAccountModulePolicy.MaintenanceBudget, false, state.AccountId.ToArray());
            engine.SnapshotCache.Delete(RoleKey(DependencyPrefix, state.AccountId, kind));
            _ = await ModuleCall(engine, state, kind, root, "clearAccount", SmartAccountCallbackPhase.Cleanup,
                CallFlags.All, SmartAccountModulePolicy.MaintenanceBudget, false, state.AccountId.ToArray());
        }
        private async ContractTask SetDependencies(ApplicationEngine engine, UInt160 id, Array children, SmartAccountModuleKind kind)
        {
            RequireApplication(engine); var state = RequireState(engine.SnapshotCache, id);
            var root = Binding(state, kind) ?? throw new InvalidOperationException("No root module is installed.");
            if (!Context(engine).IsModuleAuthorized(engine, id, kind, root.Contract, SmartAccountCallbackPhase.Configuration))
                throw new InvalidOperationException("Only the active root configuration invocation can publish dependencies.");
            RequireCompositeProfile(engine, root, kind);
            int maximum = kind == SmartAccountModuleKind.Verifier ? 3 : 8;
            ExactArray(children);
            if (children.Count > maximum) throw new InvalidOperationException("The child roster exceeds its profile bound.");
            var active = children.Select(p => new UInt160(Bytes(p, UInt160.Length))).ToArray();
            if (active.Distinct().Count() != active.Length || active.Any(p => p == root.Contract))
                throw new InvalidOperationException("Duplicate or self child module.");
            var prior = ReadDependencies(engine.SnapshotCache, state, kind);
            var removed = prior.Active.Where(p => !active.Contains(p)).ToArray();
            var enrolled = prior.Enrolled.Where(p => !removed.Contains(p.Contract)).ToList();
            foreach (var child in active)
            {
                var old = enrolled.SingleOrDefault(p => p.Contract == child);
                var binding = Inspect(engine, child, kind, old);
                _ = await Discover(engine, binding, kind, true, true);
                if (old is null) enrolled.Add(binding);
            }
            if (enrolled.Count > maximum) throw new InvalidOperationException("The enrolled cleanup roster exceeds its profile bound.");
            var next = new Dependencies(root, enrolled.ToArray(), active);
            if (kind == SmartAccountModuleKind.Verifier) await CheckDomains(engine, id, next, true);
            foreach (var child in removed)
                _ = await ModuleCall(engine, state, kind, prior.Enrolled.Single(p => p.Contract == child), "clearAccount", SmartAccountCallbackPhase.Cleanup,
                    CallFlags.All, SmartAccountModulePolicy.MaintenanceBudget, true, id.ToArray());
            WriteDependencies(engine, id, kind, next);
        }
        private async ContractTask ClearDependencies(ApplicationEngine engine, UInt160 id, SmartAccountModuleKind kind)
        {
            RequireApplication(engine); var state = RequireState(engine.SnapshotCache, id);
            var root = Binding(state, kind) ?? throw new InvalidOperationException("No root module is installed.");
            if (Context(engine).IsModuleAuthorized(engine, id, kind, root.Contract, SmartAccountCallbackPhase.Cleanup))
            {
                var current = ReadDependencies(engine.SnapshotCache, state, kind);
                if (current.Enrolled.Length != 0 || current.Active.Length != 0) throw new InvalidOperationException("Cleanup cannot discard live dependencies.");
                return;
            }
            await SetDependencies(engine, id, new Array(), kind);
        }
        [ContractMethod(Hardfork.HF_SmartAccountV1, CpuFee = BaseCpuFee, RequiredCallFlags = CallFlags.ReadStates)]
        private Array GetModuleDependencies(IReadOnlyStore snapshot, UInt160 accountId, string moduleType) =>
            ReadDependencies(snapshot, RequireState(snapshot, accountId), Kind(moduleType)).ToArray();
        [ContractMethod(Hardfork.HF_SmartAccountV1, CpuFee = BaseCpuFee, RequiredCallFlags = CallFlags.All)]
        private ContractTask SetVerifierDependencies(ApplicationEngine engine, UInt160 accountId, Array children) => SetDependencies(engine, accountId, children, SmartAccountModuleKind.Verifier);
        [ContractMethod(Hardfork.HF_SmartAccountV1, CpuFee = BaseCpuFee, RequiredCallFlags = CallFlags.All)]
        private ContractTask SetHookDependencies(ApplicationEngine engine, UInt160 accountId, Array children) => SetDependencies(engine, accountId, children, SmartAccountModuleKind.Hook);
        [ContractMethod(Hardfork.HF_SmartAccountV1, CpuFee = BaseCpuFee, RequiredCallFlags = CallFlags.All)]
        private ContractTask ClearVerifierDependencies(ApplicationEngine engine, UInt160 accountId) => ClearDependencies(engine, accountId, SmartAccountModuleKind.Verifier);
        [ContractMethod(Hardfork.HF_SmartAccountV1, CpuFee = BaseCpuFee, RequiredCallFlags = CallFlags.All)]
        private ContractTask ClearHookDependencies(ApplicationEngine engine, UInt160 accountId) => ClearDependencies(engine, accountId, SmartAccountModuleKind.Hook);

        private static Array CallArguments(UInt160 id, UInt160 module, string method, Array args)
        {
            if (args.Type != StackItemType.Array || args.Count > 63 || string.IsNullOrEmpty(method) || method.StartsWith('_'))
                throw new FormatException("Invalid account-scoped module configuration arguments.");
            var all = new Array(new StackItem[] { id.ToArray() }.Concat(args));
            var op = new Array([module.ToArray(), method, all, BigInteger.Zero, BigInteger.Zero, ByteString.Empty]);
            return (Array)ReadOperation(SnapshotOperation(op))[2];
        }
        private void RequireConfigurationMethod(ApplicationEngine engine, SmartAccountModuleBinding binding, string method, int arguments)
        {
            var contract = ContractManagement.GetContract(engine.SnapshotCache, binding.Contract)!;
            var configured = contract.Manifest.Extra?["smartAccount"]?["configurationMethods"];
            if (configured is not JArray list || list.Any(p => p is not JString) ||
                list.Select(p => p!.GetString()).Distinct(StringComparer.Ordinal).Count() != list.Count)
                throw new InvalidOperationException("The module must declare unique configuration capabilities.");
            string[] reserved = ["validateCompositeSignature", "postExecuteComposite", "validateSignatureForPostExecute", "validateSignature", "preExecute", "postExecute", "clearAccount", "supportsComposition", "getSignerDomains"];
            var names = list.Select(p => p!.GetString()).ToArray();
            var matches = contract.Manifest.Abi.Methods.Where(p => p.Name == method && p.Parameters.Length == arguments).ToArray();
            if (names.Any(p => p.StartsWith('_') || reserved.Contains(p)) || !names.Contains(method) || matches.Length != 1 ||
                matches[0].Safe || matches[0].Parameters.Length == 0 || matches[0].Parameters[0].Type != ContractParameterType.Hash160)
                throw new InvalidOperationException("The method is not an admitted account-scoped configuration capability.");
        }
        [ContractMethod(Hardfork.HF_SmartAccountV1, CpuFee = BaseCpuFee, RequiredCallFlags = CallFlags.ReadStates)]
        private StackItem GetPendingModuleCall(IReadOnlyStore snapshot, UInt160 accountId, string moduleType)
        {
            var state = RequireState(snapshot, accountId); var kind = Kind(moduleType);
            return snapshot.TryGet(RoleKey(PendingCallPrefix, accountId, kind), out var item) ? ReadIntent(item.Value, state, kind) : StackItem.Null;
        }
        [ContractMethod(Hardfork.HF_SmartAccountV1, CpuFee = BaseCpuFee, RequiredCallFlags = CallFlags.All)]
        private void CancelModuleCall(ApplicationEngine engine, UInt160 accountId, string moduleType)
        {
            RequireApplication(engine); using var locked = Context(engine).EnterAccount(accountId);
            var state = RequireState(engine.SnapshotCache, accountId); var key = RoleKey(PendingCallPrefix, accountId, Kind(moduleType));
            if (!Witness(engine, state.CustodyAddress) || !engine.SnapshotCache.Contains(key))
                throw new InvalidOperationException("Cancellation requires custody and a pending module call.");
            engine.SnapshotCache.Delete(key);
        }
        private async ContractTask<StackItem> Configure(ApplicationEngine engine, UInt160 id, UInt160? child, string method, Array args, SmartAccountModuleKind kind)
        {
            RequireApplication(engine); using var locked = Context(engine).EnterAccount(id);
            var state = RequireState(engine.SnapshotCache, id); var advanced = state.CommitConfiguration();
            if (!Witness(engine, state.CustodyAddress)) throw new InvalidOperationException("Module configuration requires the custody witness.");
            var root = Binding(state, kind) ?? throw new InvalidOperationException("No root module is installed.");
            _ = Inspect(engine, root.Contract, kind, root);
            var selected = root; var ownedArgs = CallArguments(id, child ?? root.Contract, method, args);
            var registry = ReadDependencies(engine.SnapshotCache, state, kind);
            if (child is not null)
            {
                if (child == root.Contract || !await Discover(engine, root, kind, false, false)) throw new InvalidOperationException("Child configuration requires a distinct leaf and composite root.");
                RequireCompositeProfile(engine, root, kind);
                selected = Inspect(engine, child, kind, registry.Enrolled.SingleOrDefault(p => p.Contract == child));
                _ = await Discover(engine, selected, kind, true, false);
            }
            RequireConfigurationMethod(engine, selected, method, ownedArgs.Count);
            var key = RoleKey(PendingCallPrefix, id, kind);
            ulong now = Now(engine);
            if (!engine.SnapshotCache.TryGet(key, out var stored))
            {
                if (now > ulong.MaxValue - SmartAccountState.ModuleChangeDelayMs) throw new InvalidOperationException("Configuration maturity overflows.");
                var pending = new Array([1, id.ToArray(), (int)kind, root.ToStackItem(), selected.ToStackItem(), method, ownedArgs,
                    new BigInteger(now), new BigInteger(now + SmartAccountState.ModuleChangeDelayMs), new BigInteger(state.ConfigurationNonce)]);
                Put(engine, key, BinarySerializer.Serialize(pending, 8192, 8192));
                return false;
            }
            var intent = ReadIntent(stored.Value, state, kind);
            if (intent[0].GetInteger() != 1 || new UInt160(Bytes(intent[1], 20)) != id || intent[2].GetInteger() != (int)kind ||
                !SameBinding(DecodeBinding(intent[3]), root) || !SameBinding(DecodeBinding(intent[4]), selected) ||
                intent[5].GetString() != method || !BinarySerializer.Serialize(intent[6], 8192, 8192).AsSpan().SequenceEqual(BinarySerializer.Serialize(ownedArgs, 8192, 8192)) ||
                intent[9].GetInteger() != state.ConfigurationNonce || intent[7].GetInteger() < 0 ||
                intent[8].GetInteger() != intent[7].GetInteger() + SmartAccountState.ModuleChangeDelayMs || intent[8].GetInteger() > now)
                throw new InvalidOperationException("The module call intent is immature, changed or stale.");
            if (child is not null && !registry.Enrolled.Any(p => p.Contract == child))
            {
                if (registry.Enrolled.Length >= (kind == SmartAccountModuleKind.Verifier ? 3 : 8)) throw new InvalidOperationException("The enrolled leaf roster is full.");
                registry = registry with { Enrolled = [.. registry.Enrolled, selected] }; WriteDependencies(engine, id, kind, registry);
            }
            var result = await ModuleCall(engine, state, kind, selected, method, SmartAccountCallbackPhase.Configuration,
                CallFlags.All, SmartAccountModulePolicy.MaintenanceBudget, false, ownedArgs.ToArray());
            _ = Inspect(engine, selected.Contract, kind, selected);
            if (selected.Contract != root.Contract) _ = Inspect(engine, root.Contract, kind, root);
            if (kind == SmartAccountModuleKind.Verifier)
            {
                var current = ReadDependencies(engine.SnapshotCache, state, kind);
                await CheckDomains(engine, id, current, false);
                await CheckRootDomains(engine, id, current);
            }
            Save(engine, advanced);
            return result!;
        }
        [ContractMethod(Hardfork.HF_SmartAccountV1, CpuFee = BaseCpuFee, RequiredCallFlags = CallFlags.All)]
        private ContractTask<StackItem> CallVerifier(ApplicationEngine engine, UInt160 accountId, string method, Array args) => Configure(engine, accountId, null, method, args, SmartAccountModuleKind.Verifier);
        [ContractMethod(Hardfork.HF_SmartAccountV1, CpuFee = BaseCpuFee, RequiredCallFlags = CallFlags.All)]
        private ContractTask<StackItem> CallHook(ApplicationEngine engine, UInt160 accountId, string method, Array args) => Configure(engine, accountId, null, method, args, SmartAccountModuleKind.Hook);
        [ContractMethod(Hardfork.HF_SmartAccountV1, CpuFee = BaseCpuFee, RequiredCallFlags = CallFlags.All)]
        private ContractTask<StackItem> CallVerifierChild(ApplicationEngine engine, UInt160 accountId, UInt160 childVerifier, string method, Array args) => Configure(engine, accountId, childVerifier, method, args, SmartAccountModuleKind.Verifier);
        [ContractMethod(Hardfork.HF_SmartAccountV1, CpuFee = BaseCpuFee, RequiredCallFlags = CallFlags.All)]
        private ContractTask<StackItem> CallHookChild(ApplicationEngine engine, UInt160 accountId, UInt160 childHook, string method, Array args) => Configure(engine, accountId, childHook, method, args, SmartAccountModuleKind.Hook);
    }
}
