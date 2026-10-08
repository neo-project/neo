// Copyright (C) 2015-2026 The Neo Project.
//
// AccountManagement.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

#pragma warning disable IDE0051

using Neo.Extensions;
using Neo.Json;
using Neo.Persistence;
using Neo.SmartContract.Manifest;
using Neo.VM.Types;
using System;
using System.Collections.Immutable;
using System.Numerics;
using Array = Neo.VM.Types.Array;

namespace Neo.SmartContract.Native
{
    /// <summary>The hardfork-gated native SmartAccount service.</summary>
    public sealed partial class AccountManagement : NativeContract
    {
        private const long BaseCpuFee = 1 << 15;
        private const string ParameterDigest = "2601e456d8d5a3f746c8cdcd6f90f19a62bf00bdfb856ef6f14be74a2b44f81a";
        private const byte ProxyPrefix = 0x11, PendingCallPrefix = 0x30, DependencyPrefix = 0x40;
        public override ImmutableHashSet<Hardfork?> Activations => [Hardfork.HF_SmartAccountV1];

        [ContractEvent(Hardfork.HF_SmartAccountV1, 0, "AccountCreated", "accountId", ContractParameterType.Hash160, "accountAddress", ContractParameterType.Hash160, "custodyAddress", ContractParameterType.Hash160, "verifier", ContractParameterType.Hash160, "hook", ContractParameterType.Hash160, "recoveryAddress", ContractParameterType.Hash160)]
        [ContractEvent(Hardfork.HF_SmartAccountV1, 1, "VerifierChangeProposed", "accountId", ContractParameterType.Hash160, "verifier", ContractParameterType.Hash160, "activateAt", ContractParameterType.Integer, "configurationNonce", ContractParameterType.Integer)]
        [ContractEvent(Hardfork.HF_SmartAccountV1, 2, "VerifierChanged", "accountId", ContractParameterType.Hash160, "verifier", ContractParameterType.Hash160, "configurationNonce", ContractParameterType.Integer)]
        [ContractEvent(Hardfork.HF_SmartAccountV1, 3, "VerifierChangeCancelled", "accountId", ContractParameterType.Hash160, "configurationNonce", ContractParameterType.Integer)]
        [ContractEvent(Hardfork.HF_SmartAccountV1, 4, "HookChangeProposed", "accountId", ContractParameterType.Hash160, "hook", ContractParameterType.Hash160, "activateAt", ContractParameterType.Integer, "configurationNonce", ContractParameterType.Integer)]
        [ContractEvent(Hardfork.HF_SmartAccountV1, 5, "HookChanged", "accountId", ContractParameterType.Hash160, "hook", ContractParameterType.Hash160, "configurationNonce", ContractParameterType.Integer)]
        [ContractEvent(Hardfork.HF_SmartAccountV1, 6, "HookChangeCancelled", "accountId", ContractParameterType.Hash160, "configurationNonce", ContractParameterType.Integer)]
        [ContractEvent(Hardfork.HF_SmartAccountV1, 7, "RecoveryAddressChangeProposed", "accountId", ContractParameterType.Hash160, "recoveryAddress", ContractParameterType.Hash160, "activateAt", ContractParameterType.Integer, "configurationNonce", ContractParameterType.Integer)]
        [ContractEvent(Hardfork.HF_SmartAccountV1, 8, "RecoveryAddressChanged", "accountId", ContractParameterType.Hash160, "recoveryAddress", ContractParameterType.Hash160, "configurationNonce", ContractParameterType.Integer)]
        [ContractEvent(Hardfork.HF_SmartAccountV1, 9, "RecoveryAddressChangeCancelled", "accountId", ContractParameterType.Hash160, "configurationNonce", ContractParameterType.Integer)]
        [ContractEvent(Hardfork.HF_SmartAccountV1, 10, "RecoveryProposed", "accountId", ContractParameterType.Hash160, "newCustodyAddress", ContractParameterType.Hash160, "executeAt", ContractParameterType.Integer, "configurationNonce", ContractParameterType.Integer)]
        [ContractEvent(Hardfork.HF_SmartAccountV1, 11, "RecoveryCancelled", "accountId", ContractParameterType.Hash160, "configurationNonce", ContractParameterType.Integer)]
        [ContractEvent(Hardfork.HF_SmartAccountV1, 12, "RecoveryExecuted", "accountId", ContractParameterType.Hash160, "oldCustodyAddress", ContractParameterType.Hash160, "newCustodyAddress", ContractParameterType.Hash160, "configurationNonce", ContractParameterType.Integer)]
        [ContractEvent(Hardfork.HF_SmartAccountV1, 13, "AccountFrozen", "accountId", ContractParameterType.Hash160)]
        [ContractEvent(Hardfork.HF_SmartAccountV1, 14, "AccountUnfrozen", "accountId", ContractParameterType.Hash160)]
        [ContractEvent(Hardfork.HF_SmartAccountV1, 15, "UserOpExecuted", "accountId", ContractParameterType.Hash160, "targetContract", ContractParameterType.Hash160, "method", ContractParameterType.String, "nonce", ContractParameterType.Integer)]
        internal AccountManagement() { }

