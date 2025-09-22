// Copyright (C) 2015-2025 The Neo Project.
//
// BenchmarkExecutionSummary.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System.Globalization;
using System.IO;
using System.Linq;

namespace Neo.VM.Benchmark.Infrastructure
{
    /// <summary>
    /// Handles post-run persistence of recorded benchmark statistics.
    /// </summary>
    internal sealed class BenchmarkExecutionSummary
    {
        private readonly BenchmarkResultRecorder _recorder;
        private readonly string _artifactPath;

        public BenchmarkExecutionSummary(BenchmarkResultRecorder recorder, string artifactPath)
        {
            _recorder = recorder;
            _artifactPath = artifactPath;
        }

        public void Write()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_artifactPath)!);
            using var writer = new StreamWriter(_artifactPath, append: false);
            writer.WriteLine("Component,ScenarioId,OperationKind,OperationId,Complexity,Variant,Iterations,DataLength,CollectionLength,TotalIterations,TotalDataBytes,TotalElements,Count,TotalMicroseconds,AverageNanoseconds,NanosecondsPerIteration,NanosecondsPerByte,NanosecondsPerElement,TotalAllocatedBytes,AllocatedBytesPerIteration,AllocatedBytesPerByte,AllocatedBytesPerElement,AverageStackDepth,PeakStackDepth,AverageAltStackDepth,PeakAltStackDepth,TotalGasConsumed,GasPerIteration");
            foreach (var measurement in _recorder
                         .Snapshot()
                         .OrderBy(static m => m.Component)
                         .ThenBy(static m => m.OperationKind)
                         .ThenBy(static m => m.OperationId)
                         .ThenBy(static m => m.Complexity)
                         .ThenBy(static m => m.Variant))
            {
                writer.WriteLine(string.Join(',',
                    measurement.Component,
                    measurement.ScenarioId,
                    measurement.OperationKind,
                    measurement.OperationId,
                    measurement.Complexity,
                    measurement.Variant,
                    measurement.Profile.Iterations.ToString(CultureInfo.InvariantCulture),
                    measurement.Profile.DataLength.ToString(CultureInfo.InvariantCulture),
                    measurement.Profile.CollectionLength.ToString(CultureInfo.InvariantCulture),
                    measurement.TotalIterations.ToString(CultureInfo.InvariantCulture),
                    measurement.TotalDataBytes.ToString(CultureInfo.InvariantCulture),
                    measurement.TotalElements.ToString(CultureInfo.InvariantCulture),
                    measurement.Count.ToString(CultureInfo.InvariantCulture),
                    measurement.TotalMicroseconds.ToString("F2", CultureInfo.InvariantCulture),
                    measurement.AverageNanoseconds.ToString("F2", CultureInfo.InvariantCulture),
                    measurement.NanosecondsPerIteration.ToString("F2", CultureInfo.InvariantCulture),
                    measurement.NanosecondsPerByte.ToString("F4", CultureInfo.InvariantCulture),
                    measurement.NanosecondsPerElement.ToString("F4", CultureInfo.InvariantCulture),
                    measurement.TotalAllocatedBytes.ToString(CultureInfo.InvariantCulture),
                    measurement.AllocatedBytesPerIteration.ToString("F2", CultureInfo.InvariantCulture),
                    measurement.AllocatedBytesPerByte.ToString("F6", CultureInfo.InvariantCulture),
                    measurement.AllocatedBytesPerElement.ToString("F6", CultureInfo.InvariantCulture),
                    measurement.AverageStackDepth.ToString("F2", CultureInfo.InvariantCulture),
                    measurement.PeakStackDepth.ToString(CultureInfo.InvariantCulture),
                    measurement.AverageAltStackDepth.ToString("F2", CultureInfo.InvariantCulture),
                    measurement.PeakAltStackDepth.ToString(CultureInfo.InvariantCulture),
                    measurement.TotalGasConsumed.ToString(CultureInfo.InvariantCulture),
                    measurement.GasPerIteration.ToString("F4", CultureInfo.InvariantCulture)));

            }
        }
    }
}
