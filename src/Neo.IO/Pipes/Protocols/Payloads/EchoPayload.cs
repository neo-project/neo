// Copyright (C) 2015-2024 The Neo Project.
//
// EchoPayload.cs file belongs to the neo project and is free
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
    internal class EchoPayload : INamedPipeMessage
    {
        public string? Message { get; set; }

        public int Size =>
            MemoryBuffer.GetStringSize(Message);

        public void FromStream(Stream stream)
        {
            using var reader = new MemoryBuffer(stream);
            FromMemoryBuffer(reader);
        }

        public void FromMemoryBuffer(MemoryBuffer reader)
        {
            var str = reader.ReadString();
            if (string.IsNullOrEmpty(str) == false)
                Message = str;
        }

        public byte[] ToByteArray()
        {
            using var ms = new MemoryStream();
            using var writer = new MemoryBuffer(ms);
            if (string.IsNullOrEmpty(Message) == false)
                writer.WriteString(Message);
            return ms.ToArray();
        }
    }
}
