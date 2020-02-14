using Neo.Cryptography;
using Neo.IO;
using System.IO;

namespace Neo.Trie.MPT
{
    public class ShortNode : MPTNode
    {
        public byte[] Key;
        public MPTNode Next;

        protected override byte[] CalHash()
        {
            return Key.Concat(Next.GetHash()).Sha256();
        }

        public ShortNode()
        {
            nType = NodeType.ShortNode;
        }

        public override void EncodeSpecific(BinaryWriter writer)
        {
            writer.WriteVarBytes(Key);
            var hashNode = new HashNode(Next.GetHash());
            hashNode.EncodeSpecific(writer);
        }

        public override void DecodeSpecific(BinaryReader reader)
        {
            Key = reader.ReadVarBytes();
            var hashNode = new HashNode();
            hashNode.DecodeSpecific(reader);
            Next = hashNode;
        }
    }
}
