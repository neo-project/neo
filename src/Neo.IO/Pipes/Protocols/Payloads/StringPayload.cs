// Copyright (C) 2015-2024 The Neo Project.
//
// StringPayload.cs file belongs to the neo project and is free
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
    internal class StringPayload : INamedPipeMessage
    {
        public string? Value { get; set; }

        public int Size =>
            MemoryBuffer.GetStringSize(Value);

        public void FromMemoryBuffer(MemoryBuffer reader)
        {
            var str = reader.ReadString();
            if (string.IsNullOrEmpty(str) == false)
                Value = str;
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

            if (string.IsNullOrEmpty(Value) == false)
                writer.WriteString(Value);

            return ms.ToArray();
        }
    }
}
