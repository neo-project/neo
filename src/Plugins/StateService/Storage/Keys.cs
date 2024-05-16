// Copyright (C) 2015-2024 The Neo Project.
//
// Keys.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Buffers.Binary;

namespace Neo.Plugins.StateService.Storage
{
    public static class Keys
    {
        public static byte[] StateRoot(uint index)
        {
            byte[] buffer = new byte[sizeof(uint) + 1];
            buffer[0] = 1;
            BinaryPrimitives.WriteUInt32BigEndian(buffer.AsSpan(1), index);
            return buffer;
        }

        public static readonly byte[] CurrentLocalRootIndex = { 0x02 };
        public static readonly byte[] CurrentValidatedRootIndex = { 0x04 };
    }
}
