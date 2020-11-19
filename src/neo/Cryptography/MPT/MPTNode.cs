using Neo.IO;
using System;
using System.IO;

namespace Neo.Cryptography.MPT
{
    public partial class MPTNode : ICloneable<MPTNode>, ISerializable
    {
        private NodeType type;
        private UInt256 hash;
        public int Reference;
        public UInt256 Hash => hash ??= new UInt256(Crypto.Hash256(this.ToArrayWithoutReference()));
        public NodeType Type => type;
        public bool IsEmpty => type == NodeType.Empty;
        public int Size
        {
            get
            {
                int size = sizeof(NodeType);
                return type switch
                {
                    NodeType.BranchNode => size + BranchSize + IO.Helper.GetVarSize(Reference),
                    NodeType.ExtensionNode => size + ExtensionSize + IO.Helper.GetVarSize(Reference),
                    NodeType.LeafNode => size + LeafSize + IO.Helper.GetVarSize(Reference),
                    NodeType.HashNode => size + HashSize,
                    NodeType.Empty => size,
                    _ => throw new InvalidOperationException($"{nameof(MPTNode)} Cannt get size, unsupport type")
                };
            }
        }

        public MPTNode()
        {
            type = NodeType.Empty;
        }

        public void SetDirty()
        {
            hash = null;
        }

        public int SizeAsChild
        {
            get
            {
                switch (type)
                {
                    case NodeType.BranchNode:
                    case NodeType.ExtensionNode:
                    case NodeType.LeafNode:
                        return NewHash(Hash).Size;
                    case NodeType.HashNode:
                    case NodeType.Empty:
                        return Size;
                    default:
                        throw new InvalidOperationException(nameof(MPTNode));
                }
            }
        }

        public void SerializeAsChild(BinaryWriter writer)
        {
            switch (type)
            {
                case NodeType.BranchNode:
                case NodeType.ExtensionNode:
                case NodeType.LeafNode:
                    var n = NewHash(Hash);
                    n.Serialize(writer);
                    break;
                case NodeType.HashNode:
                case NodeType.Empty:
                    Serialize(writer);
                    break;
                default:
                    throw new FormatException(nameof(SerializeAsChild));
            }
        }

        private void SerializeWithoutReference(BinaryWriter writer)
        {
            writer.Write((byte)type);
            switch (type)
            {
                case NodeType.BranchNode:
                    SerializeBranch(writer);
                    break;
                case NodeType.ExtensionNode:
                    SerializeExtension(writer);
                    break;
                case NodeType.LeafNode:
                    SerializeLeaf(writer);
                    break;
                case NodeType.HashNode:
                    SerializeHash(writer);
                    break;
                case NodeType.Empty:
                    break;
                default:
                    throw new FormatException(nameof(SerializeWithoutReference));
            }
        }

        public void Serialize(BinaryWriter writer)
        {
            SerializeWithoutReference(writer);
            if (type == NodeType.BranchNode || type == NodeType.ExtensionNode || type == NodeType.LeafNode)
                writer.WriteVarInt(Reference);
        }

        public byte[] ToArrayWithoutReference()
        {
            using MemoryStream ms = new MemoryStream();
            using BinaryWriter writer = new BinaryWriter(ms, Utility.StrictUTF8);

            SerializeWithoutReference(writer);
            writer.Flush();
            return ms.ToArray();
        }

        public void Deserialize(BinaryReader reader)
        {
            type = (NodeType)reader.ReadByte();
            switch (type)
            {
                case NodeType.BranchNode:
                    DeserializeBranch(reader);
                    Reference = (int)reader.ReadVarInt();
                    break;
                case NodeType.ExtensionNode:
                    DeserializeExtension(reader);
                    Reference = (int)reader.ReadVarInt();
                    break;
                case NodeType.LeafNode:
                    DeserializeLeaf(reader);
                    Reference = (int)reader.ReadVarInt();
                    break;
                case NodeType.Empty:
                    break;
                case NodeType.HashNode:
                    DeserializeHash(reader);
                    break;
                default:
                    throw new FormatException(nameof(Deserialize));
            }
        }

        private MPTNode CloneAsChild()
        {
            switch (type)
            {
                case NodeType.BranchNode:
                case NodeType.ExtensionNode:
                case NodeType.LeafNode:
                    return new MPTNode
                    {
                        type = NodeType.HashNode,
                        hash = Hash,
                    };
                case NodeType.HashNode:
                case NodeType.Empty:
                    return Clone();
                default:
                    throw new InvalidOperationException(nameof(Clone));
            }
        }

        public MPTNode Clone()
        {
            switch (type)
            {
                case NodeType.BranchNode:
                    var n = new MPTNode
                    {
                        type = type,
                        Reference = Reference,
                        Children = new MPTNode[BranchChildCount],
                    };
                    for (int i = 0; i < BranchChildCount; i++)
                    {
                        n.Children[i] = Children[i].CloneAsChild();
                    }
                    return n;
                case NodeType.ExtensionNode:
                    return new MPTNode
                    {
                        type = type,
                        Key = (byte[])Key.Clone(),
                        Next = Next.CloneAsChild(),
                        Reference = Reference,
                    };
                case NodeType.LeafNode:
                    return new MPTNode
                    {
                        type = type,
                        Value = (byte[])Value.Clone(),
                        Reference = Reference,
                    };
                case NodeType.HashNode:
                case NodeType.Empty:
                    return this;
                default:
                    throw new InvalidOperationException(nameof(Clone));
            }
        }

        public void FromReplica(MPTNode n)
        {
            using MemoryStream ms = new MemoryStream(n.ToArray());
            using BinaryReader reader = new BinaryReader(ms, Utility.StrictUTF8);

            Deserialize(reader);
        }
    }
}
