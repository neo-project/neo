using Neo.Cryptography;
using Neo.IO;
using Neo.Ledger;
using System;
using System.IO;
using System.Linq;

namespace Neo.SmartContract.Native
{
    internal class KeyBuilder
    {
        private readonly int id;
        private readonly MemoryStream stream = new MemoryStream();

        public KeyBuilder(int id, byte prefix)
        {
            this.id = id;
            this.stream.WriteByte(prefix);
        }

        public KeyBuilder Add(ReadOnlySpan<byte> key)
        {
            stream.Write(key);
            return this;
        }

        public KeyBuilder Add(ISerializable key)
        {
            using (BinaryWriter writer = new BinaryWriter(stream, Utility.StrictUTF8, true))
            {
                key.Serialize(writer);
                writer.Flush();
            }
            return this;
        }

        unsafe public KeyBuilder Add<T>(T key) where T : unmanaged
        {
            return Add(new ReadOnlySpan<byte>(&key, sizeof(T)));
        }

        unsafe public KeyBuilder AddBigEndian<T>(T key) where T : unmanaged
        {
            return Add(new ReadOnlySpan<byte>(&key, sizeof(T)).ToArray().Reverse().ToArray());
        }

        public byte[] ToArray()
        {
            using (stream)
            {
                return StorageKey.CreateSearchPrefix(id, stream.ToArray());
            }
        }

        public static implicit operator StorageKey(KeyBuilder builder)
        {
            using (builder.stream)
            {
                return new StorageKey
                {
                    Id = builder.id,
                    Key = builder.stream.ToArray()
                };
            }
        }
    }
}
