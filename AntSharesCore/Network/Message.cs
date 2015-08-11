using AntShares.Cryptography;
using AntShares.IO;
using System;
using System.IO;
using System.Text;

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
            Message message = new Message
            {
                Command = command,
                Payload = payload
            };
            message.Checksum = message.Payload.Checksum();
            return message;
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

        public BinaryReader OpenReader()
        {
            return new BinaryReader(new MemoryStream(Payload, false), Encoding.UTF8);
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
