// Copyright (C) 2015-2024 The Neo Project.
//
// ITransactionRemovedHandler.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Ledger;

namespace Neo.IEventHandlers;

public interface ITransactionRemovedHandler
{
    /// <summary>
    /// Handler of TransactionRemoved event from <see cref="MemoryPool"/>
    /// Triggered when a transaction is removed to the <see cref="MemoryPool"/>.
    /// </summary>
    void MemoryPool_TransactionRemoved_Handler(object sender, TransactionRemovedEventArgs tx);
}
