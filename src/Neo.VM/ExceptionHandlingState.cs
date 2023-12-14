// Copyright (C) 2016-2023 The Neo Project.
// 
// The neo-vm is free software distributed under the MIT software license, 
// see the accompanying file LICENSE in the main directory of the
// project or http://www.opensource.org/licenses/mit-license.php 
// for more details.
// 
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

namespace Neo.VM
{
    /// <summary>
    /// Indicates the state of the <see cref="ExceptionHandlingContext"/>.
    /// </summary>
    public enum ExceptionHandlingState : byte
    {
        /// <summary>
        /// Indicates that the <see langword="try"/> block is being executed.
        /// </summary>
        Try,

        /// <summary>
        /// Indicates that the <see langword="catch"/> block is being executed.
        /// </summary>
        Catch,

        /// <summary>
        /// Indicates that the <see langword="finally"/> block is being executed.
        /// </summary>
        Finally
    }
}
