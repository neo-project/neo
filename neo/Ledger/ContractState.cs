using Neo.IO;
using Neo.IO.Json;
using Neo.SmartContract;
using System.IO;

namespace Neo.Ledger
{
    public class ContractState : ICloneable<ContractState>, ISerializable
    {
        public byte[] Script;
        public ContractPropertyState ContractProperties;

        public bool HasStorage => ContractProperties.HasFlag(ContractPropertyState.HasStorage);
        public bool Payable => ContractProperties.HasFlag(ContractPropertyState.Payable);

        private UInt160 _scriptHash;
        public UInt160 ScriptHash
        {
            get
            {
                if (_scriptHash == null)
                {
                    _scriptHash = Script.ToScriptHash();
                }
                return _scriptHash;
            }
        }

        int ISerializable.Size => Script.GetVarSize() + sizeof(ContractParameterType);

        ContractState ICloneable<ContractState>.Clone()
        {
            return new ContractState
            {
                Script = Script,
                ContractProperties = ContractProperties
            };
        }

        void ISerializable.Deserialize(BinaryReader reader)
        {
            Script = reader.ReadVarBytes();
            ContractProperties = (ContractPropertyState)reader.ReadByte();
        }

        void ICloneable<ContractState>.FromReplica(ContractState replica)
        {
            Script = replica.Script;
            ContractProperties = replica.ContractProperties;
        }

        void ISerializable.Serialize(BinaryWriter writer)
        {
            writer.WriteVarBytes(Script);
            writer.Write((byte)ContractProperties);
        }

        public JObject ToJson()
        {
            JObject json = new JObject();
            json["hash"] = ScriptHash.ToString();
            json["script"] = Script.ToHexString();
            json["properties"] = new JObject();
            json["properties"]["storage"] = HasStorage;
            json["properties"]["payable"] = Payable;
            return json;
        }
    }
}