        protected override void OnManifestCompose(IsHardforkEnabledDelegate checker, uint height, ContractManifest manifest)
        {
            if (checker(Hardfork.HF_SmartAccountV1, height))
                manifest.Extra = new JObject { ["smartAccount"] = new JObject { ["abiVersion"] = 1, ["profileParameterDigest"] = ParameterDigest } };
        }

        internal override ContractTask InitializeAsync(ApplicationEngine engine, Hardfork? hardfork)
        {
            if (hardfork == Hardfork.HF_SmartAccountV1)
                engine.SnapshotCache.Add(CreateStorageKey(0), new StorageItem(BinarySerializer.Serialize(
                    new Array([1, Convert.FromHexString(ParameterDigest)]), 128, 8)));
            return ContractTask.CompletedTask;
        }

        private StorageKey AccountKey(UInt160 id) => new() { Id = Id, Key = SmartAccountProtocol.GetAccountKey(id) };
        private StorageKey NonceKey(UInt160 id, BigInteger channel) => new() { Id = Id, Key = SmartAccountProtocol.GetNonceKey(id, channel) };
        private StorageKey RoleKey(byte prefix, UInt160 id, SmartAccountModuleKind kind) => new KeyBuilder(Id, prefix).Add(id).Add((byte)kind);
        private static ulong Now(ApplicationEngine engine) => engine.PersistingBlock?.Timestamp ??
            Ledger.GetHeader(engine.SnapshotCache, Ledger.CurrentHash(engine.SnapshotCache))?.Timestamp ??
            throw new InvalidOperationException("SmartAccount requires a known ledger timestamp.");
        private static void RequireApplication(ApplicationEngine engine)
        {
            if (engine.Trigger != TriggerType.Application) throw new InvalidOperationException("This SmartAccount method requires Application trigger.");
        }
        private static SmartAccountInvocationContext Context(ApplicationEngine engine)
        {
            var context = engine.GetState<SmartAccountInvocationContext>();
            if (context is null)
            {
                context = new SmartAccountInvocationContext();
                engine.SetState(context);
            }
            return context;
        }
        private SmartAccountState? ReadState(IReadOnlyStore snapshot, UInt160 id) => snapshot.TryGet(AccountKey(id), out var item) ? SmartAccountState.Deserialize(id, item.Value) : null;
        private SmartAccountState RequireState(IReadOnlyStore snapshot, UInt160 id) => ReadState(snapshot, id) ?? throw new InvalidOperationException("The SmartAccount is not registered.");
        private void Put(ApplicationEngine engine, StorageKey key, byte[] value) => engine.Put(new StorageContext { Id = Id }, key.Key.ToArray(), value);
        private void Save(ApplicationEngine engine, SmartAccountState state)
        {
            var prior = ReadState(engine.SnapshotCache, state.AccountId);
            if (prior is not null && prior.ConfigurationNonce != state.ConfigurationNonce)
                foreach (var kind in new[] { SmartAccountModuleKind.Verifier, SmartAccountModuleKind.Hook })
                    engine.SnapshotCache.Delete(RoleKey(PendingCallPrefix, state.AccountId, kind));
            Put(engine, AccountKey(state.AccountId), state.Serialize());
        }
        private void Notify(ApplicationEngine engine, string name, params StackItem[] fields) => engine.SendNotification(Hash, name, new Array(fields));
        private static bool Witness(ApplicationEngine engine, UInt160 address) => address != UInt160.Zero && engine.CheckWitnessInternal(address);
        private static SmartAccountModuleKind Kind(string type) => type switch { "verifier" => SmartAccountModuleKind.Verifier, "hook" => SmartAccountModuleKind.Hook, _ => throw new ArgumentException("Unknown SmartAccount module type.", nameof(type)) };
        private static SmartAccountModuleBinding? Binding(SmartAccountState state, SmartAccountModuleKind kind) => kind == SmartAccountModuleKind.Verifier ? state.Verifier : state.Hook;

