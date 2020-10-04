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
        public int Id;
        public byte[] Script;
        public ContractAbi Abi;
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

        int ISerializable.Size => sizeof(int) + Script.GetVarSize() + Abi.Size + Manifest.Size;

        ContractState ICloneable<ContractState>.Clone()
        {
            return new ContractState
            {
                Id = Id,
                Script = Script,
                Abi = Abi.Clone(),
                Manifest = Manifest.Clone(),
            };
        }

        void ISerializable.Deserialize(BinaryReader reader)
        {
            Id = reader.ReadInt32();
            Script = reader.ReadVarBytes();
            Abi = reader.ReadSerializable<ContractAbi>();
            Manifest = reader.ReadSerializable<ContractManifest>();
        }

        void ICloneable<ContractState>.FromReplica(ContractState replica)
        {
            Id = replica.Id;
            Script = replica.Script;
            Abi = replica.Abi.Clone();
            Manifest = replica.Manifest.Clone();
        }

        void IInteroperable.FromStackItem(StackItem stackItem)
        {
            throw new NotSupportedException();
        }

        void ISerializable.Serialize(BinaryWriter writer)
        {
            writer.Write(Id);
            writer.WriteVarBytes(Script);
            writer.Write(Abi);
            writer.Write(Manifest);
        }

        public JObject ToJson()
        {
            JObject json = new JObject();
            json["id"] = Id;
            json["hash"] = ScriptHash.ToString();
            json["script"] = Convert.ToBase64String(Script);
            json["abi"] = Abi.ToJson();
            json["manifest"] = Manifest.ToJson();
            return json;
        }

        public StackItem ToStackItem(ReferenceCounter referenceCounter)
        {
            return new Array(referenceCounter, new StackItem[] { Script, Abi.ToString(), Manifest.ToString(), HasStorage, Payable });
        }
    }
}
