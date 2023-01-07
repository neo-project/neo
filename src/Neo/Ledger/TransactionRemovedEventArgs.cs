// Copyright (C) 2015-2023 The Neo Project.
//
// The neo is free software distributed under the MIT software license,
// see the accompanying file LICENSE in the main directory of the
// project or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Network.P2P.Payloads;
using System.Collections.Generic;

namespace Neo.Ledger;

/// <summary>
/// Represents the event data of <see cref="MemoryPool.TransactionRemoved"/>.
/// </summary>
public sealed class TransactionRemovedEventArgs
{
    /// <summary>
    /// The <see cref="Transaction"/>s that is being removed.
    /// </summary>
    public IReadOnlyCollection<Transaction> Transactions { get; init; }

    /// <summary>
    /// The reason a transaction was removed.
    /// </summary>
    public TransactionRemovalReason Reason { get; init; }
}
