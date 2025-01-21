// Copyright (C) 2015-2025 The Neo Project.
//
// ICommittedHandler.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Ledger;
using Neo.Network.P2P.Payloads;

namespace Neo.IEventHandlers
{
    public interface ICommittedHandler
    {
        /// <summary>
        /// This is the handler of Commited event from <see cref="Blockchain"/>
        /// Triggered after a new block is Commited, and state has being updated.
        /// </summary>
        /// <param name="system">The <see cref="NeoSystem"/> object.</param>
        /// <param name="block">The committed <see cref="Block"/>.</param>
        void Blockchain_Committed_Handler(NeoSystem system, Block block);
    }
}
