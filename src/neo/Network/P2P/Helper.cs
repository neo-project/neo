using K4os.Compression.LZ4;
using Neo.Network.P2P.Payloads;
using System;
using System.Buffers;
using System.IO;

namespace Neo.Network.P2P
{
    public static class Helper
    {
        public static byte[] CompressLz4(this byte[] data)
        {
            int maxLength = LZ4Codec.MaximumOutputSize(data.Length);
            byte[] buffer = ArrayPool<byte>.Shared.Rent(maxLength);
            int length = LZ4Codec.Encode(data, 0, data.Length, buffer, 0, buffer.Length);
            data = new byte[length];
            Buffer.BlockCopy(buffer, 0, data, 0, length);
            ArrayPool<byte>.Shared.Return(buffer);
            return data;
        }

        public static byte[] DecompressLz4(this byte[] data, int maxOutput)
        {
            maxOutput = Math.Min(maxOutput, data.Length * 255);
            byte[] buffer = ArrayPool<byte>.Shared.Rent(maxOutput);
            try
            {
                int length = LZ4Codec.Decode(data, 0, data.Length, buffer, 0, buffer.Length);
                if (length < 0 || length > maxOutput) throw new FormatException();
                data = new byte[length];
                Buffer.BlockCopy(buffer, 0, data, 0, length);
                return data;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
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
