// Copyright (C) 2015-2026 The Neo Project.
//
// NewTransactionEventArgs.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using System.ComponentModel;

namespace Neo.Ledger
{
    public class NewTransactionEventArgs : CancelEventArgs
    {
        public required Transaction Transaction { get; init; }
        public required IReadOnlyStore Snapshot { get; init; }
    }
}
