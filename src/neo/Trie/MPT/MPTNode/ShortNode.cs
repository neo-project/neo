using System.IO;
using Neo.Cryptography;
using Neo.IO;

namespace Neo.Trie.MPT
{
    public class ShortNode : MPTNode
    {
        public byte[] Key;

        public MPTNode Next;

        public new int Size => Key.Length + Next.Size;

        protected override byte[] CalHash()
        {
            return Key.Concat(Next.GetHash()).Sha256();
        }
        public ShortNode()
        {
            nType = NodeType.ShortNode;
        }

        public ShortNode Clone()
        {
            var cloned = new ShortNode()
            {
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
            Key = reader.ReadVarBytes();
            var hashNode = new HashNode(reader.ReadVarBytes());
            Next = hashNode;
        }
    }
}