        internal bool IsRegisteredProxy(IReadOnlyStore snapshot, UInt160 proxy) => snapshot.TryGet(CreateStorageKey(ProxyPrefix, proxy), out _);

        [ContractMethod(Hardfork.HF_SmartAccountV1, CpuFee = BaseCpuFee, RequiredCallFlags = CallFlags.None)]
        private static BigInteger GetVersion() => SmartAccountProtocol.Version;
        [ContractMethod(Hardfork.HF_SmartAccountV1, CpuFee = BaseCpuFee, RequiredCallFlags = CallFlags.ReadStates)]
        private StackItem GetAccount(IReadOnlyStore snapshot, UInt160 accountId) => ReadState(snapshot, accountId)?.ToStackItem() ?? StackItem.Null;
        [ContractMethod(Hardfork.HF_SmartAccountV1, CpuFee = BaseCpuFee, RequiredCallFlags = CallFlags.ReadStates)]
        private UInt160 GetAccountAddress(IReadOnlyStore snapshot, UInt160 accountId) => RequireState(snapshot, accountId).AccountAddress;
        [ContractMethod(Hardfork.HF_SmartAccountV1, CpuFee = BaseCpuFee, RequiredCallFlags = CallFlags.ReadStates)]
        private BigInteger GetNonce(IReadOnlyStore snapshot, UInt160 accountId, BigInteger channel)
        {
            _ = RequireState(snapshot, accountId);
            var key = NonceKey(accountId, channel);
            BigInteger value = snapshot.TryGet(key, out var item) ? new BigInteger(item.Value.Span) : BigInteger.Zero;
            if (value < 0 || value > SmartAccountProtocol.ExhaustedSequence) throw new FormatException("The stored channel cursor is invalid.");
            return value;
        }
        [ContractMethod(Hardfork.HF_SmartAccountV1, CpuFee = BaseCpuFee, RequiredCallFlags = CallFlags.None)]
        private static byte[] GetAuthorizationDomain(ApplicationEngine engine, UInt160 accountId) => SmartAccountProtocol.GetAuthorizationDomain(engine.ProtocolSettings.Network, accountId);
        [ContractMethod(Hardfork.HF_SmartAccountV1, CpuFee = BaseCpuFee, RequiredCallFlags = CallFlags.ReadStates)]
        private byte[] GetOperationDigest(ApplicationEngine engine, UInt160 accountId, Array op)
        {
            _ = RequireState(engine.SnapshotCache, accountId);
            return SmartAccountProtocol.GetOperationDigest(engine.ProtocolSettings.Network, accountId, op);
        }
        [ContractMethod(Hardfork.HF_SmartAccountV1, CpuFee = BaseCpuFee, RequiredCallFlags = CallFlags.None)]
        private static bool HasModuleContext(ApplicationEngine engine, UInt160 accountId, string moduleType, UInt160 module, string phase)
        {
            if (moduleType is not ("verifier" or "hook")) return false;
            SmartAccountCallbackPhase? selected = phase switch
            {
                "validation" => SmartAccountCallbackPhase.Validation,
                "preExecute" => SmartAccountCallbackPhase.PreExecute,
                "postExecute" => SmartAccountCallbackPhase.PostExecute,
                "configuration" => SmartAccountCallbackPhase.Configuration,
                "cleanup" => SmartAccountCallbackPhase.Cleanup,
                _ => null
            };
            return selected is { } value && (engine.GetState<SmartAccountInvocationContext>()?.IsModuleAuthorized(engine, accountId, Kind(moduleType), module, value) ?? false);
        }
        [ContractMethod(Hardfork.HF_SmartAccountV1, CpuFee = BaseCpuFee, RequiredCallFlags = CallFlags.None)]
        private static bool IsAccountAuthorized(ApplicationEngine engine, UInt160 accountId) => engine.GetState<SmartAccountInvocationContext>()?.IsTargetAuthorized(engine, accountId) ?? false;

