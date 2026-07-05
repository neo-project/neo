// Copyright (C) 2015-2025 The Neo Project.
//
// NativeContractBenchmarkInvoker.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.VM;
using System;
using System.Reflection;
using System.Runtime.ExceptionServices;

namespace Neo.Benchmarks.NativeContracts
{
    /// <summary>
    /// Executes a single benchmark case.
    /// </summary>
    public sealed class NativeContractBenchmarkInvoker
    {
        private static readonly object NullSentinel = new();
        private static readonly byte[] EntryScript = new[] { (byte)OpCode.RET };

        private readonly NativeContractBenchmarkCase _case;
        private readonly NativeContractBenchmarkContext _context;
        private ContractState _contractState;
        private static readonly PropertyInfo NativeCallingScriptHashProperty =
            typeof(ExecutionContextState).GetProperty("NativeCallingScriptHash", BindingFlags.Instance | BindingFlags.NonPublic);

        public NativeContractBenchmarkInvoker(NativeContractBenchmarkCase benchmarkCase, NativeContractBenchmarkContext context)
        {
            _case = benchmarkCase ?? throw new ArgumentNullException(nameof(benchmarkCase));
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public object Invoke()
        {
            StoreCache snapshot = null;
            ApplicationEngine engine = null;
            object[] invocationArguments = null;

            try
            {
                var userArguments = _case.ArgumentFactory(_context) ?? System.Array.Empty<object>();
                invocationArguments = PrepareInvocationArguments(userArguments, ref snapshot, ref engine);
                ApplyCaseSpecificArgumentOverrides(invocationArguments, engine);

                var target = _case.Handler.IsStatic ? null : _case.Contract;
                var result = _case.Handler.Invoke(target, invocationArguments);
                result = UnwrapContractTask(result);
                return result ?? NullSentinel;
            }
            catch (TargetInvocationException ex) when (ex.InnerException is not null)
            {
                var fault = engine?.FaultException;
                if (fault is not null)
                {
                    Console.Error.WriteLine($"[Invoker] Engine fault: {fault}");
                    throw new InvalidOperationException($"Native contract faulted: {fault.Message}", fault);
                }
                System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
                throw;
            }
            catch (Exception ex)
            {
                var fault = engine?.FaultException;
                if (fault is not null && !ReferenceEquals(fault, ex))
                {
                    Console.Error.WriteLine($"[Invoker] Engine fault: {fault}");
                    throw new InvalidOperationException($"Native contract faulted: {fault.Message}", fault);
                }
                throw;
            }
            finally
            {
                DisposeIfNeeded(invocationArguments);
                engine?.Dispose();
                snapshot?.Dispose();
            }
        }

        private object[] PrepareInvocationArguments(object[] userArguments, ref StoreCache snapshot, ref ApplicationEngine engine)
        {
            if (_case.RequiresApplicationEngine)
            {
                snapshot = _context.GetSnapshot();
                engine = _context.CreateEngine(snapshot, _case);
                EnsureExecutionContext(engine);
                var allArgs = new object[userArguments.Length + 1];
                allArgs[0] = engine;
                Array.Copy(userArguments, 0, allArgs, 1, userArguments.Length);
                return allArgs;
            }

            if (_case.RequiresSnapshot)
            {
                snapshot = _context.GetSnapshot();
                var allArgs = new object[userArguments.Length + 1];
                allArgs[0] = snapshot;
                Array.Copy(userArguments, 0, allArgs, 1, userArguments.Length);
                return allArgs;
            }

            return userArguments;
        }

        private void EnsureExecutionContext(ApplicationEngine engine)
        {
            if (engine is null)
                return;

            var originalCache = engine.SnapshotCache;
            engine.LoadScript(EntryScript, configureState: state =>
            {
                state.CallFlags = CallFlags.All;
                state.SnapshotCache = originalCache;
            });

            var contractState = _contractState ??= _case.Contract.GetContractState(_context.ProtocolSettings, 0);
            var script = contractState.Script.ToArray();
            engine.LoadScript(script, configureState: state =>
            {
                state.Contract = contractState;
                state.ScriptHash = contractState.Hash;
                state.CallFlags = GetEffectiveCallFlags();
                state.SnapshotCache = originalCache;
            });

            ApplyNativeCallingScriptOverrides(engine);
        }

        private void DisposeIfNeeded(object[] arguments)
        {
            if (arguments is null)
                return;

            var start = (_case.RequiresApplicationEngine || _case.RequiresSnapshot) ? 1 : 0;
            for (int i = start; i < arguments.Length; i++)
            {
                if (arguments[i] is IDisposable disposable)
                    disposable.Dispose();
            }
        }

        private static object UnwrapContractTask(object result)
        {
            if (result is null)
                return null;

            var type = result.GetType();
            if (IsContractTask(type))
                return null;

            return result;
        }

        private static bool IsContractTask(Type type)
        {
            while (type is not null)
            {
                if (type.FullName is "Neo.SmartContract.ContractTask" or "Neo.SmartContract.ContractTask`1")
                    return true;
                type = type.BaseType;
            }

            return false;
        }

        private void ApplyCaseSpecificArgumentOverrides(object[] arguments, ApplicationEngine engine)
        {
            if (arguments is null)
                return;

            if (_case.ContractName == nameof(Notary) && IsCaseInsensitiveMethod("Verify"))
            {
                var tx = engine?.ScriptContainer as Transaction
                    ?? throw new InvalidOperationException("Notary.Verify benchmarks require a transaction container.");
                var signature = _context.CreateNotaryVerificationSignature(tx);
                var signatureIndex = _case.RequiresApplicationEngine ? 1 : 0;
                arguments[signatureIndex] = signature;
            }
        }

        private CallFlags GetEffectiveCallFlags()
        {
            if (_case.ContractName == nameof(ContractManagement) &&
                (IsCaseInsensitiveMethod("Deploy") || IsCaseInsensitiveMethod("Update")))
            {
                return CallFlags.All;
            }

            return _case.RequiredCallFlags;
        }

        private void ApplyNativeCallingScriptOverrides(ApplicationEngine engine)
        {
            if (engine?.CurrentContext is null || NativeCallingScriptHashProperty is null)
                return;

            var state = engine.CurrentContext.GetState<ExecutionContextState>();

            if (_case.ContractName == nameof(ContractManagement) &&
                (IsCaseInsensitiveMethod("Update") || IsCaseInsensitiveMethod("Destroy")))
            {
                if (!_context.TryGetContractManagementCaller(_case.Profile.Size, out var caller))
                    throw new InvalidOperationException($"No seeded ContractManagement caller for {_case.Profile.Size}.");
                NativeCallingScriptHashProperty.SetValue(state, caller);
            }

            if (_case.ContractName == nameof(OracleContract) && IsCaseInsensitiveMethod("Request"))
            {
                NativeCallingScriptHashProperty.SetValue(state, _context.CallbackContractHash);
            }

            if (_case.ContractName == nameof(Notary) && IsCaseInsensitiveMethod("OnNEP17Payment"))
            {
                NativeCallingScriptHashProperty.SetValue(state, NativeContract.GAS.Hash);
            }

            if (_case.ContractName == nameof(NeoToken) && IsCaseInsensitiveMethod("OnNEP17Payment"))
            {
                NativeCallingScriptHashProperty.SetValue(state, NativeContract.GAS.Hash);
            }
        }

        private bool IsCaseInsensitiveMethod(string expected)
        {
            return string.Equals(_case.MethodName, expected, StringComparison.OrdinalIgnoreCase);
        }
    }
}
