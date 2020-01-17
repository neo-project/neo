using Neo.IO;
using System;
using System.IO;

namespace Neo.Ledger
{
    public class ContractIdState : ICloneable<ContractIdState>, ISerializable
    {
        public int NextId;

        int ISerializable.Size => sizeof(int);

        ContractIdState ICloneable<ContractIdState>.Clone()
        {
            return new ContractIdState
            {
                NextId = NextId
            };
        }

        void ISerializable.Deserialize(BinaryReader reader)
        {
            NextId = reader.ReadInt32();
            if (NextId < 0) throw new FormatException();
        }

        void ICloneable<ContractIdState>.FromReplica(ContractIdState replica)
        {
            NextId = replica.NextId;
        }

        void ISerializable.Serialize(BinaryWriter writer)
        {
            writer.Write(NextId);
        }
    }
}
