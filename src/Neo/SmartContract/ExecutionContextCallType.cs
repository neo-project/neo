// Copyright (C) 2015-2026 The Neo Project.
//
// ExecutionContextCallType.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;

namespace Neo.SmartContract
{
    /// <summary>
    /// Describes how an execution context was called.
    /// </summary>
    [Flags]
    public enum ExecutionContextCallType : byte
    {
        /// <summary>
        /// A directly loaded script or a static contract call.
        /// </summary>
        None = 0,

        /// <summary>
        /// A dynamic call, including System.Contract.Call and System.Runtime.LoadScript.
        /// </summary>
        Dynamic = 1,

        /// <summary>
        /// A script loaded through System.Runtime.LoadScript.
        /// </summary>
        LoadedScript = 2,

        /// <summary>
        /// A call to a native contract. May be combined with Dynamic.
        /// </summary>
        NativeContract = 4
    }
}
