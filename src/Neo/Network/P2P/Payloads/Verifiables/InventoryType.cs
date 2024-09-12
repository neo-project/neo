// Copyright (C) 2015-2024 The Neo Project.
//
// InventoryType.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

namespace Neo.Network.P2P.Payloads
{
    /// <summary>
    /// Represents the type of an inventory.
    /// </summary>
    public enum InventoryType : byte
    {
        /// <summary>
        /// Indicates that the inventory is a <see cref="Transaction"/>.
        /// </summary>
        TX = MessageCommand.Transaction,

        /// <summary>
        /// Indicates that the inventory is a <see cref="Block"/>.
        /// </summary>
        Block = MessageCommand.Block,

        /// <summary>
        /// Indicates that the inventory is an <see cref="ExtensiblePayload"/>.
        /// </summary>
        Extensible = MessageCommand.Extensible
    }
}
