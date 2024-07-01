// Copyright (C) 2015-2024 The Neo Project.
//
// NeoSystemHostedService.Methods.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Network.P2P.Payloads;
using Neo.SmartContract.Native;

namespace Neo.Hosting.App.Host.Service
{
    internal partial class NeoSystemHostedService
    {

        public Block GetBlock(uint index) =>
            NativeContract.Ledger.GetBlock(_store, index);

        public Block GetBlock(UInt256 hash) =>
            NativeContract.Ledger.GetBlock(_store, hash);

        public Block GetCurrentHeight() =>
            NativeContract.Ledger.GetBlock(_store, NativeContract.Ledger.CurrentHash(_store));

        public Transaction GetTransaction(UInt256? txHash) =>
            NativeContract.Ledger.GetTransaction(_store, txHash);
    }
}
