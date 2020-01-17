using Neo.IO;
using System;
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
            if (Id < 0) throw new FormatException();
        }

        void ICloneable<ContractIdState>.FromReplica(ContractIdState replica)
        {
            Id = replica.Id;
        }

        void ISerializable.Serialize(BinaryWriter writer)
        {
            writer.Write(Id);
        }
    }
}
