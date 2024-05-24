// Copyright (C) 2015-2024 The Neo Project.
//
// RecoveryMessage.CommitPayloadCompact.cs file belongs to the neo project and is free
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

namespace Neo.Consensus
{
    partial class RecoveryMessage
    {
        public class CommitPayloadCompact : ISerializable
        {
            public byte ViewNumber;
            public byte ValidatorIndex;
            public ReadOnlyMemory<byte> Signature;
            public ReadOnlyMemory<byte> InvocationScript;

            int ISerializable.Size =>
                sizeof(byte) +                  //ViewNumber
                sizeof(byte) +                  //ValidatorIndex
                Signature.Length +              //Signature
                InvocationScript.GetVarSize();  //InvocationScript

            void ISerializable.Deserialize(ref MemoryReader reader)
            {
                ViewNumber = reader.ReadByte();
                ValidatorIndex = reader.ReadByte();
                Signature = reader.ReadMemory(64);
                InvocationScript = reader.ReadVarMemory(1024);
            }

            void ISerializable.Serialize(BinaryWriter writer)
            {
                writer.Write(ViewNumber);
                writer.Write(ValidatorIndex);
                writer.Write(Signature.Span);
                writer.WriteVarBytes(InvocationScript.Span);
            }
        }
    }
}
