// Copyright (C) 2015-2025 The Neo Project.
//
// ByteArraySerializer.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using FASTER.core;
using System;

namespace Neo.Build.Core.Storage
{
    internal class ByteArraySerializer : BinaryObjectSerializer<byte[]>
    {
        public override void Deserialize(out byte[] value)
        {
            var bytes = new byte[4];
            reader.Read(bytes, 0, 4);
            var size = BitConverter.ToInt32(bytes);
            value = reader.ReadBytes(size);
        }

        public override void Serialize(ref byte[] value)
        {
            var len = BitConverter.GetBytes(value.Length);
            writer.Write(len);
            writer.Write(value);
        }
    }
}
