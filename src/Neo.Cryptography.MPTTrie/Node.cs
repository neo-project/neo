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
        private UInt256? _hash;
        public int Reference { get; internal set; }
        public UInt256 Hash => _hash ??= new UInt256(Crypto.Hash256(ToArrayWithoutReference()));
        public NodeType Type { get; internal set; }
        public bool IsEmpty => Type == NodeType.Empty;
        public int Size
        {
            get
            {
                var size = sizeof(NodeType);
                return Type switch
                {
                    NodeType.BranchNode => size + BranchSize + Reference.GetVarSize(),
                    NodeType.ExtensionNode => size + ExtensionSize + Reference.GetVarSize(),
                    NodeType.LeafNode => size + LeafSize + Reference.GetVarSize(),
                    NodeType.HashNode => size + HashSize,
                    NodeType.Empty => size,
                    _ => throw new InvalidOperationException($"{nameof(Node)} Cannt get size, unsupport type"),
                };
            }
        }

        public Node()
        {
            Type = NodeType.Empty;
        }

        public void SetDirty()
        {
            _hash = null;
        }

        public int SizeAsChild
        {
            get
            {
                return Type switch
                {
                    NodeType.BranchNode or NodeType.ExtensionNode or NodeType.LeafNode => NewHash(Hash).Size,
                    NodeType.HashNode or NodeType.Empty => Size,
                    _ => throw new InvalidOperationException(nameof(Node)),
                };
            }
        }

        public void SerializeAsChild(BinaryWriter writer)
        {
            switch (Type)
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
            writer.Write((byte)Type);
            switch (Type)
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
            if (Type == NodeType.BranchNode || Type == NodeType.ExtensionNode || Type == NodeType.LeafNode)
                writer.WriteVarInt(Reference);
        }

        public byte[] ToArrayWithoutReference()
        {
            using var ms = new MemoryStream();
            using var writer = new BinaryWriter(ms, Utility.StrictUTF8, true);

            SerializeWithoutReference(writer);
            writer.Flush();
            return ms.ToArray();
        }

        public void Deserialize(ref MemoryReader reader)
        {
            Type = (NodeType)reader.ReadByte();
            switch (Type)
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
            return Type switch
            {
                NodeType.BranchNode or NodeType.ExtensionNode or NodeType.LeafNode => new Node
                {
                    Type = NodeType.HashNode,
                    _hash = Hash,
                },
                NodeType.HashNode or NodeType.Empty => Clone(),
                _ => throw new InvalidOperationException(nameof(Clone)),
            };
        }

        public Node Clone()
        {
            switch (Type)
            {
                case NodeType.BranchNode:
                    var n = new Node
                    {
                        Type = Type,
                        Reference = Reference,
                        Children = new Node[BranchChildCount],
                    };
                    for (var i = 0; i < BranchChildCount; i++)
                    {
                        n.Children[i] = Children[i].CloneAsChild();
                    }
                    return n;
                case NodeType.ExtensionNode:
                    return new Node
                    {
                        Type = Type,
                        Key = Key,
                        Next = Next!.CloneAsChild(), // Next not null if ExtensionNode
                        Reference = Reference,
                    };
                case NodeType.LeafNode:
                    return new Node
                    {
                        Type = Type,
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
