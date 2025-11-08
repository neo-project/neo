// Copyright (C) 2015-2025 The Neo Project.
//
// TokenTransfer.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Extensions;
using Neo.IO;
using System.IO;
using System.Numerics;

namespace Neo.Plugins.Trackers
{
    internal class TokenTransfer : ISerializable
    {
        // These fields are always set in TokensTracker. Give it a default value to avoid null warning when deserializing.
        public UInt160 UserScriptHash = UInt160.Zero;
        public uint BlockIndex;
        public UInt256 TxHash = UInt256.Zero;
        public BigInteger Amount;

        int ISerializable.Size =>
            UInt160.Length +        // UserScriptHash
            sizeof(uint) +          // BlockIndex
            UInt256.Length +        // TxHash
            Amount.GetVarSize();    // Amount

        void ISerializable.Serialize(BinaryWriter writer)
        {
            writer.Write(UserScriptHash);
            writer.Write(BlockIndex);
            writer.Write(TxHash);
            writer.WriteVarBytes(Amount.ToByteArray());
        }

        void ISerializable.Deserialize(ref MemoryReader reader)
        {
            UserScriptHash = reader.ReadSerializable<UInt160>();
            BlockIndex = reader.ReadUInt32();
            TxHash = reader.ReadSerializable<UInt256>();
            Amount = new BigInteger(reader.ReadVarMemory(32).Span);
        }
    }
}
