using Neo.IO;
using Neo.SmartContract;
using System;
using System.IO;

namespace Neo.Cryptography.MPT
{
    public partial class MPTNode : ICloneable<MPTNode>, ISerializable
    {
        public const int MaxKeyLength = (ApplicationEngine.MaxStorageKeySize + sizeof(int)) * 2;
        public byte[] Key;
        public MPTNode Next;

        public static MPTNode NewExtension(byte[] key, MPTNode next)
        {
            if (key is null || next is null) throw new ArgumentNullException(nameof(NewExtension));
            if (key.Length == 0) throw new InvalidOperationException(nameof(NewExtension));
            var n = new MPTNode
            {
                type = NodeType.ExtensionNode,
                Key = key,
                Next = next,
                Reference = 1,
            };
            return n;
        }

        protected int ExtensionSize => Key.GetVarSize() + Next.SizeAsChild;

        private void SerializeExtension(BinaryWriter writer)
        {
            writer.WriteVarBytes(Key);
            Next.SerializeAsChild(writer);
        }

        private void DeserializeExtension(BinaryReader reader)
        {
            Key = reader.ReadVarBytes();
            var n = new MPTNode();
            n.Deserialize(reader);
            Next = n;
        }
    }
}
