// Copyright (C) 2015-2024 The Neo Project.
//
// PipeSerializable.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Hosting.App.Buffers;
using Neo.IO;

namespace Neo.Hosting.App.NamedPipes.Protocol.Messages
{
    internal class PipeSerializable<T> : IPipeMessage
        where T : ISerializable, new()
    {
        public T? Value { get; set; }

        public int Size =>
            sizeof(int) +   // Array length
            Value?.Size ?? 0;     // Block size in bytes

        public void FromArray(byte[] buffer)
        {
            var wrapper = new Struffer(buffer);

            var blockBytes = wrapper.ReadArray<byte>();

            Value = blockBytes.AsSerializable<T>();
        }

        public byte[] ToArray()
        {
            var wrapper = new Struffer(Size);

            wrapper.Write(Value?.ToArray() ?? []);

            return [.. wrapper];
        }
    }
}
