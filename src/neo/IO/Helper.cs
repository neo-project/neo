using System;
using System.Buffers;
using System.Buffers.Binary;
using K4os.Compression.LZ4;

namespace Neo.IO
{
    public static class Helper
    {
        public static byte[] CompressLz4(this byte[] data)
        {
            int maxLength = LZ4Codec.MaximumOutputSize(data.Length);
            using var buffer = MemoryPool<byte>.Shared.Rent(maxLength);
            int length = LZ4Codec.Encode(data, buffer.Memory.Span);
            byte[] result = new byte[sizeof(uint) + length];
            BinaryPrimitives.WriteInt32LittleEndian(result, data.Length);
            buffer.Memory[..length].CopyTo(result.AsMemory(4));
            return result;
        }

        public static byte[] DecompressLz4(this byte[] data, int maxOutput)
        {
            int length = BinaryPrimitives.ReadInt32LittleEndian(data);
            if (length < 0 || length > maxOutput) throw new FormatException();
            byte[] result = new byte[length];
            if (LZ4Codec.Decode(data.AsSpan(4), result) != length)
                throw new FormatException();
            return result;
        }
    }
}
