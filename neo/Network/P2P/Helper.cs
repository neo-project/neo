using System;
using System.IO;
using K4os.Compression.LZ4;
using K4os.Compression.LZ4.Streams;
using Neo.Network.P2P.Payloads;

namespace Neo.Network.P2P
{
    public static class Helper
    {
        private static readonly LZ4EncoderSettings CompressSettings = new LZ4EncoderSettings() { CompressionLevel = LZ4Level.L00_FAST };

        private static readonly LZ4DecoderSettings DecompressSettings = new LZ4DecoderSettings() { };

        public static byte[] DecompressLz4(this byte[] data)
        {
            using (var output = new MemoryStream())
            using (var input = new MemoryStream(data))
            using (var decoder = LZ4Stream.Decode(input, DecompressSettings, true))
            {
                int nRead;
                byte[] buffer = new byte[1024];

                while ((nRead = decoder.Read(buffer, 0, buffer.Length)) > 0)
                {
                    output.Write(buffer, 0, nRead);
                }

                return output.ToArray();
            }
        }

        public static byte[] CompressLz4(this byte[] data)
        {
            using (var stream = new MemoryStream())
            {
                using (var encoder = LZ4Stream.Encode(stream, CompressSettings, true))
                {
                    encoder.Write(data, 0, data.Length);
                }

                return stream.ToArray();
            }
        }

        public static byte[] GetHashData(this IVerifiable verifiable)
        {
            using (MemoryStream ms = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(ms))
            {
                verifiable.SerializeUnsigned(writer);
                writer.Flush();
                return ms.ToArray();
            }
        }

        internal static MessageCommand ToMessageCommand(this InventoryType inventoryType)
        {
            switch (inventoryType)
            {
                case InventoryType.TX:
                    return MessageCommand.Transaction;
                case InventoryType.Block:
                    return MessageCommand.Block;
                case InventoryType.Consensus:
                    return MessageCommand.Consensus;
                default:
                    throw new ArgumentOutOfRangeException(nameof(inventoryType));
            }
        }
    }
}
