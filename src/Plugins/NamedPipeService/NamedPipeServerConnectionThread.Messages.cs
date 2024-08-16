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
using System.Linq;

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

        private PipeMessage OnMemoryPoolUnVerified(PipeMessage message)
        {
            if (message.Payload is not PipeNullPayload)
                return CreateErrorResponse(message.RequestId, new InvalidDataException());

            _system.MemPool.GetVerifiedAndUnverifiedTransactions(out _, out var utx);
            var payload = new PipeArrayPayload<PipeSerializablePayload<Transaction>>()
            {
                Value = [.. utx.Select(s => new PipeSerializablePayload<Transaction>() { Value = s })],
            };

            return PipeMessage.Create(message.RequestId, PipeCommand.MemoryPoolUnVerified, payload);
        }

        private PipeMessage OnMemoryPoolVerified(PipeMessage message)
        {
            if (message.Payload is not PipeNullPayload)
                return CreateErrorResponse(message.RequestId, new InvalidDataException());

            _system.MemPool.GetVerifiedAndUnverifiedTransactions(out var vtx, out _);
            var payload = new PipeArrayPayload<PipeSerializablePayload<Transaction>>()
            {
                Value = [.. vtx.Select(s => new PipeSerializablePayload<Transaction>() { Value = s })],
            };

            return PipeMessage.Create(message.RequestId, PipeCommand.MemoryPoolVerified, payload);
        }

        private PipeMessage OnShowState(PipeMessage message)
        {
            if (message.Payload is not PipeNullPayload)
                return CreateErrorResponse(message.RequestId, new InvalidDataException());

            var height = NativeContract.Ledger.CurrentIndex(_system.StoreView);
            var remoteAddresses = _localNode.GetRemoteNodes().Select(s => new PipeShowStatePayload()
            {
                RemoteEndPoint = s.Remote,
                ListenerTcpPort = s.ListenerTcpPort,
                ConnectedCount = _localNode.ConnectedCount,
                UnconnectedCount = _localNode.ConnectedCount,
                Height = height,
                HeaderHeight = _system.HeaderCache.Last?.Index ?? height,
                LastBlockIndex = s.LastBlockIndex,
                Version = s.Version,
            });

            var payload = new PipeArrayPayload<PipeShowStatePayload>()
            {
                Value = [.. remoteAddresses],
            };

            return PipeMessage.Create(message.RequestId, PipeCommand.State, payload);
        }
    }
}
