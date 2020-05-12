using Neo.IO.Json;
using System;
using System.IO;
using System.Text;

namespace Neo.Cryptography.MPT
{
    public abstract class MPTNode
    {
        private UInt256 hash;
        public bool Dirty { get; private set; }
        protected NodeType nType;

        protected virtual UInt256 GenHash()
        {
            return new UInt256(Crypto.Hash256(this.Encode()));
        }

        public virtual UInt256 GetHash()
        {
            if (!Dirty && !(hash is null)) return hash;
            hash = GenHash();
            Dirty = false;
            return hash;
        }

        public void SetDirty()
        {
            Dirty = true;
        }

        public MPTNode()
        {
            Dirty = true;
        }

        public byte[] Encode()
        {
            using MemoryStream ms = new MemoryStream();
            using BinaryWriter writer = new BinaryWriter(ms, Encoding.UTF8, true);

            writer.Write((byte)nType);
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

                var n = (NodeType)reader.ReadByte() switch
                {
                    NodeType.BranchNode => (MPTNode)new BranchNode(),
                    NodeType.ExtensionNode => new ExtensionNode(),
                    NodeType.LeafNode => new LeafNode(),
                    _ => throw new InvalidOperationException(),
                };

                n.DecodeSpecific(reader);
                return n;
            }
        }

        public abstract void DecodeSpecific(BinaryReader reader);

        public abstract JObject ToJson();
    }
}
