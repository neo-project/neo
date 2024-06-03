// Copyright (C) 2015-2024 The Neo Project.
//
// RecoveryMessage.PreCommitPayloadCompact.cs file belongs to the neo project and is free
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

namespace Neo.Plugins.DBFTPlugin.Messages
{
    partial class RecoveryMessage
    {
        public class PreCommitPayloadCompact : ISerializable
        {
            public byte ViewNumber;
            public byte ValidatorIndex;
            public UInt256 PreparationHash;
            public ReadOnlyMemory<byte> InvocationScript;

            int ISerializable.Size =>
                sizeof(byte) +                  //ViewNumber
                sizeof(byte) +                  //ValidatorIndex
                UInt256.Length +                //PreparationHash
                InvocationScript.GetVarSize();  //InvocationScript

            void ISerializable.Deserialize(ref MemoryReader reader)
            {
                ViewNumber = reader.ReadByte();
                ValidatorIndex = reader.ReadByte();
                PreparationHash = reader.ReadSerializable<UInt256>();
                InvocationScript = reader.ReadVarMemory(1024);
            }

            void ISerializable.Serialize(BinaryWriter writer)
            {
                writer.Write(ViewNumber);
                writer.Write(ValidatorIndex);
                writer.Write(PreparationHash);
                writer.WriteVarBytes(InvocationScript.Span);
            }
        }
    }
}
