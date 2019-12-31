using Neo.IO;
using System.IO;

namespace Neo.Ledger
{
    public class HeaderHashList : ICloneable<HeaderHashList>, ISerializable
    {
        public UInt256[] Hashes;

        int ISerializable.Size => Hashes.GetVarSize();

        HeaderHashList ICloneable<HeaderHashList>.Clone()
        {
            return new HeaderHashList
            {
                Hashes = Hashes
            };
        }

        void ISerializable.Deserialize(BinaryReader reader)
        {
            Hashes = reader.ReadSerializableArray<UInt256>();
        }

        void ICloneable<HeaderHashList>.FromReplica(HeaderHashList replica)
        {
            Hashes = replica.Hashes;
        }

        void ISerializable.Serialize(BinaryWriter writer)
        {
            writer.Write(Hashes);
        }
    }
}
