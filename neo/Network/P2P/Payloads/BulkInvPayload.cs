using Neo.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Neo.Network.P2P.Payloads
{
    public class BulkInvPayload : ISerializable
    {
        public const int MaxEntriesCount = 500;
        public const int MaxSize = 512 * 1024; // 512Kb packet

        public InventoryType Type;
        public IInventory[] Values;

        public int Size =>
            sizeof(InventoryType) + // Type
            Values.GetVarSize();    // Values

        /// <summary>
        /// Create group of payloads
        /// </summary>
        /// <param name="type">Type</param>
        /// <param name="hashes">Hashes</param>
        /// <param name="source">Source</param>
        /// <returns>Groups of BulkInvPayload</returns>
        public static IEnumerable<Message> CreateGroup(InventoryType type, UInt256[] hashes, Func<UInt256, IInventory> source)
        {
            int size = 0;
            var pack = new List<IInventory>();

            foreach (var hash in hashes)
            {
                var value = source(hash);
                if (value == null) continue;

                var currentSize = value.Size;
                if (size + currentSize > MaxSize || pack.Count >= MaxEntriesCount)
                {
                    // Iterate this bulk payload
                    yield return Create(type, pack);
                    pack.Clear();
                    size = 0;
                }

                // Add it to the current bulk payload
                size += currentSize;
                pack.Add(value);
            }

            if (pack.Count > 0)
            {
                yield return Create(type, pack);
            }
        }

        /// <summary>
        /// Create BulkInvPayload message
        /// </summary>
        /// <param name="type">Type</param>
        /// <param name="inventories">Inventories</param>
        /// <returns>BulkInvPayload</returns>
        private static Message Create(InventoryType type, IEnumerable<IInventory> inventories)
        {
            var data = inventories.ToArray();

            if (data.Length == 1)
            {
                // If the group only have one entry, we will return it as a regular message

                switch (type)
                {
                    case InventoryType.TX: return Message.Create(MessageCommand.Transaction, data[0]);
                    case InventoryType.Block: return Message.Create(MessageCommand.Block, data[0]);
                    case InventoryType.Consensus: return Message.Create(MessageCommand.Consensus, data[0]);
                    default: throw new FormatException($"Unexpected inventory type {type}");
                }
            }

            // Return BulkInv message

            return Message.Create(MessageCommand.BulkInv, new BulkInvPayload
            {
                Type = type,
                Values = data
            });
        }

        public void Deserialize(BinaryReader reader)
        {
            Type = (InventoryType)reader.ReadByte();
            switch (Type)
            {
                case InventoryType.TX:
                    {
                        Values = reader.ReadSerializableArray<Transaction>();
                        break;
                    }
                case InventoryType.Block:
                    {
                        Values = reader.ReadSerializableArray<Block>();
                        break;
                    }
                case InventoryType.Consensus:
                    {
                        Values = reader.ReadSerializableArray<ConsensusPayload>();
                        break;
                    }
                default: throw new FormatException($"Unexpected inventory type {Type}");
            }
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write((byte)Type);
            writer.Write(Values);
        }
    }
}