        private async ContractTask<SmartAccountModuleBinding?> Admit(ApplicationEngine engine, UInt160 module, SmartAccountModuleKind kind)
        {
            if (module == UInt160.Zero) return null;
            var binding = Inspect(engine, module, kind);
            if (await SmartAccountModulePolicy.DiscoverAsync(engine, binding, kind, false)) RequireCompositeProfile(engine, binding, kind);
            return binding;
        }
        private SmartAccountModuleBinding Inspect(ApplicationEngine engine, UInt160 module, SmartAccountModuleKind kind, SmartAccountModuleBinding? expected = null)
        {
            engine.AddFee((BigInteger)BaseCpuFee * engine.ExecFeeFactor, true);
            return SmartAccountModulePolicy.Inspect(engine.SnapshotCache, module, kind, expected);
        }

        [ContractMethod(Hardfork.HF_SmartAccountV1, CpuFee = BaseCpuFee, RequiredCallFlags = CallFlags.All)]
        private async ContractTask<UInt160> RegisterAccount(ApplicationEngine engine, UInt160 custodyAddress, byte[] salt, UInt160 verifier, UInt160 hook, UInt160 recoveryAddress)
        {
            RequireApplication(engine);
            var empty = SmartAccountState.Create(engine.ProtocolSettings.Network, custodyAddress, salt, null, null, recoveryAddress, Witness(engine, custodyAddress));
            using var locked = Context(engine).EnterAccount(empty.AccountId);
            if (ReadState(engine.SnapshotCache, empty.AccountId) is not null || IsRegisteredProxy(engine.SnapshotCache, empty.AccountAddress))
                throw new InvalidOperationException("The SmartAccount identity or proxy is already registered.");
            var verifierBinding = await Admit(engine, verifier, SmartAccountModuleKind.Verifier);
            var hookBinding = await Admit(engine, hook, SmartAccountModuleKind.Hook);
            var state = SmartAccountState.Create(engine.ProtocolSettings.Network, custodyAddress, salt, verifierBinding, hookBinding, recoveryAddress, true);
            Save(engine, state);
            Put(engine, CreateStorageKey(ProxyPrefix, state.AccountAddress), state.AccountId.ToArray());
            Notify(engine, "AccountCreated", state.AccountId.ToArray(), state.AccountAddress.ToArray(), custodyAddress.ToArray(), verifier.ToArray(), hook.ToArray(), recoveryAddress.ToArray());
            return state.AccountId;
        }

