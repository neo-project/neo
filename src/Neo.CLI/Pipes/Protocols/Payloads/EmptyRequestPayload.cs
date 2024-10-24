// Copyright (C) 2015-2024 The Neo Project.
//
// EmptyRequestPayload.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.CLI.Pipes.Buffers;

namespace Neo.CLI.Pipes.Protocols.Payloads
{
    internal class EmptyRequestPayload : INamedPipeMessage
    {
        public int Size => 0;

        public void FromBytes(byte[] buffer)
        {

        }

        public void FromMemoryBuffer(MemoryBuffer reader)
        {

        }

        public byte[] ToByteArray()
        {
            return [];
        }
    }
}
