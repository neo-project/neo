// Copyright (C) 2015-2024 The Neo Project.
//
// BenchmarkEngine.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.VM.Types;
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
        private long _gasConsumed = 0;

        private static readonly Dictionary<VM.OpCode, (int, float)> s_opCodeValueRanges = new()
        {
            [VM.OpCode.UNPACK] = (16, 1),
            [VM.OpCode.PACK] = (16, 0.7f),
            [VM.OpCode.REVERSEN] = (64, 1),
        };

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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public BenchmarkEngine ExecuteUntil(VM.OpCode opCode)
        {
            if (State == VMState.BREAK)
                State = VMState.NONE;
            while (State == VMState.NONE)
            {
                try
                {
                    ExecuteNext();
                    var instruction = CurrentContext!.CurrentInstruction!.OpCode;
                    if (instruction == opCode)
                        State = VMState.BREAK;
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
                ExecuteNext();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ExecuteOneGASBenchmark(double gas = 1)
        {
            var maxGas = Benchmark_Opcode.OneGasDatoshi * gas;
            while (State != VMState.HALT && State != VMState.FAULT)
            {
                var instruction = CurrentContext!.CurrentInstruction ?? VM.Instruction.RET;

                //if (s_opCodeValueRanges.TryGetValue(instruction.OpCode, out var opCodeRange))
                //{

                //    var gasPrice =_complexFactor >= opCodeRange.Item1?(int)( 4 * ((_complexFactor + 4) >> 2)*opCodeRange.Item2): 1;
                //    // var gasPrice =
                //    //     ? (int)(_complexFactor * opCodeRange.Item2 + 1)
                //    //     : 1;
                //    // Console.WriteLine("opCodeRange = "+opCodeRange + " _complexFactor = "+ _complexFactor+ " gasPrice: "+gasPrice);
                //    _gasConsumed += Benchmark_Opcode.OpCodePrices[instruction.OpCode] * 3 * gasPrice;
                //}
                //else
                //{
                _gasConsumed += Benchmark_Opcode.OpCodePrices[instruction.OpCode] * 3;
                //}


                if (_gasConsumed >= maxGas)
                {
                    State = VMState.HALT;
                }
                ExecuteNext();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void ExecuteOpCodesBenchmark()
        {
            while (State != VMState.HALT && State != VMState.FAULT)
            {
                var instruction = CurrentContext!.CurrentInstruction ?? VM.Instruction.RET;
                _gasConsumed += Benchmark_Opcode.OpCodePrices[instruction.OpCode];
                if (_gasConsumed >= Benchmark_Opcode.OneGasDatoshi)
                {
                    State = VMState.HALT;
                }
#if DEBUG
                var stopwatch = Stopwatch.StartNew();
#endif
                ExecuteNext();
#if DEBUG
                stopwatch.Stop();
                UpdateOpcodeStats(instruction.OpCode, stopwatch.Elapsed);
#endif
            }
#if DEBUG
            PrintOpcodeStats();
#endif
        }

        protected override void OnFault(Exception ex)
        {
            base.OnFault(ex);
            // #if DEBUG
            throw ex;
            // #endif
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
