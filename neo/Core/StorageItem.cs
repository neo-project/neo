using Neo.IO;
using System.IO;

namespace Neo.Core
{
    public class StorageItem : StateBase, ICloneable<StorageItem>
    {
        public byte[] Value;

        public override int Size => base.Size + Value.GetVarSize();

        StorageItem ICloneable<StorageItem>.Clone()
        {
            return new StorageItem
            {
                Value = Value
            };
        }

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);
            Value = reader.ReadVarBytes();
        }

        void ICloneable<StorageItem>.FromReplica(StorageItem replica)
        {
            Value = replica.Value;
        }

        public override void Serialize(BinaryWriter writer)
        {
            base.Serialize(writer);
            writer.WriteVarBytes(Value);
        }
    }
}
