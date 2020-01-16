using System.IO;
using System.Text;
using Neo.IO;
using Neo.IO.Json;
using Neo.Cryptography;

namespace Neo.Trie.MPT
{
    public enum NodeType
    {
        FullNode,
        ShortNode,
        HashNode,
        ValueNode,
        NullNode = 0xFF
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
    
    public abstract class MPTNode: ISerializable
    {
        public NodeFlag Flag;
        protected NodeType nType;

        protected abstract byte[] calHash();
        
        public virtual byte[] GetHash() {
            if (!Flag.Dirty) return Flag.Hash;
            Flag.Hash = calHash();
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

        public abstract JObject ToJson();
    }

    public class ShortNode : MPTNode
    {
        public byte[] Key;

        public MPTNode Next;

        public new int Size => Key.Length + Next.Size;

        protected override byte[] calHash(){
            return Key.Concat(Next.GetHash()).Sha256();
        }
        public ShortNode()
        {
            nType = NodeType.ShortNode;
        }

        public ShortNode Clone()
        {
            var cloned = new ShortNode() {
                Key = (byte[])Key.Clone(),
                Next = Next,
            };
            return cloned;
        }

        public override void Serialize(BinaryWriter writer)
        {
            base.Serialize(writer);
            writer.WriteVarBytes(Key);
            writer.WriteVarBytes(Next.GetHash());
        }

        public override void Deserialize(BinaryReader reader)
        {
            var hashNode = new HashNode();
            Key = reader.ReadVarBytes();
            hashNode.Deserialize(reader);
            Next = hashNode;
        }

        public override JObject ToJson()
        {
            var json = new JObject();
            json["key"] = Key.ToHexString();
            json["next"] = Next.ToJson();
            return json;
        }
    }

    public class FullNode : MPTNode
    {
        public MPTNode[] Children = new MPTNode[17];

        public new int Size;

        public FullNode()
        {
            nType = NodeType.FullNode;
            for (int i = 0; i < Children.Length; i++)
            {
                Children[i] = HashNode.EmptyNode();
            }
        }

        protected override byte[] calHash()
        {
            var bytes = new byte[0];
            for (int i = 0; i < Children.Length; i++)
            {
                bytes = bytes.Concat(Children[i].GetHash());
            }
            return bytes.Sha256();
        }

        public FullNode Clone()
        {
            var cloned = new FullNode();
            for (int i = 0; i < Children.Length; i++)
            {
                cloned.Children[i] = Children[i];
            }
            return cloned;
        }

        public override void Serialize(BinaryWriter writer)
        {
            base.Serialize(writer);
            for (int i = 0; i < Children.Length; i++)
            {
                writer.WriteVarBytes(Children[i].GetHash());
            }
        }

        public override void Deserialize(BinaryReader reader)
        {
            for (int i = 0; i < Children.Length; i++)
            {
                var hashNode = new HashNode(reader.ReadVarBytes());
                Children[i] = hashNode;
            }
        }

        public override JObject ToJson()
        {
            var json = new JObject();
            var jchildren = new JArray();
            for (int i = 0; i < Children.Length; i++)
            {
                jchildren.Add(Children[i].ToJson());
            }
            json["children"] = jchildren;
            return json;
        }
    }

    public class HashNode : MPTNode
    {
        public byte[] Hash;

        public HashNode()
        {
            nType = NodeType.HashNode;
        }   

        public HashNode(byte[] hash)
        {
            nType = NodeType.HashNode;
            Hash = (byte[])hash.Clone();
        }
        
        protected override byte[] calHash()
        {
            return (byte[])Hash.Clone();
        }

        public static HashNode EmptyNode()
        {
            return new HashNode(new byte[]{});
        }

        public bool IsEmptyNode => Hash.Length == 0;

        public override void Serialize(BinaryWriter writer)
        {
            base.Serialize(writer);
            writer.WriteVarBytes(Hash);
        }

        public override void Deserialize(BinaryReader reader)
        {
            Hash = reader.ReadVarBytes();
        }

        public override JObject ToJson()
        {
            var json = new JObject();
            if (!this.IsEmptyNode) 
            {
                json["hash"] = Hash.ToHexString();
            }
            return json;
        }
    }

    public class ValueNode : MPTNode
    {
        public byte[] Value;

        protected override byte[] calHash()
        {
            return Value.Length < 32 ? (byte[])Value.Clone() : Value.Sha256();
        }

        public ValueNode()
        {
            nType = NodeType.ValueNode;
        }

        public ValueNode(byte[] val)
        {
            nType = NodeType.ValueNode;
            Value = (byte[])val.Clone();
        }

        public override void Serialize(BinaryWriter writer)
        {
            base.Serialize(writer);
            writer.WriteVarBytes(Value);
        }

        public override void Deserialize(BinaryReader reader)
        {
            Value = reader.ReadVarBytes();
        }

        public override JObject ToJson()
        {
            var json = new JObject();
            json["value"] = Value.ToHexString();
            return json;
        }
    }
}
