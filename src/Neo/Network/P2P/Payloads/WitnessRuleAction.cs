// Copyright (C) 2015-2022 The Neo Project.
// 
// The neo is free software distributed under the MIT software license, 
// see the accompanying file LICENSE in the main directory of the
// project or http://www.opensource.org/licenses/mit-license.php 
// for more details.
// 
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

namespace Neo.Network.P2P.Payloads
{
    /// <summary>
    /// Indicates the action to be taken if the current context meets with the rule.
    /// </summary>
    public enum WitnessRuleAction : byte
    {
        /// <summary>
        /// Deny the witness according to the rule.
        /// </summary>
        Deny = 0,

        /// <summary>
        /// Allow the witness according to the rule.
        /// </summary>
        Allow = 1
    }
}
