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
        public uint ContractId;
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

        int ISerializable.Size => 4 + Script.GetVarSize() + Manifest.ToJson().ToString().GetVarSize();

        ContractState ICloneable<ContractState>.Clone()
        {
            return new ContractState
            {
                ContractId = ContractId,
                Script = Script,
                Manifest = Manifest.Clone()
            };
        }

        void ISerializable.Deserialize(BinaryReader reader)
        {
            ContractId = reader.ReadUInt32();
            Script = reader.ReadVarBytes();
            Manifest = reader.ReadSerializable<ContractManifest>();
        }

        void ICloneable<ContractState>.FromReplica(ContractState replica)
        {
            ContractId = replica.ContractId;
            Script = replica.Script;
            Manifest = replica.Manifest.Clone();
        }

        void ISerializable.Serialize(BinaryWriter writer)
        {
            writer.Write(ContractId);
            writer.WriteVarBytes(Script);
            writer.Write(Manifest);
        }

        public JObject ToJson()
        {
            JObject json = new JObject();
            json["contractId"] = ContractId;
            json["hash"] = ScriptHash.ToString();
            json["script"] = Convert.ToBase64String(Script);
            json["manifest"] = Manifest.ToJson();
            return json;
        }

        public StackItem ToStackItem(ReferenceCounter referenceCounter)
        {
            return new Array(referenceCounter, new StackItem[] { Script, HasStorage, Payable });
        }
    }
}
