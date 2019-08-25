using Neo.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Neo.Network.P2P.Payloads
{
    public class InvPayload : ISerializable
    {
        public const int MaxHashesCount = 500;

        public InventoryType Type;
        public UInt256[] Hashes;

        public int Size => sizeof(InventoryType) + Hashes.GetVarSize();

        public static InvPayload Create(InventoryType type, params UInt256[] hashes)
        {
            return new InvPayload
            {
                Type = type,
                Hashes = hashes
            };
        }

        public static IEnumerable<InvPayload> CreateGroup(InventoryType type, UInt256[] hashes)
        {
            for (int i = 0; i < hashes.Length; i += MaxHashesCount)
                yield return new InvPayload
                {
                    Type = type,
                    Hashes = hashes.Skip(i).Take(MaxHashesCount).ToArray()
                };
        }

        void ISerializable.Deserialize(BinaryReader reader)
        {
            Type = (InventoryType)reader.ReadByte();
            if (!Enum.IsDefined(typeof(InventoryType), Type))
                throw new FormatException();
            Hashes = reader.ReadSerializableArray<UInt256>(MaxHashesCount);
        }

        void ISerializable.Serialize(BinaryWriter writer)
        {
            writer.Write((byte)Type);
            writer.Write(Hashes);
        }
    }
}
