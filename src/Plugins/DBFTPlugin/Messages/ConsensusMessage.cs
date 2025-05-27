// Copyright (C) 2015-2025 The Neo Project.
//
// ConsensusMessage.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Extensions;
using Neo.IO;
using Neo.Plugins.DBFTPlugin.Types;
using System;
using System.IO;

namespace Neo.Plugins.DBFTPlugin.Messages
{
    public abstract class ConsensusMessage : ISerializable
    {
        public readonly ConsensusMessageType Type;
        public uint BlockIndex;
        public byte ValidatorIndex;
        public byte ViewNumber;

        public virtual int Size =>
            sizeof(ConsensusMessageType) +  //Type
            sizeof(uint) +                  //BlockIndex
            sizeof(byte) +                  //ValidatorIndex
            sizeof(byte);                   //ViewNumber

        protected ConsensusMessage(ConsensusMessageType type)
        {
            if (!Enum.IsDefined(typeof(ConsensusMessageType), type))
                throw new ArgumentOutOfRangeException(nameof(type));
            Type = type;
        }

        public virtual void Deserialize(ref MemoryReader reader)
        {
            if (Type != (ConsensusMessageType)reader.ReadByte())
                throw new FormatException();
            BlockIndex = reader.ReadUInt32();
            ValidatorIndex = reader.ReadByte();
            ViewNumber = reader.ReadByte();
        }

        public static ConsensusMessage DeserializeFrom(ReadOnlyMemory<byte> data)
        {
            ConsensusMessageType type = (ConsensusMessageType)data.Span[0];
            Type t = typeof(ConsensusMessage);
            t = t.Assembly.GetType($"{t.Namespace}.{type}", false);
            if (t is null) throw new FormatException();
            return (ConsensusMessage)data.AsSerializable(t);
        }

        public virtual bool Verify(ProtocolSettings protocolSettings)
        {
            return ValidatorIndex < protocolSettings.ValidatorsCount;
        }

        public virtual void Serialize(BinaryWriter writer)
        {
            writer.Write((byte)Type);
            writer.Write(BlockIndex);
            writer.Write(ValidatorIndex);
            writer.Write(ViewNumber);
        }
    }
}
