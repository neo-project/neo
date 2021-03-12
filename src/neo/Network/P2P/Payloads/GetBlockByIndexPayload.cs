using Neo.IO;
using System;
using System.IO;

namespace Neo.Network.P2P.Payloads
{
    /// <summary>
    /// This message is sent to request for blocks.
    /// </summary>
    public class GetBlockByIndexPayload : ISerializable
    {
        /// <summary>
        /// The starting index of the blocks to request.
        /// </summary>
        public uint IndexStart;

        /// <summary>
        /// The number of blocks to request.
        /// </summary>
        public short Count;

        public int Size => sizeof(uint) + sizeof(short);

        /// <summary>
        /// Creates a new instance of the <see cref="GetBlockByIndexPayload"/> class.
        /// </summary>
        /// <param name="index_start">The starting index of the blocks to request.</param>
        /// <param name="count">The number of blocks to request. Set this parameter to -1 to request as many blocks as possible.</param>
        /// <returns>The created payload.</returns>
        public static GetBlockByIndexPayload Create(uint index_start, short count = -1)
        {
            return new GetBlockByIndexPayload
            {
                IndexStart = index_start,
                Count = count
            };
        }

        void ISerializable.Deserialize(BinaryReader reader)
        {
            IndexStart = reader.ReadUInt32();
            Count = reader.ReadInt16();
            if (Count < -1 || Count == 0 || Count > HeadersPayload.MaxHeadersCount)
                throw new FormatException();
        }

        void ISerializable.Serialize(BinaryWriter writer)
        {
            writer.Write(IndexStart);
            writer.Write(Count);
        }
    }
}
