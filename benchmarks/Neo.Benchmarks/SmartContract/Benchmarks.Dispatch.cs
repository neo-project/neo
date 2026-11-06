// Copyright (C) 2015-2026 The Neo Project.
//
// Benchmarks.Dispatch.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using Neo.Persistence;
using Neo.SmartContract.Manifest;
using Neo.SmartContract.Native;
using Neo.VM;
using Neo.VM.Types;
using System.Numerics;
using System.Reflection;

namespace Neo.SmartContract.Benchmark
{
    [Config(typeof(BenchmarkConfig))]
    [MemoryDiagnoser]
    public class Benchmarks_Dispatch
    {
        private const int DispatchCount = 1000;
        private static readonly ProtocolSettings s_protocol = ProtocolSettings.Load("config.json");
        private static readonly NeoSystem s_system = new(s_protocol, (string)null);
        private static readonly Script s_dummyScript = new(new byte[] { (byte)OpCode.RET }, true);
        private static readonly InteropDescriptor s_syscallNoArgs = CreateInteropDescriptor("InteropNoArgs");
        private static readonly InteropDescriptor s_syscallOneArg = CreateInteropDescriptor("InteropOneArg");
        private static readonly BenchmarkNativeContract s_nativeContract = new();
        private static readonly ContractState s_nativeContractState = s_nativeContract.GetContractState(s_protocol, 0);
        private static readonly ContractMethodDescriptor s_nativeNoArgMethod = s_nativeContractState.Manifest.Abi.GetMethod("noArg", 0)!;
        private static readonly ContractMethodDescriptor s_nativeOneArgMethod = s_nativeContractState.Manifest.Abi.GetMethod("oneArg", 1)!;
        private static readonly StackItem s_integerArg = new Integer(42);

        [Benchmark(OperationsPerInvoke = DispatchCount)]
        public int Syscall_NoArgs()
        {
            using var snapshot = s_system.GetSnapshotCache();
            using var engine = new BenchmarkEngine(snapshot);
            engine.PrepareInteropContext();

            int result = 0;
            for (int i = 0; i < DispatchCount; i++)
                result = engine.InvokeInterop(s_syscallNoArgs);

            return result;
        }

        [Benchmark(OperationsPerInvoke = DispatchCount)]
        public int Syscall_OneArg()
        {
            using var snapshot = s_system.GetSnapshotCache();
            using var engine = new BenchmarkEngine(snapshot);
            engine.PrepareInteropContext();

            int result = 0;
            for (int i = 0; i < DispatchCount; i++)
                result = engine.InvokeInterop(s_syscallOneArg, s_integerArg);

            return result;
        }

        [Benchmark(OperationsPerInvoke = DispatchCount)]
        public int Native_NoArgs()
        {
            using var snapshot = s_system.GetSnapshotCache();
            using var engine = new BenchmarkEngine(snapshot);
            engine.PrepareNativeContext(s_nativeContractState, s_nativeNoArgMethod);

            int result = 0;
            for (int i = 0; i < DispatchCount; i++)
                result = engine.InvokeNative();

            return result;
        }

        [Benchmark(OperationsPerInvoke = DispatchCount)]
        public int Native_OneArg()
        {
            using var snapshot = s_system.GetSnapshotCache();
            using var engine = new BenchmarkEngine(snapshot);
            engine.PrepareNativeContext(s_nativeContractState, s_nativeOneArgMethod);

            int result = 0;
            for (int i = 0; i < DispatchCount; i++)
                result = engine.InvokeNative(s_integerArg);

            return result;
        }

        private static InteropDescriptor CreateInteropDescriptor(string methodName)
        {
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var method = typeof(BenchmarkEngine).GetMethod(methodName, flags)!;

            return new InteropDescriptor
            {
                Name = $"Benchmark.{methodName}",
                Handler = method,
                FixedPrice = 0,
                RequiredCallFlags = CallFlags.None
            };
        }

        private sealed class BenchmarkConfig : ManualConfig
        {
            public BenchmarkConfig()
            {
                Options |= ConfigOptions.DisableOptimizationsValidator;
            }
        }

        private sealed class BenchmarkEngine(DataCache snapshot) : ApplicationEngine(TriggerType.Application, null, snapshot, s_system.GenesisBlock, s_protocol, long.MaxValue / FeeFactor)
        {
            public void PrepareInteropContext()
            {
                LoadScript(s_dummyScript, configureState: state => state.CallFlags = CallFlags.All);
            }

            public void PrepareNativeContext(ContractState contract, ContractMethodDescriptor method)
            {
                LoadScript(contract.Script, initialPosition: method.Offset + 1, configureState: state =>
                {
                    state.CallFlags = CallFlags.All;
                    state.ScriptHash = contract.Hash;
                    state.Contract = contract;
                });
            }

            public int InvokeInterop(InteropDescriptor descriptor, params StackItem[] args)
            {
                PushArguments(args);
                OnSysCall(descriptor);
                return PopIntegerResult();
            }

            public int InvokeNative(params StackItem[] args)
            {
                PushArguments(args);
                CallNativeContract(0);
                return PopIntegerResult();
            }

            private void PushArguments(StackItem[] args)
            {
                for (int i = args.Length - 1; i >= 0; i--)
                    CurrentContext!.EvaluationStack.Push(args[i]);
            }

            private int PopIntegerResult()
            {
                return (int)CurrentContext!.EvaluationStack.Pop().GetInteger();
            }

            private int InteropNoArgs() => 1;

            private int InteropOneArg(int value) => value + 1;
        }

        private sealed class BenchmarkNativeContract : NativeContract
        {
            [ContractMethod]
            private static int NoArg() => 1;

            [ContractMethod]
            private static int OneArg(int value) => value + 1;
        }
    }
}
