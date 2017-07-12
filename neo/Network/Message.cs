using Neo.Cryptography;
using Neo.IO;
using System;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Neo.Network
{
    public class Message : ISerializable
    {
        private const int PayloadMaxSize = 0x02000000;

        public static readonly uint Magic = Settings.Default.Magic;
        public string Command;
        public uint Checksum;
        public byte[] Payload;

        public int Size => sizeof(uint) + 12 + sizeof(int) + sizeof(uint) + Payload.Length;

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
            if (length > PayloadMaxSize)
                throw new FormatException();
            this.Checksum = reader.ReadUInt32();
            this.Payload = reader.ReadBytes((int)length);
            if (GetChecksum(Payload) != Checksum)
                throw new FormatException();
        }

        public static async Task<Message> DeserializeFromAsync(Stream stream, CancellationToken cancellationToken)
        {
            byte[] buffer = new byte[24];
            await FillBufferAsync(stream, buffer, cancellationToken);
            Message message = new Message();
            using (MemoryStream ms = new MemoryStream(buffer, false))
            using (BinaryReader reader = new BinaryReader(ms, Encoding.UTF8))
            {
                if (reader.ReadUInt32() != Magic)
                    throw new FormatException();
                message.Command = reader.ReadFixedString(12);
                uint length = reader.ReadUInt32();
                if (length > PayloadMaxSize)
                    throw new FormatException();
                message.Checksum = reader.ReadUInt32();
                message.Payload = new byte[length];
            }
            if (message.Payload.Length > 0)
                await FillBufferAsync(stream, message.Payload, cancellationToken);
            if (GetChecksum(message.Payload) != message.Checksum)
                throw new FormatException();
            return message;
        }

        public static async Task<Message> DeserializeFromAsync(WebSocket socket, CancellationToken cancellationToken)
        {
            byte[] buffer = new byte[24];
            await FillBufferAsync(socket, buffer, cancellationToken);
            Message message = new Message();
            using (MemoryStream ms = new MemoryStream(buffer, false))
            using (BinaryReader reader = new BinaryReader(ms, Encoding.UTF8))
            {
                if (reader.ReadUInt32() != Magic)
                    throw new FormatException();
                message.Command = reader.ReadFixedString(12);
                uint length = reader.ReadUInt32();
                if (length > PayloadMaxSize)
                    throw new FormatException();
                message.Checksum = reader.ReadUInt32();
                message.Payload = new byte[length];
            }
            if (message.Payload.Length > 0)
                await FillBufferAsync(socket, message.Payload, cancellationToken);
            if (GetChecksum(message.Payload) != message.Checksum)
                throw new FormatException();
            return message;
        }

        private static async Task FillBufferAsync(Stream stream, byte[] buffer, CancellationToken cancellationToken)
        {
            int offset = 0;
            while (offset < buffer.Length)
            {
                int count = await stream.ReadAsync(buffer, offset, buffer.Length - offset, cancellationToken);
                if (count <= 0) throw new IOException();
                offset += count;
            }
        }

        private static async Task FillBufferAsync(WebSocket socket, byte[] buffer, CancellationToken cancellationToken)
        {
            int offset = 0;
            while (offset < buffer.Length)
            {
                ArraySegment<byte> segment = new ArraySegment<byte>(buffer, offset, buffer.Length - offset);
                WebSocketReceiveResult result = await socket.ReceiveAsync(segment, cancellationToken);
                if (result.Count <= 0 || result.MessageType != WebSocketMessageType.Binary)
                    throw new IOException();
                offset += result.Count;
            }
        }

        private static uint GetChecksum(byte[] value)
        {
            return Crypto.Default.Hash256(value).ToUInt32(0);
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
