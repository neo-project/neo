// Copyright (C) 2015-2024 The Neo Project.
//
// Vote.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.IO;
using System;
using System.IO;

namespace Neo.Plugins.StateService.Network
{
    class Vote : ISerializable
    {
        public int ValidatorIndex;
        public uint RootIndex;
        public ReadOnlyMemory<byte> Signature;

        int ISerializable.Size => sizeof(int) + sizeof(uint) + Signature.GetVarSize();

        void ISerializable.Serialize(BinaryWriter writer)
        {
            writer.Write(ValidatorIndex);
            writer.Write(RootIndex);
            writer.WriteVarBytes(Signature.Span);
        }

        void ISerializable.Deserialize(ref MemoryReader reader)
        {
            ValidatorIndex = reader.ReadInt32();
            RootIndex = reader.ReadUInt32();
            Signature = reader.ReadVarMemory(64);
        }
    }
}
