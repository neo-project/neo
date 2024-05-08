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

using Neo.Hosting.App.NamedPipes.Protocol.Messages;

namespace Neo.Hosting.App.NamedPipes.Protocol
{
    internal enum PipeCommand : byte
    {
        [PipeProtocol(typeof(PipeEmptyPayload))]
        Empty = 0x00,

        [PipeProtocol(typeof(PipeVersion))]
        Version = 0x01,

        [PipeProtocol(typeof(PipeException))]
        Exception = 0x02,
    }
}
