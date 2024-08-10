// Copyright (C) 2015-2024 The Neo Project.
//
// PipeArrayPayload.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Plugins.Buffers;
using System.Linq;

namespace Neo.Plugins.Models.Payloads
{
    internal class PipeArrayPayload<T> : IPipeMessage
        where T : IPipeMessage, new()
    {
        public T[] Value { get; set; } = [];

        public int Size =>
            sizeof(int) +                         // Item Count
            Value.Sum(s => s.Size + sizeof(int)); // Size in bytes for item as ByteArray

        public void FromArray(byte[] buffer)
        {
            var wrapper = new Stuffer(buffer);

            var size = wrapper.Read<int>();
            Value = new T[size];

            for (var i = 0; i < size; i++)
            {
                Value[i] = new T();
                Value[i].FromArray(wrapper.ReadArray<byte>());
            }

        }

        public byte[] ToArray()
        {
            var wrapper = new Stuffer(Size);
            wrapper.Write(Value.Length);
            foreach (var item in Value)
                wrapper.Write(item.ToArray());

            return [.. wrapper];
        }
    }
}
