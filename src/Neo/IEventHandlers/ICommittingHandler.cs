// Copyright (C) 2015-2024 The Neo Project.
//
// ICommittingHandler.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using System.Collections.Generic;

namespace Neo.IEventHandlers;

public interface ICommittingHandler
{
    /// <summary>
    /// This is the handler of Committing event from <see cref="Blockchain"/>
    /// Triggered when a new block is committing, and the state is still in the cache.
    /// </summary>
    /// <param name="system">The <see cref="NeoSystem"/> instance associated with the event.</param>
    /// <param name="block">The block that is being committed.</param>
    /// <param name="snapshot">The current data snapshot.</param>
    /// <param name="applicationExecutedList">A list of executed applications associated with the block.</param>
    void Blockchain_Committing_Handler(NeoSystem system, Block block, DataCache snapshot, IReadOnlyList<Blockchain.ApplicationExecuted> applicationExecutedList);
}
