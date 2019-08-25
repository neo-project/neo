using Neo.IO;
using Neo.IO.Json;
using Neo.SmartContract;
using Neo.SmartContract.Manifest;
using System.IO;

namespace Neo.Ledger
{
    public class ContractState : ICloneable<ContractState>, ISerializable
    {
        public byte[] Script;
        public ContractManifest Manifest;

        public bool HasStorage => Manifest.Features.HasFlag(ContractFeatures.HasStorage);
        public bool Payable => Manifest.Features.HasFlag(ContractFeatures.Payable);

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

        int ISerializable.Size => Script.GetVarSize() + Manifest.ToJson().ToString().GetVarSize();

        ContractState ICloneable<ContractState>.Clone()
        {
            return new ContractState
            {
                Script = Script,
                Manifest = Manifest.Clone()
            };
        }

        void ISerializable.Deserialize(BinaryReader reader)
        {
            Script = reader.ReadVarBytes();
            Manifest = reader.ReadSerializable<ContractManifest>();
        }

        void ICloneable<ContractState>.FromReplica(ContractState replica)
        {
            Script = replica.Script;
            Manifest = replica.Manifest.Clone();
        }

        void ISerializable.Serialize(BinaryWriter writer)
        {
            writer.WriteVarBytes(Script);
            writer.Write(Manifest);
        }

        public JObject ToJson()
        {
            JObject json = new JObject();
            json["hash"] = ScriptHash.ToString();
            json["script"] = Script.ToHexString();
            json["manifest"] = Manifest.ToJson();
            return json;
        }

        public static ContractState FromJson(JObject json)
        {
            ContractState contractState = new ContractState();
            contractState.Script = json["script"].AsString().HexToBytes();
            contractState.Manifest = ContractManifest.FromJson(json["manifest"]);
            return contractState;
        }
    }
}
