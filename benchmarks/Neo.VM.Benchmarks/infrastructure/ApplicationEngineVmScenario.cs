// Copyright (C) 2015-2025 The Neo Project.
//
// ApplicationEngineVmScenario.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.SmartContract;
using System;

namespace Neo.VM.Benchmark.Infrastructure
{
    /// <summary>
    /// Scenario wrapper executing scripts under <see cref="BenchmarkApplicationEngine"/>.
    /// </summary>
    internal sealed class ApplicationEngineVmScenario : IVmScenario
    {
        private readonly Func<ScenarioProfile, ApplicationEngineScriptSet> _scriptFactory;
        private readonly Action<BenchmarkApplicationEngine, ScenarioProfile>? _configure;
        private readonly Func<ScenarioProfile, BenchmarkApplicationEngine>? _engineFactory;

        public ApplicationEngineVmScenario(
            Func<ScenarioProfile, ApplicationEngineScriptSet> scriptFactory,
            Action<BenchmarkApplicationEngine, ScenarioProfile>? configure = null,
            Func<ScenarioProfile, BenchmarkApplicationEngine>? engineFactory = null)
        {
            _scriptFactory = scriptFactory;
            _configure = configure;
            _engineFactory = engineFactory;
        }

        public void Execute(BenchmarkVariant variant)
        {
            var baseProfile = BenchmarkExecutionContext.CurrentCase?.Profile
                             ?? BenchmarkExecutionContext.CurrentProfile
                             ?? ScenarioProfile.For(ScenarioComplexity.Standard);

            var scripts = _scriptFactory(baseProfile);
            var script = scripts.GetScript(variant);
            var effectiveProfile = script.Profile.IsEmpty ? baseProfile : script.Profile;

            BenchmarkExecutionContext.SetProfileOverride(effectiveProfile);
            Execute(script);
        }

        private void Execute(ApplicationEngineScript script)
        {
            var profile = BenchmarkExecutionContext.CurrentProfile ?? ScenarioProfile.For(ScenarioComplexity.Standard);
            using var engine = _engineFactory?.Invoke(profile) ?? BenchmarkApplicationEngine.Create();
            engine.Recorder = BenchmarkExecutionContext.CurrentRecorder;
            _configure?.Invoke(engine, profile);
            engine.LoadScript(script.Script, configureState: state => script.ConfigureState?.Invoke(state));
            engine.Execute();
        }

        public readonly record struct ApplicationEngineScript(byte[] Script, ScenarioProfile Profile, Action<ExecutionContextState>? ConfigureState = null)
        {
            public static implicit operator ApplicationEngineScript(byte[] script) => new(script, default);
        }

        public readonly record struct ApplicationEngineScriptSet(ApplicationEngineScript Baseline, ApplicationEngineScript Single, ApplicationEngineScript Saturated)
        {
            public ApplicationEngineScript GetScript(BenchmarkVariant variant) => variant switch
            {
                BenchmarkVariant.Baseline => Baseline,
                BenchmarkVariant.Single => Single,
                BenchmarkVariant.Saturated => Saturated,
                _ => throw new ArgumentOutOfRangeException(nameof(variant), variant, null)
            };
        }
    }
}
