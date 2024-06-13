// Copyright (C) 2015-2024 The Neo Project.
//
// ITransactionAddedHandler.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Ledger;
using Neo.Network.P2P.Payloads;

namespace Neo.IEventHandlers;

public interface ITransactionAddedHandler
{
    /// <summary>
    /// The handler of TransactionAdded event from the <see cref="MemoryPool"/>.
    /// Triggered when a transaction is added to the <see cref="MemoryPool"/>.
    /// </summary>
    /// <param name="sender">The source of the event</param>
    /// <param name="tx">The transaction added to the memory pool <see cref="MemoryPool"/>.</param>
    void MemoryPool_TransactionAdded_Handler(object sender, Transaction tx);
}
