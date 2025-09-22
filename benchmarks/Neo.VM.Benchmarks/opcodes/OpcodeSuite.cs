// Copyright (C) 2015-2025 The Neo Project.
//
// OpcodeSuite.cs file belongs to the neo project and is free
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
using System.IO;

namespace Neo.VM.Benchmark.OpCode
{
    public class OpcodeSuite : VmBenchmarkSuite
    {
        private BenchmarkResultRecorder? _recorder;
        private string? _artifactPath;
        private string? _coveragePath;

        [GlobalSetup]
        public void SuiteSetup()
        {
            _recorder = new BenchmarkResultRecorder();
            BenchmarkExecutionContext.CurrentRecorder = _recorder;
            var root = Environment.GetEnvironmentVariable("NEO_BENCHMARK_ARTIFACTS")
                       ?? Path.Combine(AppContext.BaseDirectory, "BenchmarkArtifacts");
            Directory.CreateDirectory(root);
            _artifactPath = Path.Combine(root, $"opcode-metrics-{DateTime.UtcNow:yyyyMMddHHmmss}.csv");
            _coveragePath = Path.Combine(root, "opcode-coverage.csv");
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
            BenchmarkArtifactRegistry.RegisterMetrics(BenchmarkComponent.Opcode, _artifactPath);

            if (!string.IsNullOrEmpty(_coveragePath))
            {
                var missing = OpcodeCoverageReport.GetUncoveredOpcodes();
                OpcodeCoverageReport.WriteCoverageTable(_coveragePath);
                if (missing.Count > 0)
                {
                    Console.WriteLine($"[OpcodeSuite] Missing coverage for {missing.Count} opcodes: {string.Join(", ", missing)}");
                }
                BenchmarkArtifactRegistry.RegisterCoverage("opcode-coverage", _coveragePath);
            }
        }

        protected override IEnumerable<VmBenchmarkCase> GetCases() => OpcodeScenarioFactory.CreateCases();
    }
}
