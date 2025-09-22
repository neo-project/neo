// Copyright (C) 2015-2025 The Neo Project.
//
// BenchmarkResultRecorder.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Collections.Concurrent;
using System.Linq;

namespace Neo.VM.Benchmark.Infrastructure
{
    /// <summary>
    /// Captures benchmark metrics for VM scenarios with awareness of component, operation kind and workload profile.
    /// </summary>
    public sealed class BenchmarkResultRecorder
    {
        private readonly ConcurrentDictionary<BenchmarkMeasurementKey, MeasurementAccumulator> _measurements = new();

        public void RecordInstruction(VM.OpCode opcode, TimeSpan elapsed, long allocatedBytes, int stackDepth, int altStackDepth, long gasConsumed)
        {
            Record(BenchmarkOperationKind.Instruction, opcode.ToString(), elapsed, allocatedBytes, stackDepth, altStackDepth, gasConsumed);
        }

        public void RecordSyscall(string name, TimeSpan elapsed, long allocatedBytes, int stackDepth, int altStackDepth, long gasConsumed)
        {
            Record(BenchmarkOperationKind.Syscall, name, elapsed, allocatedBytes, stackDepth, altStackDepth, gasConsumed);
        }

        public void RecordNativeMethod(string name, TimeSpan elapsed, long allocatedBytes, int stackDepth, int altStackDepth, long gasConsumed)
        {
            Record(BenchmarkOperationKind.NativeMethod, name, elapsed, allocatedBytes, stackDepth, altStackDepth, gasConsumed);
        }

        public void Reset()
        {
            _measurements.Clear();
        }

        public IReadOnlyCollection<BenchmarkMeasurement> Snapshot()
        {
            return _measurements.Select(static kvp => kvp.Key.ToMeasurement(kvp.Value)).ToArray();
        }

        private void Record(BenchmarkOperationKind kind, string operationId, TimeSpan elapsed, long allocatedBytes, int stackDepth, int altStackDepth, long gasConsumed)
        {
            var caseInfo = BenchmarkExecutionContext.CurrentCase;
            var variant = BenchmarkExecutionContext.CurrentVariant;
            if (caseInfo is null || variant is null)
                return;

            var profile = BenchmarkExecutionContext.CurrentProfile ?? caseInfo.Profile;

            var key = new BenchmarkMeasurementKey(
                caseInfo.Component,
                caseInfo.Id,
                operationId,
                kind,
                caseInfo.Complexity,
                variant.Value,
                profile);

            _measurements.AddOrUpdate(
                key,
                static (_, arg) => MeasurementAccumulator.Create(arg.Elapsed, arg.AllocatedBytes, arg.StackDepth, arg.AltStackDepth, arg.GasConsumed),
                static (_, accumulator, arg) => accumulator.Add(arg.Elapsed, arg.AllocatedBytes, arg.StackDepth, arg.AltStackDepth, arg.GasConsumed),
                (Elapsed: elapsed, AllocatedBytes: allocatedBytes, StackDepth: stackDepth, AltStackDepth: altStackDepth, GasConsumed: gasConsumed));
        }

        private readonly record struct MeasurementAccumulator(long Count, TimeSpan Total, long AllocatedBytes, long StackDepthTotal, int MaxStackDepth, long AltStackDepthTotal, int MaxAltStackDepth, long GasConsumedTotal)
        {
            public static MeasurementAccumulator Create(TimeSpan elapsed, long allocatedBytes, int stackDepth, int altStackDepth, long gasConsumed)
            {
                return new MeasurementAccumulator(1, elapsed, Math.Max(allocatedBytes, 0), stackDepth, stackDepth, altStackDepth, altStackDepth, Math.Max(gasConsumed, 0));
            }

            public MeasurementAccumulator Add(TimeSpan elapsed, long allocatedBytes, int stackDepth, int altStackDepth, long gasConsumed)
            {
                return new MeasurementAccumulator(
                    Count + 1,
                    Total + elapsed,
                    AllocatedBytes + Math.Max(allocatedBytes, 0),
                    StackDepthTotal + Math.Max(stackDepth, 0),
                    Math.Max(MaxStackDepth, stackDepth),
                    AltStackDepthTotal + Math.Max(altStackDepth, 0),
                    Math.Max(MaxAltStackDepth, altStackDepth),
                    GasConsumedTotal + Math.Max(gasConsumed, 0));
            }
        }

