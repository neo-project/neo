using Akka.IO;
using Neo.Cryptography;
using Neo.IO;
using Neo.IO.Caching;
using System;
using System.Buffers.Binary;
using System.IO;

namespace Neo.Network.P2P
{
    /// <summary>
    /// Represents a message on the NEO network.
    /// </summary>
    public class Message : ISerializable
    {
        /// <summary>
        /// Indicates the maximum size of <see cref="Payload"/>.
        /// </summary>
        public const int PayloadMaxSize = 0x02000000;

        private const int CompressionMinSize = 128;
        private const int CompressionThreshold = 64;

        /// <summary>
        /// The flags of the message.
        /// </summary>
        public MessageFlags Flags;

        /// <summary>
        /// The command of the message.
        /// </summary>
        public MessageCommand Command;

        /// <summary>
        /// The payload of the message.
        /// </summary>
        public ISerializable Payload;

        private byte[] _payload_compressed;

        public int Size => sizeof(MessageFlags) + sizeof(MessageCommand) + _payload_compressed.GetVarSize();

        /// <summary>
        /// Creates a new instance of the <see cref="Message"/> class.
        /// </summary>
        /// <param name="command">The command of the message.</param>
        /// <param name="payload">The payload of the message. For the messages that don't require a payload, it should be <see langword="null"/>.</param>
        /// <returns></returns>
        public static Message Create(MessageCommand command, ISerializable payload = null)
        {
            Message message = new()
            {
                Flags = MessageFlags.None,
                Command = command,
                Payload = payload,
                _payload_compressed = payload?.ToArray() ?? Array.Empty<byte>()
            };

            bool tryCompression =
                command == MessageCommand.Block ||
                command == MessageCommand.Extensible ||
                command == MessageCommand.Transaction ||
                command == MessageCommand.Headers ||
                command == MessageCommand.Addr ||
                command == MessageCommand.MerkleBlock ||
                command == MessageCommand.FilterLoad ||
                command == MessageCommand.FilterAdd;

            // Try compression
            if (tryCompression && message._payload_compressed.Length > CompressionMinSize)
            {
                var compressed = message._payload_compressed.CompressLz4();
                if (compressed.Length < message._payload_compressed.Length - CompressionThreshold)
                {
                    message._payload_compressed = compressed;
                    message.Flags |= MessageFlags.Compressed;
                }
            }

            return message;
        }

        private void DecompressPayload()
        {
            if (_payload_compressed.Length == 0) return;
            byte[] decompressed = Flags.HasFlag(MessageFlags.Compressed)
                ? _payload_compressed.DecompressLz4(PayloadMaxSize)
                : _payload_compressed;
            Payload = ReflectionCache<MessageCommand>.CreateSerializable(Command, decompressed);
        }

        void ISerializable.Deserialize(BinaryReader reader)
        {
            Flags = (MessageFlags)reader.ReadByte();
            Command = (MessageCommand)reader.ReadByte();
            _payload_compressed = reader.ReadVarBytes(PayloadMaxSize);
            DecompressPayload();
        }

        void ISerializable.Serialize(BinaryWriter writer)
        {
            writer.Write((byte)Flags);
            writer.Write((byte)Command);
            writer.WriteVarBytes(_payload_compressed);
        }

        internal static int TryDeserialize(ByteString data, out Message msg)
        {
            msg = null;
            if (data.Count < 3) return 0;

            var header = data.Slice(0, 3).ToArray();
            var flags = (MessageFlags)header[0];
            ulong length = header[2];
            var payloadIndex = 3;

            if (length == 0xFD)
            {
                if (data.Count < 5) return 0;
                length = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(payloadIndex, 2).ToArray());
                payloadIndex += 2;
            }
            else if (length == 0xFE)
            {
                if (data.Count < 7) return 0;
                length = BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(payloadIndex, 4).ToArray());
                payloadIndex += 4;
            }
            else if (length == 0xFF)
            {
                if (data.Count < 11) return 0;
                length = BinaryPrimitives.ReadUInt64LittleEndian(data.Slice(payloadIndex, 8).ToArray());
                payloadIndex += 8;
            }

            if (length > PayloadMaxSize) throw new FormatException();

            if (data.Count < (int)length + payloadIndex) return 0;

            msg = new Message()
            {
                Flags = flags,
                Command = (MessageCommand)header[1],
                _payload_compressed = length <= 0 ? Array.Empty<byte>() : data.Slice(payloadIndex, (int)length).ToArray()
            };
            msg.DecompressPayload();

            return payloadIndex + (int)length;
        }
    }
}
