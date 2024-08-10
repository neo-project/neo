// Copyright (C) 2015-2024 The Neo Project.
//
// PipeCommand.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Network.P2P.Payloads;
using Neo.Plugins.Models.Payloads;

namespace Neo.Plugins
{
    internal enum PipeCommand : byte
    {
        [PipeProtocol(typeof(PipeNullPayload))]
        GetBlockHeight = 0x00,

        [PipeProtocol(typeof(PipeUnmanagedPayload<uint>))]
        BlockHeight = 0x01,

        [PipeProtocol(typeof(PipeUnmanagedPayload<uint>))]
        GetBlock = 0x02,

        [PipeProtocol(typeof(PipeSerializablePayload<Block>))]
        Block = 0x03,

        [PipeProtocol(typeof(PipeSerializablePayload<UInt256>))]
        GetTransaction = 0x04,

        [PipeProtocol(typeof(PipeSerializablePayload<Transaction>))]
        Transaction = 0x05,

        [PipeProtocol(typeof(PipeExceptionPayload))]
        Exception = 0xe0,

        [PipeProtocol(typeof(PipeNullPayload))]
        NAck = 0xf0, // NULL ACK
    }
}
