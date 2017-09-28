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

        public MessageCommand Command;
        public byte[] Payload;

        public int Size => sizeof(uint) + 12 + sizeof(int) + sizeof(uint) + Payload.Length;

        public static Message Create(MessageCommand command, ISerializable payload = null)
        {
            return Create(command, payload == null ? new byte[0] : payload.ToArray());
        }

        public static Message Create(MessageCommand command, byte[] payload)
        {
            return new Message
            {
                Command = command,
                Payload = payload
            };
        }

        void ISerializable.Deserialize(BinaryReader reader)
        {
            if (reader.ReadUInt32() != Magic)
                throw new FormatException();
            if (!Enum.TryParse(reader.ReadFixedString(12), out this.Command))
                throw new FormatException();
            uint length = reader.ReadUInt32();
            if (length > PayloadMaxSize)
                throw new FormatException();

            uint checksum = reader.ReadUInt32();
            this.Payload = reader.ReadBytes((int)length);
            if (GetChecksum(Payload) != checksum)
                throw new FormatException();
        }

        public static async Task<Message> DeserializeFromAsync(Stream stream, CancellationToken cancellationToken)
        {
            byte[] buffer = new byte[24];
            await FillBufferAsync(stream, buffer, cancellationToken);

            uint checksum;
            Message message = new Message();
            using (MemoryStream ms = new MemoryStream(buffer, false))
            using (BinaryReader reader = new BinaryReader(ms, Encoding.UTF8))
            {
                if (reader.ReadUInt32() != Magic)
                    throw new FormatException();
                if (!Enum.TryParse(reader.ReadFixedString(12), out message.Command))
                    throw new FormatException();
                uint length = reader.ReadUInt32();
                if (length > PayloadMaxSize)
                    throw new FormatException();

                checksum = reader.ReadUInt32();
                message.Payload = new byte[length];
            }
            if (message.Payload.Length > 0)
                await FillBufferAsync(stream, message.Payload, cancellationToken);
            if (GetChecksum(message.Payload) != checksum)
                throw new FormatException();
            return message;
        }

        public static async Task<Message> DeserializeFromAsync(WebSocket socket, CancellationToken cancellationToken)
        {
            byte[] buffer = new byte[24];
            await FillBufferAsync(socket, buffer, cancellationToken);

            uint checksum;
            Message message = new Message();
            using (MemoryStream ms = new MemoryStream(buffer, false))
            using (BinaryReader reader = new BinaryReader(ms, Encoding.UTF8))
            {
                if (reader.ReadUInt32() != Magic)
                    throw new FormatException();
                if (!Enum.TryParse(reader.ReadFixedString(12), out message.Command))
                    throw new FormatException();
                uint length = reader.ReadUInt32();
                if (length > PayloadMaxSize)
                    throw new FormatException();

                checksum = reader.ReadUInt32();
                message.Payload = new byte[length];
            }
            if (message.Payload.Length > 0)
                await FillBufferAsync(socket, message.Payload, cancellationToken);
            if (GetChecksum(message.Payload) != checksum)
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
            writer.WriteFixedString(Command.ToString(), 12);
            writer.Write(Payload.Length);
            writer.Write(GetChecksum(Payload));
            writer.Write(Payload);
        }
    }
}
