// Copyright (C) 2015-2024 The Neo Project.
//
// VMState.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

namespace Neo.VM
{
    /// <summary>
    /// Indicates the status of the VM.
    /// </summary>
    public enum VMState : byte
    {
        /// <summary>
        /// Indicates that the execution is in progress or has not yet begun.
        /// </summary>
        NONE = 0,

        /// <summary>
        /// Indicates that the execution has been completed successfully.
        /// </summary>
        HALT = 1 << 0,

        /// <summary>
        /// Indicates that the execution has ended, and an exception that cannot be caught is thrown.
        /// </summary>
        FAULT = 1 << 1,

        /// <summary>
        /// Indicates that a breakpoint is currently being hit.
        /// </summary>
        BREAK = 1 << 2,
    }
}
