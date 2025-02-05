// Copyright (C) 2015-2025 The Neo Project.
//
// ITransactionRemovedHandler.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the repository
// or https://opensource.org/license/mit for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Ledger;

namespace Neo.IEventHandlers
{
    public interface ITransactionRemovedHandler
    {
        /// <summary>
        /// Handler of TransactionRemoved event from <see cref="MemoryPool"/>
        /// Triggered when a transaction is removed to the <see cref="MemoryPool"/>.
        /// </summary>
        /// <param name="sender">The source of the event</param>
        /// <param name="tx">The arguments of event that removes a transaction from the <see cref="MemoryPool"/></param>
        void MemoryPool_TransactionRemoved_Handler(object sender, TransactionRemovedEventArgs tx);
    }
}
