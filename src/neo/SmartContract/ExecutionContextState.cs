// Copyright (C) 2015-2021 The Neo Project.
// 
// The neo is free software distributed under the MIT software license, 
// see the accompanying file LICENSE in the main directory of the
// project or http://www.opensource.org/licenses/mit-license.php 
// for more details.
// 
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.VM;

namespace Neo.SmartContract
{
    /// <summary>
    /// Represents the custom state in <see cref="ExecutionContext"/>.
    /// </summary>
    public class ExecutionContextState
    {
        /// <summary>
        /// The script hash of the current context.
        /// </summary>
        public UInt160 ScriptHash { get; set; }

        /// <summary>
        /// The script hash of the calling contract.
        /// </summary>
        public UInt160 CallingScriptHash { get; set; }

        /// <summary>
        /// The <see cref="ContractState"/> of the current context.
        /// </summary>
        public ContractState Contract { get; set; }

        /// <summary>
        /// The <see cref="SmartContract.CallFlags"/> of the current context.
        /// </summary>
        public CallFlags CallFlags { get; set; } = CallFlags.All;

        /// <summary>
        /// Indicates whether the method allow to throw exceptions from external contract.
        /// False by default
        /// </summary>
        public bool AllowCrossThrows { get; set; } = false;
    }
}
