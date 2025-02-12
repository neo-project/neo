// Copyright (C) 2015-2025 The Neo Project.
//
// Node.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Extensions;
using Neo.IO;
using System;
using System.IO;

namespace Neo.Cryptography.MPTTrie
{
    public partial class Node : ISerializable
    {
        private NodeType type;
        private UInt256 hash;
        public int Reference;
        public UInt256 Hash => hash ??= new UInt256(Crypto.Hash256(ToArrayWithoutReference()));
        public NodeType Type => type;
        public bool IsEmpty => type == NodeType.Empty;
        public int Size
        {
            get
            {
                int size = sizeof(NodeType);
                switch (type)
                {
                    case NodeType.BranchNode:
                        return size + BranchSize + UnsafeData.GetVarSize(Reference);
                    case NodeType.ExtensionNode:
                        return size + ExtensionSize + UnsafeData.GetVarSize(Reference);
                    case NodeType.LeafNode:
                        return size + LeafSize + UnsafeData.GetVarSize(Reference);
                    case NodeType.HashNode:
                        return size + HashSize;
                    case NodeType.Empty:
                        return size;
                    default:
                        throw new InvalidOperationException($"{nameof(Node)} Cannt get size, unsupport type");
                }
            }
        }

        public Node()
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
                        throw new InvalidOperationException(nameof(Node));
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
            using BinaryWriter writer = new BinaryWriter(ms, Utility.StrictUTF8, true);

            SerializeWithoutReference(writer);
            writer.Flush();
            return ms.ToArray();
        }

        public void Deserialize(ref MemoryReader reader)
        {
            type = (NodeType)reader.ReadByte();
            switch (type)
            {
                case NodeType.BranchNode:
                    DeserializeBranch(ref reader);
                    Reference = (int)reader.ReadVarInt();
                    break;
                case NodeType.ExtensionNode:
                    DeserializeExtension(ref reader);
                    Reference = (int)reader.ReadVarInt();
                    break;
                case NodeType.LeafNode:
                    DeserializeLeaf(ref reader);
                    Reference = (int)reader.ReadVarInt();
                    break;
                case NodeType.Empty:
                    break;
                case NodeType.HashNode:
                    DeserializeHash(ref reader);
                    break;
                default:
                    throw new FormatException(nameof(Deserialize));
            }
        }

        private Node CloneAsChild()
        {
            switch (type)
            {
                case NodeType.BranchNode:
                case NodeType.ExtensionNode:
                case NodeType.LeafNode:
                    return new Node
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

        public Node Clone()
        {
            switch (type)
            {
                case NodeType.BranchNode:
                    var n = new Node
                    {
                        type = type,
                        Reference = Reference,
                        Children = new Node[BranchChildCount],
                    };
                    for (int i = 0; i < BranchChildCount; i++)
                    {
                        n.Children[i] = Children[i].CloneAsChild();
                    }
                    return n;
                case NodeType.ExtensionNode:
                    return new Node
                    {
                        type = type,
                        Key = Key,
                        Next = Next.CloneAsChild(),
                        Reference = Reference,
                    };
                case NodeType.LeafNode:
                    return new Node
                    {
                        type = type,
                        Value = Value,
                        Reference = Reference,
                    };
                case NodeType.HashNode:
                case NodeType.Empty:
                    return this;
                default:
                    throw new InvalidOperationException(nameof(Clone));
            }
        }

        public void FromReplica(Node n)
        {
            MemoryReader reader = new(n.ToArray());
            Deserialize(ref reader);
        }
    }
}
