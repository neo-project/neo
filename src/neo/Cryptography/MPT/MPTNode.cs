using Neo.IO.Caching;
using Neo.IO.Json;
using System;
using System.IO;
using System.Text;

namespace Neo.Cryptography.MPT
{
    public abstract class MPTNode
    {
        private UInt256 hash;

        protected abstract NodeType Type { get; }

        protected virtual UInt256 GenHash()
        {
            return new UInt256(Crypto.Hash256(Encode()));
        }

        public UInt256 GetHash()
        {
            return hash ??= GenHash();
        }

        public void SetDirty()
        {
            hash = null;
        }

        public byte[] Encode()
        {
            using MemoryStream ms = new MemoryStream();
            using BinaryWriter writer = new BinaryWriter(ms, Encoding.UTF8, true);

            writer.Write((byte)Type);
            EncodeSpecific(writer);
            writer.Flush();

            return ms.ToArray();
        }

        public abstract void EncodeSpecific(BinaryWriter writer);

        public static unsafe MPTNode Decode(ReadOnlySpan<byte> data)
        {
            if (data.Length < 1) return null;

            fixed (byte* pointer = data)
            {
                using UnmanagedMemoryStream stream = new UnmanagedMemoryStream(pointer, data.Length);
                using BinaryReader reader = new BinaryReader(stream);

                MPTNode n = (MPTNode)ReflectionCache<NodeType>.CreateInstance((NodeType)reader.ReadByte());
                if (n is null) throw new InvalidOperationException();

                n.DecodeSpecific(reader);
                return n;
            }
        }

        public abstract void DecodeSpecific(BinaryReader reader);

        public abstract JObject ToJson();
    }
}
