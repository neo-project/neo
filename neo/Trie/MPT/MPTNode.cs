using Neo.Cryptography;
using Neo.IO.Json;
using System;
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
            MemoryStream ms = null;
            try
            {
                ms = new MemoryStream();
                using (BinaryWriter writer = new BinaryWriter(ms, Encoding.UTF8))
                {
                    writer.Write((byte)nType);
                    EncodeSpecific(writer);
                    writer.Flush();
                    return ms.ToArray();
                }
            }
            finally
            {
                if (ms != null)
                    ms.Dispose();
            }
        }

        public abstract void EncodeSpecific(BinaryWriter writer);

        public static MPTNode Decode(byte[] data)
        {
            if (data is null || data.Length == 0)
                return null;

            MemoryStream ms = null;
            try
            {
                ms = new MemoryStream(data, false);
                using (BinaryReader reader = new BinaryReader(ms, Encoding.UTF8))
                {
                    var nodeType = (NodeType)reader.ReadByte();
                    switch (nodeType)
                    {
                        case NodeType.BranchNode:
                            {
                                var n = new BranchNode();
                                n.DecodeSpecific(reader);
                                return n;
                            }
                        case NodeType.ExtensionNode:
                            {
                                var n = new ExtensionNode();
                                n.DecodeSpecific(reader);
                                return n;
                            }
                        case NodeType.LeafNode:
                            {
                                var n = new LeafNode();
                                n.DecodeSpecific(reader);
                                return n;
                            }
                        default:
                            throw new System.InvalidOperationException();
                    }
                }
            }
            finally
            {
                if (ms != null)
                    ms.Dispose();
            }
        }

        public abstract void DecodeSpecific(BinaryReader reader);

        public abstract JObject ToJson();
    }
}
