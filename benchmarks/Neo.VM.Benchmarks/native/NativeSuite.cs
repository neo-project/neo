// Copyright (C) 2015-2025 The Neo Project.
//
// NativeSuite.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using BenchmarkDotNet.Attributes;
using Neo.VM.Benchmark.Infrastructure;
using System;
using System.Collections.Generic;
using System.IO;

namespace Neo.VM.Benchmark.Native
{
    public class NativeSuite : VmBenchmarkSuite
    {
        private BenchmarkResultRecorder? _recorder;
        private string? _artifactPath;

        [GlobalSetup]
        public void SuiteSetup()
        {
            _recorder = new BenchmarkResultRecorder();
            BenchmarkExecutionContext.CurrentRecorder = _recorder;
            var root = Environment.GetEnvironmentVariable("NEO_BENCHMARK_ARTIFACTS")
                       ?? Path.Combine(AppContext.BaseDirectory, "BenchmarkArtifacts");
            Directory.CreateDirectory(root);
            _artifactPath = Path.Combine(root, $"native-metrics-{DateTime.UtcNow:yyyyMMddHHmmss}.csv");
        }

        [IterationSetup]
        public void IterationSetup()
        {
            if (_recorder is not null)
                BenchmarkExecutionContext.CurrentRecorder = _recorder;
        }

        [GlobalCleanup]
        public void SuiteCleanup()
        {
            if (_recorder is null || string.IsNullOrEmpty(_artifactPath))
                return;

            var summary = new BenchmarkExecutionSummary(_recorder, _artifactPath);
            summary.Write();
            BenchmarkArtifactRegistry.RegisterMetrics(BenchmarkComponent.NativeContract, _artifactPath);

            var coverageRoot = Environment.GetEnvironmentVariable("NEO_BENCHMARK_ARTIFACTS")
                               ?? Path.Combine(AppContext.BaseDirectory, "BenchmarkArtifacts");
            var coveragePath = Path.Combine(coverageRoot, "native-missing.csv");
            var missingNative = NativeCoverageReport.GetMissing();
            InteropCoverageReport.WriteReport(coveragePath, System.Array.Empty<string>(), missingNative);
            BenchmarkArtifactRegistry.RegisterCoverage("native-missing", coveragePath);

        }

        protected override IEnumerable<VmBenchmarkCase> GetCases()
        {
            return NativeScenarioFactory.CreateCases();
        }
    }
}
