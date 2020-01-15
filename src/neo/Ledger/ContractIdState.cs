using Neo.IO;
using System.IO;

namespace Neo.Ledger
{
    public class ContractIdState : ICloneable<ContractIdState>, ISerializable
    {
        public int Id;

        int ISerializable.Size => sizeof(int);

        ContractIdState ICloneable<ContractIdState>.Clone()
        {
            return new ContractIdState
            {
                Id = Id
            };
        }

        void ISerializable.Deserialize(BinaryReader reader)
        {
            Id = reader.ReadInt32();
        }

        void ICloneable<ContractIdState>.FromReplica(ContractIdState replica)
        {
            Id = replica.Id;
        }

        void ISerializable.Serialize(BinaryWriter writer)
        {
            writer.Write(Id);
        }

        internal void Set(int value)
        {
            Id = value;
        }
    }
}
