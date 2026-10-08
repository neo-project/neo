// Copyright (C) 2015-2026 The Neo Project.
//
// SmartAccountInvocationContext.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Extensions;
using Neo.VM;
using System;
using System.Collections.Generic;

namespace Neo.SmartContract.Native
{
    internal enum SmartAccountCallbackPhase { Validation, PreExecute, PostExecute, Configuration, Cleanup }

    /// <summary>
    /// Engine-local account locks and frame-bound callback/target grants. These are
    /// additional authority constraints, not witness facts or module admission proofs.
    /// </summary>
    internal sealed class SmartAccountInvocationContext
    {
        private readonly Dictionary<UInt160, AccountLease> _accounts = new();
        private readonly Stack<Lease> _scopes = new();
        private object _generation = new();

        private abstract class Lease(SmartAccountInvocationContext owner) : IDisposable
        {
            internal readonly object Generation = owner._generation;
            private bool _disposed;
            internal abstract AccountLease Account { get; }
            public void Dispose()
            {
                if (_disposed) return;
                owner.Release(this);
                _disposed = true;
            }
        }

        private sealed class AccountLease(SmartAccountInvocationContext owner, UInt160 identity) : Lease(owner)
        {
            internal readonly UInt160 Identity = Copy(identity);
            internal readonly UInt160 Address = SmartAccountProtocol.GetAccountAddress(identity);
            internal override AccountLease Account => this;
        }

        private sealed class GrantLease(SmartAccountInvocationContext owner, AccountLease account,
            UInt160 contract, ExecutionContextState anchor, SmartAccountModuleKind? kind,
            SmartAccountCallbackPhase? phase, HashSet<UInt160> children) : Lease(owner)
        {
            internal override AccountLease Account => account;
            internal readonly UInt160 Contract = Copy(contract);
            internal readonly ExecutionContextState Anchor = anchor;
            internal readonly SmartAccountModuleKind? Kind = kind;
            internal readonly SmartAccountCallbackPhase? Phase = phase;
            internal readonly HashSet<UInt160> Children = children;
        }

        private static UInt160 Copy(UInt160 value) => new(value.ToArray());

        private static void RequireIdentity(UInt160 identity)
        {
            ArgumentNullException.ThrowIfNull(identity);
            if (identity == UInt160.Zero)
                throw new ArgumentException("An invocation identity cannot be zero.", nameof(identity));
        }

        private static bool MatchesContract(ExecutionContextState? state, UInt160 contract) =>
            state?.Contract is not null && state.ScriptHash == contract && state.Contract.Hash == contract;

        internal IDisposable EnterAccount(UInt160 accountId)
        {
            RequireIdentity(accountId);
            if (_accounts.ContainsKey(accountId))
                throw new InvalidOperationException("The SmartAccount is already executing an authorized transition.");
            var lease = new AccountLease(this, accountId);
            _accounts.Add(lease.Identity, lease);
            _scopes.Push(lease);
            return lease;
        }

        private AccountLease RequireAccount(UInt160 accountId)
        {
            RequireIdentity(accountId);
            if (!_accounts.TryGetValue(accountId, out var account) ||
                !_scopes.TryPeek(out var active) || !ReferenceEquals(active.Account, account))
                throw new InvalidOperationException("A grant requires the currently locked SmartAccount.");
            return account;
        }

        private static void RequireAnchor(UInt160 contract, ExecutionContextState anchor)
        {
            RequireIdentity(contract);
            ArgumentNullException.ThrowIfNull(anchor);
            if (!MatchesContract(anchor, contract))
                throw new ArgumentException("The grant must identify its loaded contract invocation.", nameof(anchor));
        }

        private static bool ValidPhase(SmartAccountModuleKind kind, SmartAccountCallbackPhase phase) => kind switch
        {
            SmartAccountModuleKind.Verifier => phase is SmartAccountCallbackPhase.Validation or SmartAccountCallbackPhase.PostExecute
                or SmartAccountCallbackPhase.Configuration or SmartAccountCallbackPhase.Cleanup,
            SmartAccountModuleKind.Hook => phase is SmartAccountCallbackPhase.PreExecute or SmartAccountCallbackPhase.PostExecute
                or SmartAccountCallbackPhase.Configuration or SmartAccountCallbackPhase.Cleanup,
            _ => false
        };

