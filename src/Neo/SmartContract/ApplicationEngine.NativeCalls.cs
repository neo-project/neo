// Copyright (C) 2015-2026 The Neo Project.
//
// ApplicationEngine.NativeCalls.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Extensions;
using Neo.SmartContract.Native;
using Neo.VM;
using Neo.VM.Types;
using System;

namespace Neo.SmartContract
{
    partial class ApplicationEngine
    {
        /// <summary>
        /// Originates a bounded dynamic callback from the current native contract.
        /// Results retain their exact VM type; a Void callee produces Null. Protocol
        /// callers must validate callback ABIs and results without Boolean coercion.
        /// </summary>
        internal ContractTask<StackItem> CallFromNativeContractWithGasLimitAsync(
            UInt160 callingScriptHash, UInt160 contractHash, string method, CallFlags flags,
            long gasLimit, params StackItem[] args)
        {
            ArgumentNullException.ThrowIfNull(callingScriptHash);
            ArgumentNullException.ThrowIfNull(contractHash);
            ArgumentNullException.ThrowIfNull(method);
            ArgumentNullException.ThrowIfNull(args);
            if (!IsHardforkEnabled(Hardfork.HF_SmartAccountV1))
                throw new InvalidOperationException("Bounded native callbacks require SmartAccount activation.");
            if (callingScriptHash != CurrentScriptHash || !NativeContract.IsNative(callingScriptHash))
                throw new InvalidOperationException("The callback caller must be the current native contract.");
            ValidateCallFlags(System_Contract_CallWithGasLimit.RequiredCallFlags);
            // A native continuation can run during child RET, before the child's
            // post-instruction charge. Dispatch belongs to the resumed native
            // caller, while that pending RET still belongs to the child.
            bool instructionWhitelisted = _whitelisted;
            ExecutionContext context;
            try
            {
                _whitelisted = CurrentContext!.GetState<ExecutionContextState>().WhiteListed;
                AddFee(System_Contract_CallWithGasLimit.FixedPrice * _execFeeFactor, false);
                Diagnostic?.CallFromNative(contractHash, method, args);
                context = LoadBoundedContractCall(contractHash, method, flags, gasLimit, args);
            }
            finally
            {
                _whitelisted = instructionWhitelisted;
            }
            context.GetState<ExecutionContextState>().NativeCallingScriptHash = new UInt160(callingScriptHash.ToArray());
            ContractTask<StackItem> task = new();
            contractTasks.Add(context, task.GetAwaiter());
            return task;
        }
        /// <summary>Dispatches an exact-type native call without allocating a new child budget.</summary>
        internal ContractTask<StackItem> CallFromNativeContractRawAsync(UInt160 caller, UInt160 target,
            string method, CallFlags flags, bool requireInheritedBudget, params StackItem[] args)
        {
            if (!IsHardforkEnabled(Hardfork.HF_SmartAccountV1) || caller != CurrentScriptHash || !NativeContract.IsNative(caller))
                throw new InvalidOperationException("Raw native dispatch requires an activated native caller.");
            ValidateCallFlags(CallFlags.ReadStates | CallFlags.AllowCall);
            if (requireInheritedBudget && CurrentContext!.GetState<ExecutionContextState>().ContractCallGasBudget is null)
                throw new InvalidOperationException("A registry continuation must inherit a bounded root callback.");
            if (string.IsNullOrEmpty(method) || method.StartsWith('_') || (flags & ~CallFlags.All) != 0)
                throw new ArgumentException("Invalid raw native call method or flags.");
            var contract = NativeContract.ContractManagement.GetContract(SnapshotCache, target)
                ?? throw new InvalidOperationException("The target contract is not deployed.");
            var descriptor = contract.Manifest.Abi.GetMethod(method, args.Length)
                ?? throw new InvalidOperationException("The target method is not defined.");
            bool prior = _whitelisted;
            ExecutionContext context;
            try
            {
                _whitelisted = CurrentContext!.GetState<ExecutionContextState>().WhiteListed;
                AddFee(System_Contract_Call.FixedPrice * _execFeeFactor, false);
                Diagnostic?.CallFromNative(target, method, args);
                context = CallContractInternal(contract, descriptor, flags,
                    descriptor.ReturnType != ContractParameterType.Void, args);
            }
            finally { _whitelisted = prior; }
            var state = context.GetState<ExecutionContextState>();
            state.IsDynamicCall = true;
            state.NativeCallingScriptHash = new UInt160(caller.ToArray());
            ContractTask<StackItem> task = new();
            contractTasks.Add(context, task.GetAwaiter());
            return task;
        }
    }
}
