using System.IO;
using Neo.IO;
using Neo.Cryptography;

namespace Neo.Trie.MPT
{
    enum NodeType
    {
        BranchNode,
        ExtensionNode,
        LeafNode,
        HashNode,
        ValueNode
    }
    public class NodeFlag
    {
        public byte[] Hash;
        public bool dirty;
    }
    public abstract class MPTNode: ISerializable
    {

        public NodeFlag Flag { get; }

        public int Size { get; }

        public static MPTNode Decode(byte[] data)
        {
            var nodeType = (NodeType)data[0];
            data = data.Skip(1);
            switch (nodeType)
            {
                case NodeType.BranchNode:
                    return BranchNode.Decode(data);
                case NodeType.ExtensionNode:
                    return ExtensionNode.Decode(data);
                case NodeType.LeafNode:
                    return LeafNode.Decode(data);
                case NodeType.ValueNode:
                    return ValueNode.Decode(data);
                default:
                    throw new System.Exception();
            }
        }

    }

    public class ExtensionNode : MPTNode
    {
        public byte[] Key;
        public MPTNode Next;

        public ExtensionNode Clone()
        {
            var cloned = new ExtensionNode();
            cloned.Key = (byte[])Key.Clone();
            cloned.Next = Next;
            return cloned;
        }

        public byte[] Encode()
        {
            return new byte[]{};
        }

        public new static ExtensionNode Decode(byte[] data)
        {
            var n = new ExtensionNode();
            return n;
        }
    }

    public class BranchNode : MPTNode
    {
        public MPTNode[] Children = new MPTNode[17];

        public BranchNode Clone()
        {
            var cloned = new BranchNode();
            for (int i = 0; i < Children.Length; i++)
            {
                cloned.Children[i] = Children[i];
            }
            return cloned;
        }

        public override void Serialize(BinaryWriter writer)
        {
            writer.WriteNullableArray<MPTNode>(Children);
        }

        public override void Deserialize(BinaryReader reader)
        {

        }
    }

    public class LeafNode : MPTNode
    {
        public byte[] Key;
        public MPTNode Value;

        public override void Serialize(BinaryWriter writer)
        {
            
        }

        public override void Deserialize(BinaryReader reader)
        {
            Value = new HashNode(reader.ReadVarBytes());
        }
    }

    public class HashNode : MPTNode
    {
        public byte[] Hash;

        public HashNode(byte[] hash)
        {
            Hash = new byte[hash.Length];
            hash.CopyTo(Hash, 0);
        }

        public override void Serialize(BinaryWriter writer)
        {
            writer.WriteVarBytes(Hash);
        }

        public override void Deserialize(BinaryReader reader)
        {
            Hash = reader.ReadVarBytes();
        }
    }

    public class ValueNode : MPTNode
    {
        public byte[] Value;

        public ValueNode(byte[] val)
        {
            Value = new byte[val.Length];
            val.CopyTo(Value, 0);
        }

        public override void Serialize(BinaryWriter writer)
        {
            writer.WriteVarBytes(Value);
        }

        public override void Deserialize(BinaryReader reader)
        {
            Value = reader.ReadVarBytes();
        }
    }
}
