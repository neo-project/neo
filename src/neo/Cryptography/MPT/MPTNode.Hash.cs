using Neo.IO;
using System;
using System.IO;

namespace Neo.Cryptography.MPT
{
    public partial class MPTNode : ICloneable<MPTNode>, ISerializable
    {
        public static MPTNode NewHash(UInt256 hash)
        {
            if (hash is null) throw new ArgumentNullException(nameof(NewHash));
            var n = new MPTNode
            {
                type = NodeType.HashNode,
                hash = hash,
            };
            return n;
        }

        protected int HashSize => hash.Size;

        private void SerializeHash(BinaryWriter writer)
        {
            writer.Write(hash);
        }

        private void DeserializeHash(BinaryReader reader)
        {
            hash = reader.ReadSerializable<UInt256>();
        }
    }
}
