// Copyright (C) 2015-2024 The Neo Project.
//
// NamedPipeServerConnectionThread.Messages.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Network.P2P.Payloads;
using Neo.Plugins.Models;
using Neo.Plugins.Models.Payloads;
using Neo.SmartContract.Native;
using System.IO;

namespace Neo.Plugins
{
    internal partial class NamedPipeServerConnectionThread
    {
        private PipeMessage OnBlockHeight(PipeMessage message)
        {
            if (message.Payload is not PipeNullPayload)
                return CreateErrorResponse(message.RequestId, new InvalidDataException());

            var blockHeight = NativeContract.Ledger.CurrentIndex(_system.StoreView);
            var payload = new PipeUnmanagedPayload<uint>() { Value = blockHeight };

            return PipeMessage.Create(message.RequestId, PipeCommand.BlockHeight, payload);
        }

        private PipeMessage OnBlock(PipeMessage message)
        {
            if (message.Payload is not PipeUnmanagedPayload<uint> blockIndex)
                return CreateErrorResponse(message.RequestId, new InvalidDataException());

            var block = NativeContract.Ledger.GetBlock(_system.StoreView, blockIndex.Value);
            var payload = new PipeSerializablePayload<Block>() { Value = block };

            return PipeMessage.Create(message.RequestId, PipeCommand.Block, payload);
        }

        private PipeMessage OnMemoryPool(PipeMessage message)
        {
            if (message.Payload is not PipeNullPayload)
                return CreateErrorResponse(message.RequestId, new InvalidDataException());

            _system.MemPool.GetVerifiedAndUnverifiedTransactions(out var vtx, out var utx);
            var payload = new PipeMemoryPoolPayload() { VerifiedTransactions = [.. vtx], UnVerifiedTransactions = [.. utx] };

            return PipeMessage.Create(message.RequestId, PipeCommand.MemoryPool, payload);
        }
    }
}
