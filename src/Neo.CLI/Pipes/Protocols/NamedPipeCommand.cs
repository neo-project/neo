// Copyright (C) 2015-2024 The Neo Project.
//
// NamedPipeCommand.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

namespace Neo.CLI.Pipes.Protocols
{
    internal enum NamedPipeCommand : byte
    {
        [NamedPipeCommand(typeof(Payloads.EchoPayload))]
        Echo = 0x01,
    }
}
