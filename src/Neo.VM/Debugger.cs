// Copyright (C) 2015-2024 The Neo Project.
//
// Debugger.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System.Collections.Generic;

namespace Neo.VM
{
    /// <summary>
    /// A simple debugger for <see cref="ExecutionEngine"/>.
    /// </summary>
    public class Debugger
    {
        private readonly ExecutionEngine engine;
        private readonly Dictionary<Script, HashSet<uint>> break_points = new();

        /// <summary>
        /// Create a debugger on the specified <see cref="ExecutionEngine"/>.
        /// </summary>
        /// <param name="engine">The <see cref="ExecutionEngine"/> to attach the debugger.</param>
        public Debugger(ExecutionEngine engine)
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
        public VMState Execute()
        {
            if (engine.State == VMState.BREAK)
                engine.State = VMState.NONE;
            while (engine.State == VMState.NONE)
                ExecuteAndCheckBreakPoints();
            return engine.State;
        }

        private void ExecuteAndCheckBreakPoints()
        {
            engine.ExecuteNext();
            if (engine.State == VMState.NONE && engine.InvocationStack.Count > 0 && break_points.Count > 0)
            {
                if (break_points.TryGetValue(engine.CurrentContext!.Script, out HashSet<uint>? hashset) && hashset.Contains((uint)engine.CurrentContext.InstructionPointer))
                    engine.State = VMState.BREAK;
            }
        }

        /// <summary>
        /// Removes the breakpoint at the specified position in the specified script.
        /// </summary>
        /// <param name="script">The script to remove the breakpoint.</param>
        /// <param name="position">The position of the breakpoint in the script.</param>
        /// <returns>
        /// <see langword="true"/> if the breakpoint is successfully found and removed;
        /// otherwise, <see langword="false"/>.
        /// </returns>
        public bool RemoveBreakPoint(Script script, uint position)
        {
            if (!break_points.TryGetValue(script, out HashSet<uint>? hashset)) return false;
            if (!hashset.Remove(position)) return false;
            if (hashset.Count == 0) break_points.Remove(script);
            return true;
        }

        /// <summary>
        /// Execute the next instruction. If the instruction involves a call to a method, it steps into the method and breaks the execution on the first instruction of that method.
        /// </summary>
        /// <returns>The VM state after the instruction is executed.</returns>
        public VMState StepInto()
        {
            if (engine.State == VMState.HALT || engine.State == VMState.FAULT)
                return engine.State;
            engine.ExecuteNext();
            if (engine.State == VMState.NONE)
                engine.State = VMState.BREAK;
            return engine.State;
        }

        /// <summary>
        /// Execute until the currently executed method is returned.
        /// </summary>
        /// <returns>The VM state after the currently executed method is returned.</returns>
        public VMState StepOut()
        {
            if (engine.State == VMState.BREAK)
                engine.State = VMState.NONE;
            int c = engine.InvocationStack.Count;
            while (engine.State == VMState.NONE && engine.InvocationStack.Count >= c)
                ExecuteAndCheckBreakPoints();
            if (engine.State == VMState.NONE)
                engine.State = VMState.BREAK;
            return engine.State;
        }

        /// <summary>
        /// Execute the next instruction. If the instruction involves a call to a method, it does not step into the method (it steps over it instead).
        /// </summary>
        /// <returns>The VM state after the instruction is executed.</returns>
        public VMState StepOver()
        {
            if (engine.State == VMState.HALT || engine.State == VMState.FAULT)
                return engine.State;
            engine.State = VMState.NONE;
            int c = engine.InvocationStack.Count;
            do
            {
                ExecuteAndCheckBreakPoints();
            }
            while (engine.State == VMState.NONE && engine.InvocationStack.Count > c);
            if (engine.State == VMState.NONE)
                engine.State = VMState.BREAK;
            return engine.State;
        }
    }
}
