// Copyright (C) 2016-2023 The Neo Project.
// 
// The neo-vm is free software distributed under the MIT software license, 
// see the accompanying file LICENSE in the main directory of the
// project or http://www.opensource.org/licenses/mit-license.php 
// for more details.
// 
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System.Diagnostics;

namespace Neo.VM
{
    /// <summary>
    /// Represents the context used for exception handling.
    /// </summary>
    [DebuggerDisplay("State={State}, CatchPointer={CatchPointer}, FinallyPointer={FinallyPointer}, EndPointer={EndPointer}")]
    public sealed class ExceptionHandlingContext
    {
        /// <summary>
        /// The position of the <see langword="catch"/> block.
        /// </summary>
        public int CatchPointer { get; }

        /// <summary>
        /// The position of the <see langword="finally"/> block.
        /// </summary>
        public int FinallyPointer { get; }

        /// <summary>
        /// The end position of the <see langword="try"/>-<see langword="catch"/>-<see langword="finally"/> block.
        /// </summary>
        public int EndPointer { get; internal set; } = -1;

        /// <summary>
        /// Indicates whether the <see langword="catch"/> block is included in the context.
        /// </summary>
        public bool HasCatch => CatchPointer >= 0;

        /// <summary>
        /// Indicates whether the <see langword="finally"/> block is included in the context.
        /// </summary>
        public bool HasFinally => FinallyPointer >= 0;

        /// <summary>
        /// Indicates the state of the context.
        /// </summary>
        public ExceptionHandlingState State { get; internal set; } = ExceptionHandlingState.Try;

        internal ExceptionHandlingContext(int catchPointer, int finallyPointer)
        {
            this.CatchPointer = catchPointer;
            this.FinallyPointer = finallyPointer;
        }
    }
}
