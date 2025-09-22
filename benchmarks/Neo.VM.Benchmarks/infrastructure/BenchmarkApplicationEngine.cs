// Copyright (C) 2015-2025 The Neo Project.
//
// BenchmarkApplicationEngine.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.Persistence.Providers;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.VM;
using System;
using System.Collections;
using System.Diagnostics;
using System.Reflection;

namespace Neo.VM.Benchmark.Infrastructure
{
    /// <summary>
    /// Thin wrapper around <see cref="ApplicationEngine"/> that records syscall and native-method timings.
    /// </summary>
    internal sealed class BenchmarkApplicationEngine : ApplicationEngine
    {
        private static readonly MethodInfo? s_getContractMethods = typeof(NativeContract)
            .GetMethod("GetContractMethods", BindingFlags.Instance | BindingFlags.NonPublic);

        private readonly StoreCache _snapshot;

        private BenchmarkApplicationEngine(
            TriggerType trigger,
            StoreCache snapshot,
            IVerifiable? container,
            Block persistingBlock,
            ProtocolSettings settings,
            long gas,
            IDiagnostic? diagnostic,
            JumpTable jumpTable)
            : base(trigger, container, snapshot, persistingBlock, settings, gas, diagnostic, jumpTable)
        {
            _snapshot = snapshot;
        }

        public BenchmarkResultRecorder? Recorder { get; set; }

        public static BenchmarkApplicationEngine Create(
            ProtocolSettings? settings = null,
            long? gas = null,
            IVerifiable? container = null,
            StoreCache? snapshot = null,
            TriggerType trigger = TriggerType.Application,
            Block? persistingBlock = null,
            IDiagnostic? diagnostic = null)
        {
            settings = BenchmarkProtocolSettings.ResolveSettings(settings);
            snapshot ??= NativeBenchmarkStateFactory.CreateSnapshot();
            persistingBlock ??= CreateBenchmarkBlock();
            var index = persistingBlock.Index;
            var jumpTable = settings.IsHardforkEnabled(Hardfork.HF_Echidna, index) ? DefaultJumpTable : NotEchidnaJumpTable;
            return new BenchmarkApplicationEngine(trigger, snapshot, container, persistingBlock, settings, gas ?? TestModeGas, diagnostic, jumpTable);
        }

        protected override void OnSysCall(InteropDescriptor descriptor)
        {
            if (Recorder is null)
            {
                base.OnSysCall(descriptor);
                return;
            }

            var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
            var gasBefore = FeeConsumed;
            var stopwatch = Stopwatch.StartNew();
            string? nativeMethodName = null;
            if (descriptor == System_Contract_CallNative)
            {
                nativeMethodName = TryResolveNativeCall();
            }

            try
            {
                base.OnSysCall(descriptor);
            }
            finally
            {
                stopwatch.Stop();
                var allocatedAfter = GC.GetAllocatedBytesForCurrentThread();
                var allocatedBytes = Math.Max(0, allocatedAfter - allocatedBefore);
                var gasAfter = FeeConsumed;
                var gasDelta = Math.Max(0, gasAfter - gasBefore);
                var stackDepth = CurrentContext?.EvaluationStack.Count ?? 0;
                var altStackDepth = 0;
                if (nativeMethodName is not null)
                {
                    Recorder.RecordNativeMethod(nativeMethodName, stopwatch.Elapsed, allocatedBytes, stackDepth, altStackDepth, gasDelta);
                }
                else
                {
                    Recorder.RecordSyscall(descriptor.Name, stopwatch.Elapsed, allocatedBytes, stackDepth, altStackDepth, gasDelta);
                }
            }
        }

        private string? TryResolveNativeCall()
        {
            var context = CurrentContext;
            if (context is null)
                return null;

            var contract = NativeContract.GetContract(CurrentScriptHash);
            if (contract is null)
                return null;

            if (s_getContractMethods is null)
                return contract.Name;

            if (s_getContractMethods.Invoke(contract, new object[] { this }) is not IDictionary methods)
                return contract.Name;

            var pointer = context.InstructionPointer;
            object? metadata = null;

            if (methods is IDictionary dict && dict.Contains(pointer))
            {
                metadata = dict[pointer];
            }
            else
            {
                foreach (DictionaryEntry entry in methods)
                {
                    if (entry.Key is int key && key == pointer)
                    {
                        metadata = entry.Value;
                        break;
                    }
                }
            }

            var name = metadata is null
                ? "unknown"
                : metadata.GetType().GetProperty("Name", BindingFlags.Public | BindingFlags.Instance)?.GetValue(metadata) as string ?? "unknown";

            return string.IsNullOrEmpty(name)
                ? contract.Name
                : $"{contract.Name}.{name}";
        }

        public override void Dispose()
        {
            base.Dispose();
            _snapshot.Dispose();
        }

        private static Block CreateBenchmarkBlock()
        {
            return new Block
            {
                Header = new Header
                {
                    Version = 0,
                    PrevHash = UInt256.Zero,
                    MerkleRoot = UInt256.Zero,
                    Timestamp = (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    Index = 1,
                    NextConsensus = UInt160.Zero,
                    Witness = Witness.Empty
                },
                Transactions = Array.Empty<Transaction>()
            };
        }
    }
}
