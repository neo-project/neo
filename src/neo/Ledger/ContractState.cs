using Neo.IO;
using Neo.IO.Json;
using Neo.SmartContract;
using Neo.SmartContract.Manifest;
using Neo.VM;
using Neo.VM.Types;
using System;
using System.IO;
using Array = Neo.VM.Types.Array;

namespace Neo.Ledger
{
    public class ContractState : ICloneable<ContractState>, ISerializable, IInteroperable
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
            json["script"] = Convert.ToBase64String(Script);
            json["manifest"] = Manifest.ToJson();
            return json;
        }

        public static ContractState FromJson(JObject json)
        {
            ContractState contractState = new ContractState();
            contractState.Script = Convert.FromBase64String(json["script"].AsString());
            contractState.Manifest = ContractManifest.FromJson(json["manifest"]);
            return contractState;
        }

        public StackItem ToStackItem(ReferenceCounter referenceCounter)
        {
            return new Array(referenceCounter, new StackItem[] { Script, HasStorage, Payable });
        }
    }
}
