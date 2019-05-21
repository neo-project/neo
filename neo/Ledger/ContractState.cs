using Neo.IO;
using Neo.IO.Json;
using Neo.SmartContract;
using System.IO;

namespace Neo.Ledger
{
    public class ContractState : ICloneable<ContractState>, ISerializable
    {
        public byte[] Script;

        public ContractManifest Manifest;

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

        public bool HasStorage => Manifest.Features.HasFlag(ContractPropertyState.HasStorage);
        public bool Payable => Manifest.Features.HasFlag(ContractPropertyState.Payable);

        int ISerializable.Size => Script.GetVarSize() + Manifest.ToJson().GetVarSize();

        ContractState ICloneable<ContractState>.Clone()
        {
            return new ContractState
            {
                Script = Script,
                Manifest = Manifest
            };
        }

        void ISerializable.Deserialize(BinaryReader reader)
        {
            Script = reader.ReadVarBytes();
            Manifest = ContractManifest.Parse(reader.ReadVarString(ushort.MaxValue));
        }

        void ICloneable<ContractState>.FromReplica(ContractState replica)
        {
            Script = replica.Script;
            Manifest = replica.Manifest.Clone();
        }

        void ISerializable.Serialize(BinaryWriter writer)
        {
            writer.WriteVarBytes(Script);
            writer.WriteVarString(Manifest.ToJson());
        }

        public JObject ToJson()
        {
            JObject json = new JObject();
            json["hash"] = ScriptHash.ToString();
            json["script"] = Script.ToHexString();
            json["properties"] = new JObject();
            json["properties"]["storage"] = Manifest.Features.HasFlag(ContractPropertyState.HasStorage);
            json["properties"]["payable"] = Manifest.Features.HasFlag(ContractPropertyState.Payable);
            return json;
        }
    }
}
