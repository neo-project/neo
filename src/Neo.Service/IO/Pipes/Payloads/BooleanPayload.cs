// Copyright (C) 2015-2024 The Neo Project.
//
// BooleanPayload.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.IO;
using System.IO;

namespace Neo.Service.IO.Pipes.Payloads
{
    internal sealed class BooleanPayload : ISerializable
    {
        public static readonly BooleanPayload True = Create(true);
        public static readonly BooleanPayload False = Create(false);

        public bool Value => _value == 1;

        private byte _value = 0;

        public int Size =>
            sizeof(byte);

        public static BooleanPayload Create(bool value) =>
            new()
            {
                _value = (byte)(value ? 0xff : 0x00),
            };

        public void Deserialize(ref MemoryReader reader)
        {
            _value = reader.ReadByte();
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(_value);
        }
    }
}
