using AntShares.Cryptography;
using AntShares.IO;
using System;
using System.IO;
using System.Threading.Tasks;

namespace AntShares.Network
{
    internal class Message : ISerializable
    {
#if TESTNET
        public const uint Magic = 0x74746e41;
#else
        public const uint Magic = 0x00746e41;
#endif
        public string Command;
        public uint Checksum;
        public byte[] Payload;

        public static Message Create(string command, ISerializable payload = null)
        {
            return Create(command, payload == null ? new byte[0] : payload.ToArray());
        }

        public static Message Create(string command, byte[] payload)
        {
            return new Message
            {
                Command = command,
                Checksum = payload.Checksum(),
                Payload = payload
            };
        }

        void ISerializable.Deserialize(BinaryReader reader)
        {
            if (reader.ReadUInt32() != Magic)
                throw new FormatException();
            this.Command = reader.ReadFixedString(12);
            uint length = reader.ReadUInt32();
            if (length > 0x02000000)
                throw new FormatException();
            this.Checksum = reader.ReadUInt32();
            this.Payload = reader.ReadBytes((int)length);
            if (Payload.Checksum() != Checksum)
                throw new FormatException();
        }

        public static async Task<Message> DeserializeFromStreamAsync(Stream stream)
        {
            byte[] buffer = new byte[sizeof(uint) + 12 + sizeof(uint) + sizeof(uint)];
            await stream.ReadAsync(buffer, 0, buffer.Length);
            Message message = new Message();
            using (MemoryStream ms = new MemoryStream(buffer, false))
            using (BinaryReader reader = new BinaryReader(ms))
            {
                if (reader.ReadUInt32() != Magic)
                    throw new FormatException();
                message.Command = reader.ReadFixedString(12);
                uint length = reader.ReadUInt32();
                if (length > 0x02000000)
                    throw new FormatException();
                message.Checksum = reader.ReadUInt32();
                message.Payload = new byte[length];
            }
            await stream.ReadAsync(message.Payload, 0, message.Payload.Length);
            return message;
        }

        void ISerializable.Serialize(BinaryWriter writer)
        {
            writer.Write(Magic);
            writer.WriteFixedString(Command, 12);
            writer.Write(Payload.Length);
            writer.Write(Checksum);
            writer.Write(Payload);
        }
    }
}
