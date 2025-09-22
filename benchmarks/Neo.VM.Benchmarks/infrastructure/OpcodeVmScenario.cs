// Copyright (C) 2015-2025 The Neo Project.
//
// OpcodeVmScenario.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.VM.Benchmark.OpCode;
using System;

namespace Neo.VM.Benchmark.Infrastructure
{
    /// <summary>
    /// Scenario wrapper executing pre-generated opcode scripts.
    /// </summary>
    public sealed class OpcodeVmScenario : IVmScenario
    {
        private readonly byte[] _baselineScript;
        private readonly byte[] _singleScript;
        private readonly byte[] _saturatedScript;
        private readonly long _saturatedGasBudget;
        private readonly Action<BenchmarkEngine, VM.Instruction>? _before;
        private readonly Action<BenchmarkEngine, VM.Instruction>? _after;

        public OpcodeVmScenario(
            byte[] baselineScript,
            byte[] singleScript,
            byte[] saturatedScript,
            long saturatedGasBudget,
            Action<BenchmarkEngine, VM.Instruction>? before = null,
            Action<BenchmarkEngine, VM.Instruction>? after = null)
        {
            _baselineScript = baselineScript;
            _singleScript = singleScript;
            _saturatedScript = saturatedScript;
            _saturatedGasBudget = saturatedGasBudget;
            _before = before;
            _after = after;
        }

        public void Execute(BenchmarkVariant variant)
        {
            switch (variant)
            {
                case BenchmarkVariant.Baseline:
                    ExecuteScript(_baselineScript, static (engine, _) => engine.ExecuteBenchmark());
                    break;
                case BenchmarkVariant.Single:
                    ExecuteScript(_singleScript, static (engine, _) => engine.ExecuteBenchmark());
                    break;
                case BenchmarkVariant.Saturated:
                    ExecuteScript(_saturatedScript, (engine, _) => engine.ExecuteUntilGas(_saturatedGasBudget));
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(variant), variant, null);
            }
        }

        private void ExecuteScript(byte[] script, Action<BenchmarkEngine, byte[]> runner)
        {
            using var engine = Benchmark_Opcode.LoadScript(script);
            engine.BeforeInstruction = _before;
            engine.AfterInstruction = _after;
            engine.Recorder = BenchmarkExecutionContext.CurrentRecorder;
            runner(engine, script);
        }
    }
}
