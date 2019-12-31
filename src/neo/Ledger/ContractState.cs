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

        int ISerializable.Size => Script.GetVarSize() + Manifest.ToJson().ToString().GetVarSize() + sizeof(bool) + RedirectionHash.ToArray().GetVarSize();

        private UInt160 _redirectionHash = UInt160.Zero;
        public virtual UInt160 RedirectionHash
        {
            get
            {
                return _redirectionHash;
            }

            set
            {
                _redirectionHash = value;
            }
        }

        private bool isDeleted = false;

        public virtual bool IsDeleted
        {
            get
            {
                return isDeleted;
            }

            set
            {
                isDeleted = value;
            }
        }

        ContractState ICloneable<ContractState>.Clone()
        {
            return new ContractState
            {
                Script = Script,
                Manifest = Manifest.Clone(),
                RedirectionHash = RedirectionHash,
                IsDeleted = IsDeleted
            };
        }

        void ISerializable.Deserialize(BinaryReader reader)
        {
            Script = reader.ReadVarBytes();
            Manifest = reader.ReadSerializable<ContractManifest>();
            RedirectionHash = reader.ReadSerializable<UInt160>();
            IsDeleted = reader.ReadBoolean();
        }

        void ICloneable<ContractState>.FromReplica(ContractState replica)
        {
            Script = replica.Script;
            Manifest = replica.Manifest.Clone();
            RedirectionHash = replica.RedirectionHash;
            IsDeleted = replica.IsDeleted;
        }

        void ISerializable.Serialize(BinaryWriter writer)
        {
            writer.WriteVarBytes(Script);
            writer.Write(Manifest);
            writer.Write(RedirectionHash);
            writer.Write(IsDeleted);
        }

        public JObject ToJson()
        {
            JObject json = new JObject();
            json["hash"] = ScriptHash.ToString();
            json["script"] = Convert.ToBase64String(Script);
            json["manifest"] = Manifest.ToJson();
            json["redirectionHash"] = RedirectionHash.ToString();
            json["isDeleted"] = IsDeleted;
            return json;
        }

        public static ContractState FromJson(JObject json)
        {
            ContractState contractState = new ContractState();
            contractState.Script = Convert.FromBase64String(json["script"].AsString());
            contractState.Manifest = ContractManifest.FromJson(json["manifest"]);
            contractState.RedirectionHash = UInt160.Parse(json["RedirectionHash"].AsString());
            contractState.IsDeleted = json["isDeleted"].AsBoolean();
            return contractState;
        }

        public StackItem ToStackItem(ReferenceCounter referenceCounter)
        {
            return new Array(referenceCounter, new StackItem[] { Script, HasStorage, Payable });
        }
    }
}
