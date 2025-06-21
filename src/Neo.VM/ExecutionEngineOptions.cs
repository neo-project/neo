// Copyright (C) 2015-2025 The Neo Project.
//
// ExecutionEngineOptions.cs file belongs to the neo project and is free
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
    /// Options for configuring the execution engine behavior.
    /// </summary>
    public class ExecutionEngineOptions
    {
        /// <summary>
        /// When enabled, uses the optimized array-based evaluation stack implementation
        /// instead of the default List-based implementation.
        /// Default: false for backward compatibility.
        /// </summary>
        public bool UseOptimizedStack { get; set; } = false;

        /// <summary>
        /// Default options instance.
        /// </summary>
        public static ExecutionEngineOptions Default { get; } = new ExecutionEngineOptions();
    }
}