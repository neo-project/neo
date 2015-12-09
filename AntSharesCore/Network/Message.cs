using AntShares.Cryptography;
using AntShares.IO;
using System;
using System.IO;

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
                Checksum = GetChecksum(payload),
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
            if (GetChecksum(Payload) != Checksum)
                throw new FormatException();
        }

        private static uint GetChecksum(byte[] value)
        {
            return BitConverter.ToUInt32(value.Sha256().Sha256(), 0);
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
