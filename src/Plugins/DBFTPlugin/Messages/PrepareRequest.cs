// Copyright (C) 2015-2024 The Neo Project.
//
// PrepareRequest.cs file belongs to the neo project and is free
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
using System.Linq;

namespace Neo.Consensus
{
    public class PrepareRequest : ConsensusMessage
    {
        public uint Version;
        public UInt256 PrevHash;
        public ulong Timestamp;
        public ulong Nonce;
        public UInt256[] TransactionHashes;

        public override int Size => base.Size
            + sizeof(uint)                      //Version
            + UInt256.Length                    //PrevHash
            + sizeof(ulong)                     //Timestamp
            + sizeof(ulong)                     // Nonce
            + TransactionHashes.GetVarSize();   //TransactionHashes

        public PrepareRequest() : base(ConsensusMessageType.PrepareRequest) { }

        public override void Deserialize(ref MemoryReader reader)
        {
            base.Deserialize(ref reader);
            Version = reader.ReadUInt32();
            PrevHash = reader.ReadSerializable<UInt256>();
            Timestamp = reader.ReadUInt64();
            Nonce = reader.ReadUInt64();
            TransactionHashes = reader.ReadSerializableArray<UInt256>(ushort.MaxValue);
            if (TransactionHashes.Distinct().Count() != TransactionHashes.Length)
                throw new FormatException();
        }

        public override bool Verify(ProtocolSettings protocolSettings)
        {
            if (!base.Verify(protocolSettings)) return false;
            return TransactionHashes.Length <= protocolSettings.MaxTransactionsPerBlock;
        }

        public override void Serialize(BinaryWriter writer)
        {
            base.Serialize(writer);
            writer.Write(Version);
            writer.Write(PrevHash);
            writer.Write(Timestamp);
            writer.Write(Nonce);
            writer.Write(TransactionHashes);
        }
    }
}
