// Copyright (C) 2015-2025 The Neo Project.
//
// NativeContractMethodBenchmarks.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using System;
using System.Collections.Generic;

namespace Neo.Benchmarks.NativeContracts
{
    [MemoryDiagnoser]
    public class NativeContractMethodBenchmarks : IConfigSource, IDisposable
    {
        private readonly NativeContractBenchmarkSuite _suite;
        private NativeContractBenchmarkInvoker _invoker;

        public NativeContractMethodBenchmarks()
        {
            _suite = NativeContractBenchmarkSuite.CreateDefault();
            Config = new NativeContractBenchmarkConfig(_suite);
        }

        public IConfig Config { get; }

        [ParamsSource(nameof(GetCases))]
        public NativeContractBenchmarkCase Case { get; set; }

        public IEnumerable<NativeContractBenchmarkCase> GetCases() => _suite.Cases;

        [GlobalSetup]
        public void GlobalSetup()
        {
            if (Case is null)
                throw new InvalidOperationException("Benchmark case not set. Ensure discovery produced at least one scenario.");
            _invoker = _suite.CreateInvoker(Case);
        }

        [Benchmark(Description = "Invoke native contract method")]
        public object Execute() => _invoker.Invoke();

        [GlobalCleanup]
        public void GlobalCleanup()
        {
            _invoker = null;
        }

        public void Dispose()
        {
            _suite.Dispose();
        }
    }
}
