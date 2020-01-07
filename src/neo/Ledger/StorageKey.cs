using Neo.Cryptography;
using Neo.IO;
using System;
using System.IO;

namespace Neo.Ledger
{
    public class StorageKey : IEquatable<StorageKey>, ISerializable
    {
        public Guid Guid;
        public byte[] Key;

        int ISerializable.Size => 16 + (Key.Length / 16 + 1) * 17;

        internal static byte[] CreateSearchPrefix(Guid guid, byte[] prefix)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                int index = 0;
                int remain = prefix.Length;
                while (remain >= 16)
                {
                    ms.Write(prefix, index, 16);
                    ms.WriteByte(16);
                    index += 16;
                    remain -= 16;
                }
                if (remain > 0)
                    ms.Write(prefix, index, remain);
                return Helper.Concat(guid.ToByteArray(), ms.ToArray());
            }
        }

        void ISerializable.Deserialize(BinaryReader reader)
        {
            Guid = new Guid(reader.ReadBytes(16));
            Key = reader.ReadBytesWithGrouping();
        }

        public bool Equals(StorageKey other)
        {
            if (other is null)
                return false;
            if (ReferenceEquals(this, other))
                return true;
            return Guid.Equals(other.Guid) && MemoryExtensions.SequenceEqual<byte>(Key, other.Key);
        }

        public override bool Equals(object obj)
        {
            if (!(obj is StorageKey other)) return false;
            return Equals(other);
        }

        public override int GetHashCode()
        {
            return Guid.GetHashCode() + (int)Key.Murmur32(0);
        }

        void ISerializable.Serialize(BinaryWriter writer)
        {
            writer.Write(Guid.ToByteArray());
            writer.WriteBytesWithGrouping(Key);
        }
    }
}
