using Neo.Cryptography;
using Neo.IO;
using System.IO;
using System.Text;

namespace Neo.Trie.MPT
{
    public enum NodeType
    {
        FullNode,
        ShortNode,
        HashNode,
        ValueNode,
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

    public abstract class MPTNode : ISerializable
    {
        public NodeFlag Flag;
        protected NodeType nType;

        protected abstract byte[] CalHash();

        public virtual byte[] GetHash()
        {
            if (!Flag.Dirty) return Flag.Hash;
            Flag.Hash = CalHash();
            Flag.Dirty = false;
            return (byte[])Flag.Hash.Clone();
        }

        public void ResetFlag()
        {
            Flag = new NodeFlag();
        }

        public int Size { get; }

        public MPTNode()
        {
            Flag = new NodeFlag();
        }

        public virtual void Serialize(BinaryWriter writer)
        {
            writer.Write((byte)nType);
        }

        public virtual void Deserialize(BinaryReader reader)
        {

        }

        public byte[] Encode()
        {
            return this.ToArray();
        }

        public static MPTNode Decode(byte[] data)
        {
            var nodeType = (NodeType)data[0];
            data = data.Skip(1);

            using (MemoryStream ms = new MemoryStream(data, false))
            using (BinaryReader reader = new BinaryReader(ms, Encoding.UTF8))
            {
                switch (nodeType)
                {
                    case NodeType.FullNode:
                        {
                            var n = new FullNode();
                            n.Deserialize(reader);
                            return n;
                        }
                    case NodeType.ShortNode:
                        {
                            var n = new ShortNode();
                            n.Deserialize(reader);
                            return n;
                        }
                    case NodeType.ValueNode:
                        {
                            var n = new ValueNode();
                            n.Deserialize(reader);
                            return n;
                        }
                    default:
                        throw new System.Exception();
                }
            }
        }
    }
}
