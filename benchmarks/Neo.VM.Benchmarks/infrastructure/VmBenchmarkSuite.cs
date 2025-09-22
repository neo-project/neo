// Copyright (C) 2015-2025 The Neo Project.
//
// VmBenchmarkSuite.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using BenchmarkDotNet.Attributes;
using System;

namespace Neo.VM.Benchmark.Infrastructure
{
    /// <summary>
    /// Base class for BenchmarkDotNet suites that execute VM scenarios.
    /// </summary>
    public abstract class VmBenchmarkSuite
    {
        [ParamsSource(nameof(Cases))]
        public VmBenchmarkCase Case { get; set; } = default!;

        protected abstract IEnumerable<VmBenchmarkCase> GetCases();

        public IEnumerable<VmBenchmarkCase> Cases() => GetCases();

        [Benchmark(Baseline = true)]
        public void Baseline() => RunVariant(BenchmarkVariant.Baseline);

        [Benchmark]
        public void Single() => RunVariant(BenchmarkVariant.Single);

        [Benchmark]
        public void Saturated() => RunVariant(BenchmarkVariant.Saturated);

        private void RunVariant(BenchmarkVariant variant)
        {
            if (Case is null)
                throw new InvalidOperationException("Benchmark case not initialized.");

            BenchmarkExecutionContext.CurrentCase = Case;
            BenchmarkExecutionContext.CurrentVariant = variant;
            try
            {
                Case.Scenario.Execute(variant);
            }
            finally
            {
                BenchmarkExecutionContext.ClearVariant();
            }
        }
    }
}
