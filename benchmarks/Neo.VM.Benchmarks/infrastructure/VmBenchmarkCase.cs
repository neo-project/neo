// Copyright (C) 2015-2025 The Neo Project.
//
// VmBenchmarkCase.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

namespace Neo.VM.Benchmark.Infrastructure
{
    /// <summary>
    /// Aggregates the metadata and scenario used by BenchmarkDotNet parameter sources.
    /// </summary>
    public sealed class VmBenchmarkCase
    {
        public string Id { get; }
        public BenchmarkComponent Component { get; }
        public ScenarioComplexity Complexity { get; }
        public ScenarioProfile Profile { get; }
        public IVmScenario Scenario { get; }

        public VmBenchmarkCase(string id, BenchmarkComponent component, ScenarioComplexity complexity, IVmScenario scenario, ScenarioProfile? profileOverride = null)
        {
            Id = id;
            Component = component;
            Complexity = complexity;
            Profile = profileOverride ?? ScenarioProfile.For(complexity);
            Scenario = scenario;
        }

        public override string ToString()
        {
            return $"{Component}:{Id} ({Complexity})";
        }
    }
}