        private void Transition(ApplicationEngine engine, UInt160 id, Func<SmartAccountState, SmartAccountState> change,
            string notification, Func<SmartAccountState, SmartAccountState, StackItem[]> fields)
        {
            RequireApplication(engine); using var locked = Context(engine).EnterAccount(id);
            var prior = RequireState(engine.SnapshotCache, id); var next = change(prior);
            Save(engine, next); Notify(engine, notification, fields(prior, next));
        }
        [ContractMethod(Hardfork.HF_SmartAccountV1, CpuFee = BaseCpuFee, RequiredCallFlags = CallFlags.All)]
        private void ProposeRecoveryAddress(ApplicationEngine engine, UInt160 accountId, UInt160 recovery) =>
            Transition(engine, accountId, s => s.ProposeRecoveryAddress(recovery, Now(engine), Witness(engine, s.CustodyAddress)), "RecoveryAddressChangeProposed", (p, n) => [accountId.ToArray(), recovery.ToArray(), new BigInteger(Now(engine) + SmartAccountState.ModuleChangeDelayMs), new BigInteger(n.ConfigurationNonce)]);
        [ContractMethod(Hardfork.HF_SmartAccountV1, CpuFee = BaseCpuFee, RequiredCallFlags = CallFlags.All)]
        private void ActivateRecoveryAddress(ApplicationEngine engine, UInt160 accountId) =>
            Transition(engine, accountId, s => s.ActivateRecoveryAddress(Now(engine)), "RecoveryAddressChanged", (p, n) => [accountId.ToArray(), n.RecoveryAddress.ToArray(), new BigInteger(n.ConfigurationNonce)]);
        [ContractMethod(Hardfork.HF_SmartAccountV1, CpuFee = BaseCpuFee, RequiredCallFlags = CallFlags.All)]
        private void CancelRecoveryAddress(ApplicationEngine engine, UInt160 accountId) =>
            Transition(engine, accountId, s => s.CancelRecoveryAddress(Witness(engine, s.CustodyAddress)), "RecoveryAddressChangeCancelled", (p, n) => [accountId.ToArray(), new BigInteger(n.ConfigurationNonce)]);
        [ContractMethod(Hardfork.HF_SmartAccountV1, CpuFee = BaseCpuFee, RequiredCallFlags = CallFlags.All)]
        private void ProposeRecovery(ApplicationEngine engine, UInt160 accountId, UInt160 newCustody) =>
            Transition(engine, accountId, s => s.ProposeRecovery(newCustody, Now(engine), Witness(engine, s.RecoveryAddress)), "RecoveryProposed", (p, n) => [accountId.ToArray(), newCustody.ToArray(), new BigInteger(Now(engine) + SmartAccountState.CustodyRecoveryDelayMs), new BigInteger(n.ConfigurationNonce)]);
        [ContractMethod(Hardfork.HF_SmartAccountV1, CpuFee = BaseCpuFee, RequiredCallFlags = CallFlags.All)]
        private void ExecuteRecovery(ApplicationEngine engine, UInt160 accountId) =>
            Transition(engine, accountId, s => s.ExecuteRecovery(Now(engine)), "RecoveryExecuted", (p, n) => [accountId.ToArray(), p.CustodyAddress.ToArray(), n.CustodyAddress.ToArray(), new BigInteger(n.ConfigurationNonce)]);
        [ContractMethod(Hardfork.HF_SmartAccountV1, CpuFee = BaseCpuFee, RequiredCallFlags = CallFlags.All)]
        private void CancelRecovery(ApplicationEngine engine, UInt160 accountId) =>
            Transition(engine, accountId, s => s.CancelRecovery(Now(engine), Witness(engine, s.CustodyAddress), Witness(engine, s.RecoveryAddress)), "RecoveryCancelled", (p, n) => [accountId.ToArray(), new BigInteger(n.ConfigurationNonce)]);
        [ContractMethod(Hardfork.HF_SmartAccountV1, CpuFee = BaseCpuFee, RequiredCallFlags = CallFlags.All)]
        private void Freeze(ApplicationEngine engine, UInt160 accountId) =>
            Transition(engine, accountId, s => s.Freeze(Witness(engine, s.RecoveryAddress)), "AccountFrozen", (p, n) => [accountId.ToArray()]);
        [ContractMethod(Hardfork.HF_SmartAccountV1, CpuFee = BaseCpuFee, RequiredCallFlags = CallFlags.All)]
        private void Unfreeze(ApplicationEngine engine, UInt160 accountId) =>
            Transition(engine, accountId, s => s.Unfreeze(Witness(engine, s.CustodyAddress), Witness(engine, s.RecoveryAddress)), "AccountUnfrozen", (p, n) => [accountId.ToArray()]);

