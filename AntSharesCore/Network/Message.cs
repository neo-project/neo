using AntShares.Cryptography;
using AntShares.IO;
using System;
using System.IO;

namespace AntShares.Network
{
    internal class Message : ISerializable
    {
        public const UInt32 Magic = 0x00746e41;
        public string Command;
        public UInt32 Checksum;
        public byte[] Payload;

        public static Message Create(string command, Payload payload = null)
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

        public void Deserialize(BinaryReader reader)
        {
            if (reader.ReadUInt32() != Magic)
                throw new FormatException();
            this.Command = reader.ReadFixedString(12);
            UInt32 length = reader.ReadUInt32();
            if (length > 0x02000000)
                throw new FormatException();
            this.Checksum = reader.ReadUInt32();
            this.Payload = reader.ReadBytes((int)length);
            if (Payload.Checksum() != Checksum)
                throw new FormatException();
        }

        public BinaryReader OpenReader()
        {
            return new BinaryReader(new MemoryStream(Payload, false));
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(Magic);
            writer.WriteFixedString(Command, 12);
            writer.Write(Payload.Length);
            writer.Write(Checksum);
            writer.Write(Payload);
        }
    }
}
