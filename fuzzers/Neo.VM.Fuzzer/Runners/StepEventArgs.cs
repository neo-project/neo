// Copyright (C) 2015-2025 The Neo Project.
//
// StepEventArgs.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

namespace Neo.VM.Fuzzer.Runners
{
    /// <summary>
    /// Provides data for the step event in the execution engine
    /// </summary>
    public class StepEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the operation code being executed
        /// </summary>
        public OpCode OpCode { get; }

        /// <summary>
        /// Gets the current instruction pointer position
        /// </summary>
        public int InstructionPointer { get; }

        /// <summary>
        /// Gets the current size of the evaluation stack
        /// </summary>
        public int StackSize { get; }

        /// <summary>
        /// Initializes a new instance of the StepEventArgs class
        /// </summary>
        /// <param name="opCode">The operation code being executed</param>
        /// <param name="instructionPointer">The current instruction pointer position</param>
        /// <param name="stackSize">The current size of the evaluation stack</param>
        public StepEventArgs(OpCode opCode, int instructionPointer, int stackSize)
        {
            OpCode = opCode;
            InstructionPointer = instructionPointer;
            StackSize = stackSize;
        }
    }
}
