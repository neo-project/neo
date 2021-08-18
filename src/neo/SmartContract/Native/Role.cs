// Copyright (C) 2015-2021 The Neo Project.
// 
// The neo is free software distributed under the MIT software license, 
// see the accompanying file LICENSE in the main directory of the
// project or http://www.opensource.org/licenses/mit-license.php 
// for more details.
// 
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

namespace Neo.SmartContract.Native
{
    /// <summary>
    /// Represents the roles in the NEO system.
    /// </summary>
    public enum Role : byte
    {
        /// <summary>
        /// The validators of state. Used to generate and sign the state root.
        /// </summary>
        StateValidator = 4,

        /// <summary>
        /// The nodes used to process Oracle requests.
        /// </summary>
        Oracle = 8,

        /// <summary>
        /// NeoFS Alphabet nodes.
        /// </summary>
        NeoFSAlphabetNode = 16
    }
}
