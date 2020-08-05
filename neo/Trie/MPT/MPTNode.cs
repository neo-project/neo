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
            return new UInt256(Crypto.Default.Hash256(this.Encode()));
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
            using (BinaryWriter writer = new BinaryWriter(new MemoryStream(), Encoding.UTF8, false))
            {
                writer.Write((byte)nType);
                EncodeSpecific(writer);
                writer.Flush();
                return ((MemoryStream)writer.BaseStream).ToArray();
            }
        }

        public abstract void EncodeSpecific(BinaryWriter writer);

        public static MPTNode Decode(byte[] data)
        {
            if (data is null || data.Length == 0)
                return null;

            MPTNode node;
            using (BinaryReader reader = new BinaryReader(new MemoryStream(data, false), Encoding.UTF8))
            {
                var nodeType = (NodeType)reader.ReadByte();
                switch (nodeType)
                {
                    case NodeType.BranchNode:
                        {
                            node = new BranchNode();
                            break;
                        }
                    case NodeType.ExtensionNode:
                        {
                            node = new ExtensionNode();
                            break;
                        }
                    case NodeType.LeafNode:
                        {
                            node = new LeafNode();
                            break;
                        }
                    default:
                        throw new System.InvalidOperationException();
                }

                node.DecodeSpecific(reader);
            }
            return node;
        }

        public abstract void DecodeSpecific(BinaryReader reader);

        public abstract JObject ToJson();
    }
}
