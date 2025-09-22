// Copyright (C) 2015-2025 The Neo Project.
//
// DelegatingScenario.cs file belongs to the neo project and is free
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
    /// Convenience scenario that delegates execution to pre-defined callbacks per variant.
    /// </summary>
    public sealed class DelegatingScenario : IVmScenario
    {
        private readonly IReadOnlyDictionary<BenchmarkVariant, Action> _actions;

        public DelegatingScenario(IReadOnlyDictionary<BenchmarkVariant, Action> actions)
        {
            _actions = actions;
        }

        public void Execute(BenchmarkVariant variant)
        {
            if (_actions.TryGetValue(variant, out var action))
            {
                action();
                return;
            }

            if (_actions.TryGetValue(BenchmarkVariant.Single, out var fallback))
            {
                fallback();
                return;
            }

            throw new NotSupportedException($"Scenario variant '{variant}' is not supported.");
        }
    }
}