        private readonly record struct BenchmarkMeasurementKey(
            BenchmarkComponent Component,
            string ScenarioId,
            string OperationId,
            BenchmarkOperationKind OperationKind,
            ScenarioComplexity Complexity,
            BenchmarkVariant Variant,
            ScenarioProfile Profile)
        {
            public BenchmarkMeasurement ToMeasurement(MeasurementAccumulator accumulator)
            {
                return new BenchmarkMeasurement(
                    Component,
                    ScenarioId,
                    OperationId,
                    OperationKind,
                    Complexity,
                    Variant,
                    Profile,
                    accumulator.Count,
                    accumulator.Total,
                    accumulator.AllocatedBytes,
                    accumulator.StackDepthTotal,
                    accumulator.MaxStackDepth,
                    accumulator.AltStackDepthTotal,
                    accumulator.MaxAltStackDepth,
                    accumulator.GasConsumedTotal);
            }
        }

        public readonly record struct BenchmarkMeasurement(
            BenchmarkComponent Component,
            string ScenarioId,
            string OperationId,
            BenchmarkOperationKind OperationKind,
            ScenarioComplexity Complexity,
            BenchmarkVariant Variant,
            ScenarioProfile Profile,
            long Count,
            TimeSpan Total,
            long AllocatedBytes,
            long StackDepthTotal,
            int MaxStackDepth,
            long AltStackDepthTotal,
            int MaxAltStackDepth,
            long GasConsumedTotal)
        {
            private double TotalNanoseconds => Total.TotalMilliseconds * 1_000_000.0;
            public double TotalMicroseconds => TotalNanoseconds / 1000.0;
            public double AverageNanoseconds => Count > 0 ? TotalNanoseconds / Count : 0;

            public double NanosecondsPerIteration
            {
                get
                {
                    var iterations = TotalIterations;
                    return TotalNanoseconds / iterations;
                }
            }

            public double NanosecondsPerByte
            {
                get
                {
                    var totalBytes = TotalDataBytes;
                    return totalBytes > 0 ? TotalNanoseconds / totalBytes : 0;
                }
            }

            public double NanosecondsPerElement
            {
                get
                {
                    var totalElements = TotalElements;
                    return totalElements > 0 ? TotalNanoseconds / totalElements : 0;
                }
            }

            public long TotalIterations => Math.Max(1, Profile.Iterations);
            public long TotalDataBytes => (long)Math.Max(0, Profile.DataLength) * TotalIterations;
            public long TotalElements => (long)Math.Max(0, Profile.CollectionLength) * TotalIterations;
            public long TotalAllocatedBytes => AllocatedBytes;

            public double AllocatedBytesPerIteration => TotalIterations > 0 ? (double)TotalAllocatedBytes / TotalIterations : 0;
            public double AllocatedBytesPerByte => TotalDataBytes > 0 ? (double)TotalAllocatedBytes / TotalDataBytes : 0;
            public double AllocatedBytesPerElement => TotalElements > 0 ? (double)TotalAllocatedBytes / TotalElements : 0;

            public double AverageStackDepth => Count > 0 ? (double)StackDepthTotal / Count : 0;
            public int PeakStackDepth => MaxStackDepth;
            public double AverageAltStackDepth => Count > 0 ? (double)AltStackDepthTotal / Count : 0;
            public int PeakAltStackDepth => MaxAltStackDepth;
            public long TotalGasConsumed => GasConsumedTotal;
            public double GasPerIteration => TotalIterations > 0 ? (double)TotalGasConsumed / TotalIterations : 0;
        }
    }
}