        internal IDisposable EnterModule(UInt160 accountId, SmartAccountModuleKind kind, UInt160 module,
            SmartAccountCallbackPhase phase, ExecutionContextState anchor, IReadOnlyList<UInt160>? children = null)
        {
            AccountLease account = RequireAccount(accountId);
            RequireAnchor(module, anchor);
            if (!ValidPhase(kind, phase) || NativeContract.IsNative(module) || module == SmartAccountProtocol.ServiceHash)
                throw new ArgumentException("The module kind, phase or native identity is invalid.");
            int maximum = kind == SmartAccountModuleKind.Verifier ? 3 : 8;
            if (children is not null && (children.Count > maximum ||
                (children.Count > 0 && phase is SmartAccountCallbackPhase.Configuration or SmartAccountCallbackPhase.Cleanup)))
                throw new ArgumentException("The callback phase or roster size does not permit delegation.", nameof(children));
            HashSet<UInt160> roster = new();
            if (children is not null)
                foreach (UInt160 child in children)
                {
                    if (child is null || child == UInt160.Zero || child == module || child == SmartAccountProtocol.ServiceHash ||
                        NativeContract.IsNative(child) || !roster.Add(Copy(child)))
                        throw new ArgumentException("The child roster contains an invalid or duplicate identity.", nameof(children));
                }
            var lease = new GrantLease(this, account, module, anchor, kind, phase, roster);
            _scopes.Push(lease);
            return lease;
        }

        internal IDisposable EnterTarget(UInt160 accountId, UInt160 target, ExecutionContextState anchor)
        {
            AccountLease account = RequireAccount(accountId);
            RequireAnchor(target, anchor);
            var lease = new GrantLease(this, account, target, anchor, null, null, new());
            _scopes.Push(lease);
            return lease;
        }

        private GrantLease? CurrentGrant(UInt160 accountId) =>
            accountId is not null && _scopes.TryPeek(out var active) && active is GrantLease grant &&
            grant.Account.Identity == accountId ? grant : null;

        private static ExecutionContextState? QueryCaller(ApplicationEngine engine) =>
            engine.State != VMState.FAULT && engine.CurrentScriptHash == SmartAccountProtocol.ServiceHash
                ? engine.CurrentContext!.GetState<ExecutionContextState>().CallingContext?.GetState<ExecutionContextState>()
                : null;

        internal bool IsModuleAuthorized(ApplicationEngine engine, UInt160 accountId, SmartAccountModuleKind kind,
            UInt160 module, SmartAccountCallbackPhase phase)
        {
            if (!ValidPhase(kind, phase) ||
                !(engine.Trigger == TriggerType.Application ||
                  (engine.Trigger == TriggerType.Verification && phase == SmartAccountCallbackPhase.Validation)))
                return false;
            GrantLease? grant = CurrentGrant(accountId);
            ExecutionContextState? caller = QueryCaller(engine);
            if (grant is null || grant.Kind != kind || grant.Phase != phase || !MatchesContract(caller, module))
                return false;
            if (ReferenceEquals(caller, grant.Anchor))
                return module == grant.Contract;
            // Only the root's actual invocation can delegate one direct call to a
            // registered leaf. Hash equality alone does not identify that invocation.
            return grant.Children.Contains(module) && MatchesContract(grant.Anchor, grant.Contract) &&
                ReferenceEquals(caller!.CallingContext?.GetState<ExecutionContextState>(), grant.Anchor);
        }

        internal bool IsTargetAuthorized(ApplicationEngine engine, UInt160 accountId)
        {
            GrantLease? grant = CurrentGrant(accountId);
            ExecutionContextState? caller = QueryCaller(engine);
            return engine.Trigger == TriggerType.Application && grant is { Kind: null } &&
                ReferenceEquals(caller, grant.Anchor) && MatchesContract(caller, grant.Contract);
        }

        internal bool IsWitnessAuthorized(ApplicationEngine engine, UInt160 accountAddress)
        {
            if (engine.State == VMState.FAULT || engine.Trigger != TriggerType.Application ||
                !_scopes.TryPeek(out var active) || active is not GrantLease { Kind: null } grant)
                return false;
            var current = engine.CurrentContext?.GetState<ExecutionContextState>();
            return grant.Account.Address == accountAddress && ReferenceEquals(current, grant.Anchor) &&
                MatchesContract(current, grant.Contract);
        }

        private void Release(Lease lease)
        {
            if (!ReferenceEquals(lease.Generation, _generation)) return;
            if (!_scopes.TryPeek(out var active) || !ReferenceEquals(active, lease))
                throw new InvalidOperationException("Invocation scopes must be released in reverse order.");
            _scopes.Pop();
            if (lease is AccountLease account) _accounts.Remove(account.Identity);
        }

        internal void Reset()
        {
            _scopes.Clear();
            _accounts.Clear();
            _generation = new();
        }
    }
}
