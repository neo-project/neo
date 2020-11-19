using Neo.IO;
using Neo.SmartContract;
using System;
using System.IO;

namespace Neo.Cryptography.MPT
{
    public partial class MPTNode : ICloneable<MPTNode>, ISerializable
    {
        public const int MaxValueLength = 3 + ApplicationEngine.MaxStorageValueSize + sizeof(bool);
        public byte[] Value;

        public static MPTNode NewLeaf(byte[] value)
        {
            if (value is null) throw new ArgumentNullException(nameof(NewLeaf));
            if (value.Length == 0) throw new InvalidOperationException(nameof(NewLeaf));
            var n = new MPTNode
            {
                type = NodeType.LeafNode,
                Value = value,
                Reference = 1,
            };
            return n;
        }

        protected int LeafSize => Value.GetVarSize();

        private void SerializeLeaf(BinaryWriter writer)
        {
            writer.WriteVarBytes(Value);
        }

        private void DeserializeLeaf(BinaryReader reader)
        {
            Value = reader.ReadVarBytes();
        }
    }
}
