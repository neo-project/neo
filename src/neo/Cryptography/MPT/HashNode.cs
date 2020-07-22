using Neo.IO;
using System;
using System.IO;

namespace Neo.Cryptography.MPT
{
    public class HashNode : MPTNode
    {
        private UInt256 hash;

        public override UInt256 Hash => hash;
        protected override NodeType Type => NodeType.HashNode;
        public bool IsEmpty => Hash is null;
        public static HashNode EmptyNode { get; } = new HashNode();

        public HashNode()
        {
        }

        public HashNode(UInt256 hash)
        {
            this.hash = hash;
        }

        internal override void EncodeSpecific(BinaryWriter writer)
        {
            WriteHash(writer, hash);
        }

        internal override void DecodeSpecific(BinaryReader reader)
        {
            byte[] buffer = reader.ReadVarBytes(UInt256.Length);
            hash = buffer.Length switch
            {
                0 => null,
                UInt256.Length => new UInt256(buffer),
                _ => throw new FormatException()
            };
        }
    }
}
