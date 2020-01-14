using Neo.IO;
using Neo.Network.P2P.Payloads;
using System.IO;

namespace Neo.Ledger
{
    public class ContractIdState : ICloneable<ContractIdState>, ISerializable
    {
        public uint ContractId = 0x00000000;

        int ISerializable.Size => sizeof(uint);

        ContractIdState ICloneable<ContractIdState>.Clone()
        {
            return new ContractIdState
            {
                ContractId = ContractId
            };
        }

        void ISerializable.Deserialize(BinaryReader reader)
        {
            ContractId = reader.ReadUInt32();
        }

        void ICloneable<ContractIdState>.FromReplica(ContractIdState replica)
        {
            ContractId = replica.ContractId;
        }

        void ISerializable.Serialize(BinaryWriter writer)
        {
            writer.Write(ContractId);
        }

        internal void Set(uint value)
        {
            ContractId = value;
        }
    }
}
