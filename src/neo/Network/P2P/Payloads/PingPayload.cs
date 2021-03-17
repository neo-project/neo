using Neo.IO;
using System;
using System.IO;

namespace Neo.Network.P2P.Payloads
{
    /// <summary>
    /// Sent to detect whether the connection has been disconnected.
    /// </summary>
    public class PingPayload : ISerializable
    {
        /// <summary>
        /// The latest block index.
        /// </summary>
        public uint LastBlockIndex;

        /// <summary>
        /// The timestamp when the message was sent.
        /// </summary>
        public uint Timestamp;

        /// <summary>
        /// A random number. This number must be the same in <see cref="MessageCommand.Ping"/> and <see cref="MessageCommand.Pong"/> messages.
        /// </summary>
        public uint Nonce;

        public int Size =>
            sizeof(uint) +  //LastBlockIndex
            sizeof(uint) +  //Timestamp
            sizeof(uint);   //Nonce

        /// <summary>
        /// Creates a new instance of the <see cref="PingPayload"/> class.
        /// </summary>
        /// <param name="height">The latest block index.</param>
        /// <returns>The created payload.</returns>
        public static PingPayload Create(uint height)
        {
            Random rand = new();
            return Create(height, (uint)rand.Next());
        }

        /// <summary>
        /// Creates a new instance of the <see cref="PingPayload"/> class.
        /// </summary>
        /// <param name="height">The latest block index.</param>
        /// <param name="nonce">The random number.</param>
        /// <returns>The created payload.</returns>
        public static PingPayload Create(uint height, uint nonce)
        {
            return new PingPayload
            {
                LastBlockIndex = height,
                Timestamp = TimeProvider.Current.UtcNow.ToTimestamp(),
                Nonce = nonce
            };
        }

        void ISerializable.Deserialize(BinaryReader reader)
        {
            LastBlockIndex = reader.ReadUInt32();
            Timestamp = reader.ReadUInt32();
            Nonce = reader.ReadUInt32();
        }

        void ISerializable.Serialize(BinaryWriter writer)
        {
            writer.Write(LastBlockIndex);
            writer.Write(Timestamp);
            writer.Write(Nonce);
        }
    }
}
