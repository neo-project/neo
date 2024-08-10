// Copyright (C) 2015-2024 The Neo Project.
//
// PipeUnmanagedArrayPayload.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Plugins.Buffers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace Neo.Plugins.Models.Payloads
{
    internal class PipeUnmanagedArrayPayload<T> : IPipeMessage
        where T : unmanaged
    {
        public IEnumerable<T> Value { get; set; } = [];

        public PipeUnmanagedArrayPayload()
        {
            if (typeof(T) == typeof(char))
                throw new InvalidOperationException("You can't use 'System.Char'.");
        }

        public int Size =>
            sizeof(int) +                        // Array Length
            Value.Sum(s => Marshal.SizeOf<T>()); // Size in bytes of all values in the array.

        public void FromArray(byte[] buffer)
        {
            var wrapper = new Stuffer(buffer);

            Value = wrapper.ReadArray<T>();
        }

        public byte[] ToArray()
        {
            var wrapper = new Stuffer(Size);

            wrapper.Write(Value.ToArray());

            return [.. wrapper];
        }
    }
}
