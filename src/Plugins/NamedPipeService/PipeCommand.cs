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

using Neo.Plugins.Models.Payloads;

namespace Neo.Plugins
{
    internal enum PipeCommand : byte
    {
        [PipeProtocol(typeof(PipeExceptionPayload))]
        Exception = 0xe0,

        [PipeProtocol(typeof(PipeNullPayload))]
        Nack = 0xf0, // NULL ACK
    }
}
