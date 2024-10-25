// Copyright (C) 2015-2024 The Neo Project.
//
// INamedPipeMessage.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.IO.Pipes.Buffers;

namespace Neo.IO.Pipes.Protocols
{
    internal interface INamedPipeMessage
    {
        public int Size { get; }

        public void FromBytes(byte[] buffer);
        public void FromMemoryBuffer(MemoryBuffer reader);
        public byte[] ToByteArray();
    }
}
