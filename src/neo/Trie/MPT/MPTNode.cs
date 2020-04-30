using Neo.Cryptography;
using Neo.IO.Json;
using System.IO;
using System.Text;

namespace Neo.Trie.MPT
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

        public static MPTNode Decode(byte[] data)
        {
            if (data is null || data.Length < 1)
                return null;

            using BinaryReader reader = new BinaryReader(new MemoryStream(data, false), Encoding.UTF8, false);

            var n = (NodeType)reader.ReadByte() switch
            {
                NodeType.BranchNode => (MPTNode)new BranchNode(),
                NodeType.ExtensionNode => new ExtensionNode(),
                NodeType.LeafNode => new LeafNode(),
                _ => throw new System.InvalidOperationException(),
            };

            n.DecodeSpecific(reader);
            return n;
        }

        public abstract void DecodeSpecific(BinaryReader reader);

        public abstract JObject ToJson();
    }
}
