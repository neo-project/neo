using Neo.IO.Json;
using System;
using System.IO;
using System.Text;

namespace Neo.Trie.MPT
{
    public enum NodeType
    {
        FullNode = 0x00,
        ShortNode = 0x01,
        HashNode = 0x02,
        ValueNode = 0x03,
    }

    public class NodeFlag
    {
        public byte[] Hash;
        public bool Dirty;

        public NodeFlag()
        {
            Dirty = true;
        }
    }

    public abstract class MPTNode
    {
        public NodeFlag Flag;
        protected NodeType nType;

        protected abstract byte[] GenHash();

        public virtual byte[] GetHash()
        {
            if (!Flag.Dirty && Flag.Hash.Length > 0) return Flag.Hash;
            Flag.Hash = GenHash();
            Flag.Dirty = false;
            return (byte[])Flag.Hash.Clone();
        }

        public void ResetFlag()
        {
            Flag = new NodeFlag();
        }
        public MPTNode()
        {
            Flag = new NodeFlag();
        }

        public byte[] Encode()
        {
            using (MemoryStream ms = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(ms, Encoding.UTF8))
            {
                writer.Write((byte)nType);
                EncodeSpecific(writer);
                writer.Flush();
                return ms.ToArray();
            }
        }

        public abstract void EncodeSpecific(BinaryWriter writer);

        public static MPTNode Decode(byte[] data)
        {
            if (data is null || data.Length == 0)
                return null;

            using (MemoryStream ms = new MemoryStream(data, false))
            using (BinaryReader reader = new BinaryReader(ms, Encoding.UTF8))
            {
                var nodeType = (NodeType)reader.ReadByte();
                switch (nodeType)
                {
                    case NodeType.FullNode:
                        {
                            var n = new FullNode();
                            n.DecodeSpecific(reader);
                            return n;
                        }
                    case NodeType.ShortNode:
                        {
                            var n = new ShortNode();
                            n.DecodeSpecific(reader);
                            return n;
                        }
                    case NodeType.ValueNode:
                        {
                            var n = new ValueNode();
                            n.DecodeSpecific(reader);
                            return n;
                        }
                    default:
                        throw new System.InvalidOperationException();
                }
            }
        }

        public abstract void DecodeSpecific(BinaryReader reader);

        public abstract JObject ToJson();
    }
}
