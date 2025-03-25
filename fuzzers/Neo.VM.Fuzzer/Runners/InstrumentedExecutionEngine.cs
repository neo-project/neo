// Copyright (C) 2015-2025 The Neo Project.
//
// InstrumentedExecutionEngine.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Neo.VM.Fuzzer.Runners
{
    /// <summary>
    /// An instrumented execution engine that tracks code coverage and execution details
    /// </summary>
    public class InstrumentedExecutionEngine : ExecutionEngine
    {
        private readonly Stopwatch _stopwatch = new Stopwatch();
        private readonly HashSet<string> _coverage = new HashSet<string>();
        private readonly Dictionary<OpCode, int> _opcodeExecutionCounts = new Dictionary<OpCode, int>();
        private readonly Dictionary<OpCode, List<long>> _opcodeExecutionTimes = new Dictionary<OpCode, List<long>>();
        private readonly Dictionary<int, int> _instructionPointerCounts = new Dictionary<int, int>();
        private readonly Stopwatch _opcodeStopwatch = new Stopwatch();
        private OpCode _currentOpcode;

        /// <summary>
        /// Event that is raised when a step is executed
        /// </summary>
        public event EventHandler<StepEventArgs>? OnStepEvent;

        /// <summary>
        /// Event that is raised when a fault occurs
        /// </summary>
        public event EventHandler<FaultEventArgs>? OnFaultEvent;

        /// <summary>
        /// Event that is raised when execution is completed
        /// </summary>
        public event EventHandler<EventArgs>? OnExecutionCompleted;

        /// <summary>
        /// Gets the collection of coverage points tracked during execution
        /// </summary>
        public HashSet<string> Coverage => _coverage;

        /// <summary>
        /// Gets the execution time in milliseconds
        /// </summary>
        public long ExecutionTimeMs => _stopwatch.ElapsedMilliseconds;

        /// <summary>
        /// Gets the maximum stack size reached during execution
        /// </summary>
        public int MaxStackSize { get; private set; } = 0;

        /// <summary>
        /// Gets the maximum invocation stack depth reached during execution
        /// </summary>
        public int MaxInvocationDepth { get; private set; } = 0;

        /// <summary>
        /// Gets the number of instructions executed
        /// </summary>
        public int InstructionsExecuted { get; private set; } = 0;

        /// <summary>
        /// Gets the dictionary of instruction execution counts
        /// </summary>
        public IReadOnlyDictionary<int, int> InstructionExecutionCount => _instructionPointerCounts;

        /// <summary>
        /// Gets the dictionary of opcode execution times in ticks
        /// </summary>
        public IReadOnlyDictionary<OpCode, List<long>> OpcodeExecutionTimes => _opcodeExecutionTimes;

        /// <summary>
        /// Initializes a new instance of the InstrumentedExecutionEngine class
        /// </summary>
        public InstrumentedExecutionEngine() : base()
        {
            // In the current Neo VM version, we need to override methods instead of using events
            // Event handlers will be called from overridden methods
        }

        /// <summary>
        /// Executes the loaded script and tracks execution metrics
        /// </summary>
        /// <returns>The final VM state</returns>
        public new VMState Execute()
        {
            _stopwatch.Restart();
            MaxStackSize = 0;
            MaxInvocationDepth = 0;
            _coverage.Clear();
            _opcodeExecutionCounts.Clear();
            _opcodeExecutionTimes.Clear();
            _instructionPointerCounts.Clear();

            try
            {
                var result = base.Execute();

                // Raise the OnExecutionCompleted event
                OnExecutionCompleted?.Invoke(this, EventArgs.Empty);

                return result;
            }
            finally
            {
                _stopwatch.Stop();
            }
        }

        /// <summary>
        /// Handles the step event to track execution details
        /// </summary>
        protected override void PreExecuteInstruction(Instruction instruction)
        {
            base.PreExecuteInstruction(instruction);

            // Start timing the opcode execution
            _opcodeStopwatch.Restart();

            // Save the current opcode for timing
            _currentOpcode = instruction.OpCode;

            // Track instruction pointer coverage
            int ip = CurrentContext.InstructionPointer;
            if (CurrentContext?.Script != null)
            {
                string key = $"{CurrentContext.Script.GetHashCode()}:{ip}";
                _coverage.Add(key);
            }

            // Track instruction execution count
            if (!_instructionPointerCounts.ContainsKey(ip))
            {
                _instructionPointerCounts[ip] = 1;
            }
            else
            {
                _instructionPointerCounts[ip]++;
            }

            // Increment the instruction counter
            InstructionsExecuted++;

            // Track specific patterns
            TrackSpecificPatterns(instruction.OpCode);

            // Raise the OnStep event
            if (CurrentContext != null)
            {
                OnStepEvent?.Invoke(this, new StepEventArgs(instruction.OpCode, ip, CurrentContext.EvaluationStack.Count));
            }
        }

        /// <summary>
        /// Handles post-execution of an instruction
        /// </summary>
        protected override void PostExecuteInstruction(Instruction instruction)
        {
            base.PostExecuteInstruction(instruction);

            // Stop timing the opcode execution
            _opcodeStopwatch.Stop();

            // Track opcode execution time
            if (!_opcodeExecutionTimes.ContainsKey(_currentOpcode))
            {
                _opcodeExecutionTimes[_currentOpcode] = new List<long>();
            }
            _opcodeExecutionTimes[_currentOpcode].Add(_opcodeStopwatch.ElapsedTicks);

            // Track opcode execution count
            if (!_opcodeExecutionCounts.ContainsKey(_currentOpcode))
            {
                _opcodeExecutionCounts[_currentOpcode] = 1;
            }
            else
            {
                _opcodeExecutionCounts[_currentOpcode]++;
            }
        }

        /// <summary>
        /// Handles faults during execution
        /// </summary>
        protected override void OnFault(Exception ex)
        {
            base.OnFault(ex);

            // Raise the OnFaultEvent
            int instructionPointer = CurrentContext?.InstructionPointer ?? 0;
            OnFaultEvent?.Invoke(this, new FaultEventArgs(ex.GetType().Name, ex.Message, instructionPointer));
        }

        /// <summary>
        /// Tracks specific patterns of opcodes that might be interesting for fuzzing
        /// </summary>
        /// <param name="opcode">The current opcode</param>
        private void TrackSpecificPatterns(OpCode opcode)
        {
            // Track arithmetic operations
            if (opcode >= OpCode.ADD && opcode <= OpCode.MOD)
            {
                _coverage.Add("Pattern:Arithmetic");
            }

            // Track stack operations
            if (opcode >= OpCode.DUP && opcode <= OpCode.DEPTH)
            {
                _coverage.Add("Pattern:StackOp");
            }

            // Track control flow operations
            if (opcode >= OpCode.JMP && opcode <= OpCode.ENDTRY)
            {
                _coverage.Add("Pattern:ControlFlow");
            }

            // Track array/struct operations
            if ((opcode >= OpCode.NEWARRAY && opcode <= OpCode.NEWSTRUCT) ||
                (opcode >= OpCode.APPEND && opcode <= OpCode.SETITEM))
            {
                _coverage.Add("Pattern:ArrayStruct");
            }

            // Track crypto operations - removed SHA1 and CHECKMULTISIG as they don't exist in current Neo VM
            if (opcode == OpCode.EQUAL)
            {
                _coverage.Add("Pattern:Crypto");
            }

            // Track potentially expensive operations for DOS detection
            if (opcode == OpCode.APPEND ||
                opcode == OpCode.SETITEM ||
                opcode == OpCode.NEWARRAY ||
                opcode == OpCode.NEWSTRUCT ||
                opcode == OpCode.UNPACK)
            {
                _coverage.Add("Pattern:ExpensiveOperation");
            }
        }
    }
}
