// Copyright (C) 2015-2024 The Neo Project.
//
// PipeStringPayload.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Plugins.Buffers;
using System.Text;

namespace Neo.Plugins.Models.Payloads
{
    internal class PipeStringPayload : IPipeMessage
    {
        public string Value { get; set; } = string.Empty;

        public int Size =>
            sizeof(int) + // Size in bytes
            Encoding.UTF8.GetByteCount(Value); // size of the string

        public void FromByteArray(byte[] buffer, int position = 0)
        {
            var wrapper = new Stuffer(buffer, position);

            Value = wrapper.ReadString();
        }

        public byte[] ToByteArray()
        {
            var wrapper = new Stuffer(Size);

            wrapper.Write(Value);

            return [.. wrapper];
        }
    }
}