        private async ContractTask ProposeModule(ApplicationEngine engine, UInt160 id, UInt160 module, SmartAccountModuleKind kind)
        {
            RequireApplication(engine); using var locked = Context(engine).EnterAccount(id);
            var state = RequireState(engine.SnapshotCache, id);
            // Validate authority/status before allowing any plugin discovery.
            _ = state.ProposeModule(kind, null, Now(engine), Witness(engine, state.CustodyAddress));
            var binding = await Admit(engine, module, kind);
            var next = state.ProposeModule(kind, binding, Now(engine), true); Save(engine, next);
            Notify(engine, kind == SmartAccountModuleKind.Verifier ? "VerifierChangeProposed" : "HookChangeProposed",
                id.ToArray(), module.ToArray(), new BigInteger(Now(engine) + SmartAccountState.ModuleChangeDelayMs), new BigInteger(state.ConfigurationNonce));
        }
        private async ContractTask ActivateModule(ApplicationEngine engine, UInt160 id, SmartAccountModuleKind kind)
        {
            RequireApplication(engine); using var locked = Context(engine).EnterAccount(id);
            var state = RequireState(engine.SnapshotCache, id); var next = state.ActivateModule(kind, Now(engine));
            var replacement = Binding(next, kind);
            if (replacement is not null)
            {
                _ = Inspect(engine, replacement.Contract, kind, replacement);
                if (await SmartAccountModulePolicy.DiscoverAsync(engine, replacement, kind, false)) RequireCompositeProfile(engine, replacement, kind);
            }
            await CleanupRoot(engine, state, kind);
            Save(engine, next);
            Notify(engine, kind == SmartAccountModuleKind.Verifier ? "VerifierChanged" : "HookChanged",
                id.ToArray(), (replacement?.Contract ?? UInt160.Zero).ToArray(), new BigInteger(next.ConfigurationNonce));
        }
        [ContractMethod(Hardfork.HF_SmartAccountV1, CpuFee = BaseCpuFee, RequiredCallFlags = CallFlags.All)]
        private ContractTask ProposeVerifier(ApplicationEngine engine, UInt160 accountId, UInt160 verifier) => ProposeModule(engine, accountId, verifier, SmartAccountModuleKind.Verifier);
        [ContractMethod(Hardfork.HF_SmartAccountV1, CpuFee = BaseCpuFee, RequiredCallFlags = CallFlags.All)]
        private ContractTask ActivateVerifier(ApplicationEngine engine, UInt160 accountId) => ActivateModule(engine, accountId, SmartAccountModuleKind.Verifier);
        [ContractMethod(Hardfork.HF_SmartAccountV1, CpuFee = BaseCpuFee, RequiredCallFlags = CallFlags.All)]
        private void CancelVerifier(ApplicationEngine engine, UInt160 accountId) => Transition(engine, accountId, s => s.CancelModule(SmartAccountModuleKind.Verifier, Witness(engine, s.CustodyAddress)), "VerifierChangeCancelled", (p, n) => [accountId.ToArray(), new BigInteger(n.ConfigurationNonce)]);
        [ContractMethod(Hardfork.HF_SmartAccountV1, CpuFee = BaseCpuFee, RequiredCallFlags = CallFlags.All)]
        private ContractTask ProposeHook(ApplicationEngine engine, UInt160 accountId, UInt160 hook) => ProposeModule(engine, accountId, hook, SmartAccountModuleKind.Hook);
        [ContractMethod(Hardfork.HF_SmartAccountV1, CpuFee = BaseCpuFee, RequiredCallFlags = CallFlags.All)]
        private ContractTask ActivateHook(ApplicationEngine engine, UInt160 accountId) => ActivateModule(engine, accountId, SmartAccountModuleKind.Hook);
        [ContractMethod(Hardfork.HF_SmartAccountV1, CpuFee = BaseCpuFee, RequiredCallFlags = CallFlags.All)]
        private void CancelHook(ApplicationEngine engine, UInt160 accountId) => Transition(engine, accountId, s => s.CancelModule(SmartAccountModuleKind.Hook, Witness(engine, s.CustodyAddress)), "HookChangeCancelled", (p, n) => [accountId.ToArray(), new BigInteger(n.ConfigurationNonce)]);
    }
}
