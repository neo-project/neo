// Copyright (C) 2015-2021 The Neo Project.
// 
// The neo is free software distributed under the MIT software license, 
// see the accompanying file LICENSE in the main directory of the
// project or http://www.opensource.org/licenses/mit-license.php 
// for more details.
// 
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;

namespace Neo.SmartContract
{
    /// <summary>
    /// Represents the operations allowed when a contract is called.
    /// </summary>
    [Flags]
    public enum CallFlags : byte
    {
        /// <summary>
        /// No flag is set.
        /// </summary>
        None = 0,

        /// <summary>
        /// Indicates that the called contract is allowed to read states.
        /// </summary>
        ReadStates = 0b00000001,

        /// <summary>
        /// Indicates that the called contract is allowed to write states.
        /// </summary>
        WriteStates = 0b00000010,

        /// <summary>
        /// Indicates that the called contract is allowed to call another contract.
        /// </summary>
        AllowCall = 0b00000100,

        /// <summary>
        /// Indicates that the called contract is allowed to send notifications.
        /// </summary>
        AllowNotify = 0b00001000,

        /// <summary>
        /// Indicates that the called contract is allowed to read or write states.
        /// </summary>
        States = ReadStates | WriteStates,

        /// <summary>
        /// Indicates that the called contract is allowed to read states or call another contract.
        /// </summary>
        ReadOnly = ReadStates | AllowCall,

        /// <summary>
        /// All flags are set.
        /// </summary>
        All = States | AllowCall | AllowNotify
    }
}
