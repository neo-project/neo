// Copyright (C) 2015-2021 The Neo Project.
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
    /// Represents a message that can be relayed on the NEO network.
    /// </summary>
    public interface IInventory : IVerifiable
    {
        /// <summary>
        /// The type of the inventory.
        /// </summary>
        InventoryType InventoryType { get; }
    }
}
