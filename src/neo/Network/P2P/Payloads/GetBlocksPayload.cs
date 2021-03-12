using Neo.IO;
using System;
using System.IO;

namespace Neo.Network.P2P.Payloads
{
    /// <summary>
    /// This message is sent to request for blocks.
    /// </summary>
    public class GetBlocksPayload : ISerializable
    {
        /// <summary>
        /// The starting hash of the blocks to request.
        /// </summary>
        public UInt256 HashStart;

        /// <summary>
        /// The number of blocks to request.
        /// </summary>
        public short Count;

        public int Size => sizeof(short) + HashStart.Size;

        /// <summary>
        /// Creates a new instance of the <see cref="GetBlocksPayload"/> class.
        /// </summary>
        /// <param name="hash_start">The starting hash of the blocks to request.</param>
        /// <param name="count">The number of blocks to request. Set this parameter to -1 to request as many blocks as possible.</param>
        /// <returns>The created payload.</returns>
        public static GetBlocksPayload Create(UInt256 hash_start, short count = -1)
        {
            return new GetBlocksPayload
            {
                HashStart = hash_start,
                Count = count
            };
        }

        void ISerializable.Deserialize(BinaryReader reader)
        {
            HashStart = reader.ReadSerializable<UInt256>();
            Count = reader.ReadInt16();
            if (Count < -1 || Count == 0) throw new FormatException();
        }

        void ISerializable.Serialize(BinaryWriter writer)
        {
            writer.Write(HashStart);
            writer.Write(Count);
        }
    }
}
