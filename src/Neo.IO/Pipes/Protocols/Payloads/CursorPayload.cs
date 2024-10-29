// Copyright (C) 2015-2024 The Neo Project.
//
// CursorPayload.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.IO.Buffers;
using System.IO;

namespace Neo.IO.Pipes.Protocols.Payloads
{
    internal class CursorPayload : INamedPipeMessage
    {
        public int Left { get; set; }
        public int Right { get; set; }

        public int Size =>
            sizeof(int) +
            sizeof(int);

        public void FromMemoryBuffer(MemoryBuffer reader)
        {
            Left = reader.Read<int>();
            Right = reader.Read<int>();
        }

        public void FromStream(Stream stream)
        {
            using var reader = new MemoryBuffer(stream);
            FromMemoryBuffer(reader);
        }

        public byte[] ToByteArray()
        {
            using var ms = new MemoryStream();
            using var writer = new MemoryBuffer(ms);

            writer.Write(Left);
            writer.Write(Right);

            return ms.ToArray();
        }
    }
}
