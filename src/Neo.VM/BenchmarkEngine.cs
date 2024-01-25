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

using System;
using System.Collections.Generic;

namespace Neo.VM
{
    /// <summary>
    /// A simple benchmark engine for <see cref="ExecutionEngine"/>.
    /// </summary>
    public class BenchmarkEngine
    {
        private readonly ExecutionEngine engine;
        private readonly Dictionary<Script, HashSet<uint>> break_points = new();

        /// <summary>
        /// Create a debugger on the specified <see cref="ExecutionEngine"/>.
        /// </summary>
        /// <param name="engine">The <see cref="ExecutionEngine"/> to attach the debugger.</param>
        public BenchmarkEngine(ExecutionEngine engine)
        {
            this.engine = engine;
        }

        /// <summary>
        /// Add a breakpoint at the specified position of the specified script. The VM will break the execution when it reaches the breakpoint.
        /// </summary>
        /// <param name="script">The script to add the breakpoint.</param>
        /// <param name="position">The position of the breakpoint in the script.</param>
        public void AddBreakPoint(Script script, uint position)
        {
            if (!break_points.TryGetValue(script, out HashSet<uint>? hashset))
            {
                hashset = new HashSet<uint>();
                break_points.Add(script, hashset);
            }
            hashset.Add(position);
        }

        /// <summary>
        /// Start or continue execution of the VM.
        /// </summary>
        /// <returns>Returns the state of the VM after the execution.</returns>
        public BenchmarkEngine ExecuteUntil(OpCode opCode)
        {
            if (engine.State == VMState.BREAK)
                engine.State = VMState.NONE;
            while (engine.State == VMState.NONE)
            {
                engine.ExecuteNext();
                try
                {
                    var instruction = engine.CurrentContext!.CurrentInstruction!.OpCode;
                    if (instruction == opCode) break;
                }
                catch (Exception e)
                {
                    break;
                }
            }
            return this;
        }

        public void ExecuteBenchmark()
        {
            this.engine.ExecuteNext();
        }
    }
}
