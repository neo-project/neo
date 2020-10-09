using Neo.IO;
using Neo.IO.Caching;
using System;
using System.IO;

namespace Neo.Cryptography.MPT
{
    public abstract class MPTNode
    {
        private UInt256 hash;
        public long Reference = 1;
        public virtual UInt256 Hash => hash ??= new UInt256(Crypto.Hash256(Encode()));
        public bool IsEmpty => this is HashNode hn && hn.Hash is null;
        public static HashNode EmptyNode { get; } = new HashNode();
        protected abstract NodeType Type { get; }

        public void SetDirty()
        {
            hash = null;
        }

        public byte[] Encode()
        {
            using MemoryStream ms = new MemoryStream();
            using BinaryWriter writer = new BinaryWriter(ms);

            writer.Write((byte)Type);
            EncodeSpecific(writer);
            writer.Flush();

            return ms.ToArray();
        }

        public byte[] EncodeWithReference()
        {
            using MemoryStream ms = new MemoryStream();
            using BinaryWriter writer = new BinaryWriter(ms);

            writer.Write((byte)Type);
            EncodeSpecific(writer);
            writer.WriteVarInt(Reference);
            writer.Flush();

            return ms.ToArray();
        }

        internal abstract void EncodeSpecific(BinaryWriter writer);

        public void EncodeAsChild(BinaryWriter writer)
        {
            if (Hash is null)
                writer.Write((byte)0);
            else
                writer.WriteVarBytes(Hash.ToArray());
        }

        public static unsafe MPTNode Decode(ReadOnlySpan<byte> data)
        {
            if (data.IsEmpty) return null;

            fixed (byte* pointer = data)
            {
                using UnmanagedMemoryStream stream = new UnmanagedMemoryStream(pointer, data.Length);
                using BinaryReader reader = new BinaryReader(stream);

                MPTNode n = (MPTNode)ReflectionCache<NodeType>.CreateInstance((NodeType)reader.ReadByte());
                if (n is null) throw new InvalidOperationException();

                n.DecodeSpecific(reader);
                n.Reference = (long)reader.ReadVarInt();

                return n;
            }
        }

        internal abstract void DecodeSpecific(BinaryReader reader);
    }
}
