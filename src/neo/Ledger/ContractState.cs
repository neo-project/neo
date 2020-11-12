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
        public ushort Version;
        public UInt160 ScriptHash;
        public byte[] Script;
        public ContractManifest Manifest;

        int ISerializable.Size => sizeof(int) + sizeof(ushort) + UInt160.Length + Script.GetVarSize() + Manifest.Size;

        ContractState ICloneable<ContractState>.Clone()
        {
            return new ContractState
            {
                Id = Id,
                Version = Version,
                ScriptHash = ScriptHash,
                Script = Script,
                Manifest = Manifest.Clone()
            };
        }

        void ISerializable.Deserialize(BinaryReader reader)
        {
            Id = reader.ReadInt32();
            Version = reader.ReadUInt16();
            ScriptHash = reader.ReadSerializable<UInt160>();
            Script = reader.ReadVarBytes();
            Manifest = reader.ReadSerializable<ContractManifest>();
        }

        void ICloneable<ContractState>.FromReplica(ContractState replica)
        {
            Id = replica.Id;
            Version = replica.Version;
            ScriptHash = replica.ScriptHash;
            Script = replica.Script;
            Manifest = replica.Manifest.Clone();
        }

        void IInteroperable.FromStackItem(StackItem stackItem)
        {
            throw new NotSupportedException();
        }

        void ISerializable.Serialize(BinaryWriter writer)
        {
            writer.Write(Id);
            writer.Write(Version);
            writer.Write(ScriptHash);
            writer.WriteVarBytes(Script);
            writer.Write(Manifest);
        }

        public JObject ToJson()
        {
            JObject json = new JObject();
            json["id"] = Id;
            json["version"] = Version;
            json["hash"] = ScriptHash.ToString();
            json["script"] = Convert.ToBase64String(Script);
            json["manifest"] = Manifest.ToJson();
            return json;
        }

        public StackItem ToStackItem(ReferenceCounter referenceCounter)
        {
            return new Array(referenceCounter, new StackItem[] { Id, (int)Version, ScriptHash.ToArray(), Script, Manifest.ToString() });
        }
    }
}
