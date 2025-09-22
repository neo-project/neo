// Copyright (C) 2015-2025 The Neo Project.
//
// BenchmarkExecutionContext.cs file belongs to the neo project and is free
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
    /// Holds ambient state for benchmark execution (per-thread).
    /// </summary>
    public static class BenchmarkExecutionContext
    {
        private static readonly System.Threading.AsyncLocal<BenchmarkResultRecorder?> s_recorder = new();
        private static readonly System.Threading.AsyncLocal<VmBenchmarkCase?> s_case = new();
        private static readonly System.Threading.AsyncLocal<BenchmarkVariant?> s_variant = new();
        private static readonly System.Threading.AsyncLocal<ScenarioProfile?> s_profileOverride = new();

        public static BenchmarkResultRecorder? CurrentRecorder
        {
            get => s_recorder.Value;
            set => s_recorder.Value = value;
        }

        public static VmBenchmarkCase? CurrentCase
        {
            get => s_case.Value;
            set => s_case.Value = value;
        }

        public static BenchmarkVariant? CurrentVariant
        {
            get => s_variant.Value;
            set => s_variant.Value = value;
        }

        public static ScenarioProfile? CurrentProfile => s_profileOverride.Value ?? CurrentCase?.Profile;

        public static ScenarioComplexity? CurrentComplexity => CurrentCase?.Complexity;

        public static BenchmarkComponent? CurrentComponent => CurrentCase?.Component;

        public static void SetProfileOverride(ScenarioProfile profile)
        {
            s_profileOverride.Value = profile;
        }

        public static void ClearVariant()
        {
            s_variant.Value = null;
            s_case.Value = null;
            s_profileOverride.Value = null;
        }
    }
}
