// Copyright (C) 2015-2024 The Neo Project.
//
// PipeSerializablePayload.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Extensions;
using Neo.IO;
using System.Linq;

namespace Neo.Plugins.Models.Payloads
{
    internal class PipeSerializablePayload<T> : IPipeMessage
        where T : ISerializable, new()
    {
        public T? Value { get; set; }

        public int Size =>
            Value?.Size ?? 0; // Block size in bytes

        public void FromByteArray(byte[] buffer, int position = 0)
        {
            Value = buffer.TryCatch(t => t.AsSerializable<T>(position), default);
        }

        public byte[] ToByteArray()
        {
            return Value?.ToArray() ?? [];
        }
    }
}
