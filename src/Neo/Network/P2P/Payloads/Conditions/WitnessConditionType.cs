// Copyright (C) 2015-2021 The Neo Project.
// 
// The neo is free software distributed under the MIT software license, 
// see the accompanying file LICENSE in the main directory of the
// project or http://www.opensource.org/licenses/mit-license.php 
// for more details.
// 
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.IO.Caching;

namespace Neo.Network.P2P.Payloads.Conditions
{
    /// <summary>
    /// Represents the type of <see cref="WitnessCondition"/>.
    /// </summary>
    public enum WitnessConditionType : byte
    {
        /// <summary>
        /// Indicates that the condition will always be met or not met.
        /// </summary>
        [ReflectionCache(typeof(BooleanCondition))]
        Boolean = 0x00,

        /// <summary>
        /// Reverse another condition.
        /// </summary>
        [ReflectionCache(typeof(NotCondition))]
        Not = 0x01,

        /// <summary>
        /// Indicates that all conditions must be met.
        /// </summary>
        [ReflectionCache(typeof(AndCondition))]
        And = 0x02,

        /// <summary>
        /// Indicates that any of the conditions meets.
        /// </summary>
        [ReflectionCache(typeof(OrCondition))]
        Or = 0x03,

        /// <summary>
        /// Indicates that the condition is met when the current context has the specified script hash.
        /// </summary>
        [ReflectionCache(typeof(ScriptHashCondition))]
        ScriptHash = 0x18,

        /// <summary>
        /// Indicates that the condition is met when the current context has the specified group.
        /// </summary>
        [ReflectionCache(typeof(GroupCondition))]
        Group = 0x19,

        /// <summary>
        /// Indicates that the condition is met when the current context is the entry point or is called by the entry point.
        /// </summary>
        [ReflectionCache(typeof(CalledByEntryCondition))]
        CalledByEntry = 0x20,

        /// <summary>
        /// Indicates that the condition is met when the current context is called by the specified contract.
        /// </summary>
        [ReflectionCache(typeof(CalledByContractCondition))]
        CalledByContract = 0x28,

        /// <summary>
        /// Indicates that the condition is met when the current context is called by the specified group.
        /// </summary>
        [ReflectionCache(typeof(CalledByGroupCondition))]
        CalledByGroup = 0x29
    }
}
