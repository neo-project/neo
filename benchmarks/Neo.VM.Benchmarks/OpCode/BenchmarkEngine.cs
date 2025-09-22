// Copyright (C) 2015-2025 The Neo Project.
//
// BenchmarkEngine.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.VM.Benchmark.Infrastructure;
using Neo.VM.Types;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Neo.VM.Benchmark.OpCode
{
    /// <summary>
    /// A simple benchmark engine for <see cref="ExecutionEngine"/>.
    /// </summary>
    public class BenchmarkEngine : ExecutionEngine
    {
        private readonly Dictionary<VM.OpCode, (int Count, TimeSpan TotalTime)> _opcodeStats = new();
        private readonly Dictionary<Script, HashSet<uint>> _breakPoints = new();
        private long _gasConsumed;

        public BenchmarkResultRecorder? Recorder { get; set; }
        public Action<BenchmarkEngine, VM.Instruction>? BeforeInstruction { get; set; }
        public Action<BenchmarkEngine, VM.Instruction>? AfterInstruction { get; set; }

        public BenchmarkEngine() : base(ComposeJumpTable()) { }

        /// <summary>
        /// Add a breakpoint at the specified position of the specified script. The VM will break the execution when it reaches the breakpoint.
        /// </summary>
        /// <param name="script">The script to add the breakpoint.</param>
        /// <param name="position">The position of the breakpoint in the script.</param>
        public void AddBreakPoint(Script script, uint position)
        {
            if (!_breakPoints.TryGetValue(script, out var hashset))
            {
                hashset = [];
                _breakPoints.Add(script, hashset);
            }
            hashset.Add(position);
        }

        /// <summary>
        /// Start or continue execution of the VM.
        /// </summary>
        /// <returns>Returns the state of the VM after the execution.</returns>
        public BenchmarkEngine ExecuteUntil(VM.OpCode opCode)
        {
            if (State == VMState.BREAK)
                State = VMState.NONE;
            while (State == VMState.NONE)
            {
                ExecuteNext();
                try
                {
                    var instruction = CurrentContext!.CurrentInstruction!.OpCode;
                    if (instruction == opCode) break;
                }
                catch
                {
                    break;
                }
            }
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ExecuteBenchmark()
        {
            while (State != VMState.HALT && State != VMState.FAULT)
            {
                var instruction = CurrentContext!.CurrentInstruction ?? VM.Instruction.RET;
                ExecuteStep(instruction);
            }
#if DEBUG
            PrintOpcodeStats();
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ExecuteOneGASBenchmark()
        {
            ExecuteWithGasBudget(Benchmark_Opcode.OneGasDatoshi);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ExecuteTwentyGASBenchmark()
        {
            ExecuteWithGasBudget(20 * Benchmark_Opcode.OneGasDatoshi);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ExecuteOpCodesBenchmark()
        {
            ExecuteWithGasBudget(Benchmark_Opcode.OneGasDatoshi);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ExecuteUntilGas(long gasBudget)
        {
            ExecuteWithGasBudget(gasBudget);
        }

        protected override void OnFault(Exception ex)
        {
            base.OnFault(ex);
            // throw ex;
        }

        private void UpdateOpcodeStats(VM.OpCode opcode, TimeSpan elapsed)
        {
            if (!_opcodeStats.TryGetValue(opcode, out var value))
            {
                _opcodeStats[opcode] = (1, elapsed);
            }
            else
            {
                var (count, totalTime) = value;
                _opcodeStats[opcode] = (count + 1, totalTime + elapsed);
            }
        }

        private void PrintOpcodeStats()
        {
            Console.WriteLine("Opcode Statistics:");
            foreach (var kvp in _opcodeStats)
            {
                Console.WriteLine($"{kvp.Key,-15} " +
                                  $"Count: {kvp.Value.Count,8} " +
                                  $"Total Time: {kvp.Value.TotalTime.TotalMilliseconds * 1000,10:F2} μs " +
                                  $"Avg Time: {kvp.Value.TotalTime.TotalMilliseconds * 1000 / kvp.Value.Count,10:F2} μs");
            }
        }

        private static JumpTable ComposeJumpTable()
        {
            JumpTable jumpTable = new JumpTable();
            jumpTable[VM.OpCode.SYSCALL] = OnSysCall;
            return jumpTable;
        }

        private void ExecuteWithGasBudget(long gasBudget)
        {
            _gasConsumed = 0;
            while (State != VMState.HALT && State != VMState.FAULT)
            {
                var instruction = CurrentContext!.CurrentInstruction ?? VM.Instruction.RET;
                ExecuteStep(instruction);
                if (State == VMState.HALT || State == VMState.FAULT)
                    break;
                if (ConsumeGas(instruction.OpCode, gasBudget))
                    break;
            }
#if DEBUG
            PrintOpcodeStats();
#endif
        }

        private bool ConsumeGas(VM.OpCode opcode, long gasBudget)
        {
            if (gasBudget <= 0)
                return true;
            if (!Benchmark_Opcode.OpCodePrices.TryGetValue(opcode, out var price))
                throw new KeyNotFoundException($"Missing benchmark gas price for opcode {opcode}.");
            _gasConsumed += price;
            if (_gasConsumed >= gasBudget)
            {
                State = VMState.HALT;
                return true;
            }
            return false;
        }

        private void ExecuteStep(VM.Instruction instruction)
        {
            var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
            Stopwatch? stopwatch = null;
#if DEBUG
            stopwatch = Stopwatch.StartNew();
#else
            if (Recorder is not null)
                stopwatch = Stopwatch.StartNew();
#endif
            BeforeInstruction?.Invoke(this, instruction);
            ExecuteNext();
            var elapsed = stopwatch?.Elapsed ?? TimeSpan.Zero;
            var allocatedAfter = GC.GetAllocatedBytesForCurrentThread();
            var allocatedBytes = Math.Max(0, allocatedAfter - allocatedBefore);
#if DEBUG
            UpdateOpcodeStats(instruction.OpCode, elapsed);
#endif
            var stackDepth = CurrentContext?.EvaluationStack.Count ?? 0;
            var altStackDepth = 0;
            if (!Benchmark_Opcode.OpCodePrices.TryGetValue(instruction.OpCode, out var gas))
                throw new KeyNotFoundException($"Missing benchmark gas price for opcode {instruction.OpCode}.");
            Recorder?.RecordInstruction(instruction.OpCode, elapsed, allocatedBytes, stackDepth, altStackDepth, gas);
            AfterInstruction?.Invoke(this, instruction);
        }

        private static void OnSysCall(ExecutionEngine engine, VM.Instruction instruction)
        {
            uint method = instruction.TokenU32;
            if (method == 0x77777777)
                engine.CurrentContext!.EvaluationStack.Push(StackItem.FromInterface(new object()));
            else if (method == 0xaddeadde)
                engine.JumpTable.ExecuteThrow(engine, "error");
            else
                throw new Exception();
        }
    }
}
