using Neo.IO;
using Neo.IO.Json;
using Neo.SmartContract;
using Neo.SmartContract.Manifest;
using Neo.VM;
using Neo.VM.Types;
using System;
using System.IO;
using System.Linq;
using Array = Neo.VM.Types.Array;

namespace Neo.Ledger
{
    public class ContractState : ICloneable<ContractState>, ISerializable, IInteroperable
    {
        public int Id;
        public ushort UpdateCounter;
        public UInt160 Hash;
        public byte[] Script;
        public ContractManifest Manifest;

        int ISerializable.Size => sizeof(int) + sizeof(ushort) + UInt160.Length + Script.GetVarSize() + Manifest.Size;

        ContractState ICloneable<ContractState>.Clone()
        {
            return new ContractState
            {
                Id = Id,
                UpdateCounter = UpdateCounter,
                Hash = Hash,
                Script = Script,
                Manifest = Manifest.Clone()
            };
        }

        void ISerializable.Deserialize(BinaryReader reader)
        {
            Id = reader.ReadInt32();
            UpdateCounter = reader.ReadUInt16();
            Hash = reader.ReadSerializable<UInt160>();
            Script = reader.ReadVarBytes();
            Manifest = reader.ReadSerializable<ContractManifest>();
        }

        void ICloneable<ContractState>.FromReplica(ContractState replica)
        {
            Id = replica.Id;
            UpdateCounter = replica.UpdateCounter;
            Hash = replica.Hash;
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
            writer.Write(UpdateCounter);
            writer.Write(Hash);
            writer.WriteVarBytes(Script);
            writer.Write(Manifest);
        }

        /// <summary>
        /// Return true if is allowed
        /// </summary>
        /// <param name="targetContract">The contract that we are calling</param>
        /// <param name="targetMethod">The method that we are calling</param>
        /// <returns>Return true or false</returns>
        public bool CanCall(ContractState targetContract, string targetMethod)
        {
            if (Manifest.SafeMethods.Contains(targetMethod)) return true;
            return Manifest.Permissions.Any(u => u.IsAllowed(targetContract, targetMethod));
        }

        public JObject ToJson()
        {
            JObject json = new JObject();
            json["id"] = Id;
            json["updatecounter"] = UpdateCounter;
            json["hash"] = Hash.ToString();
            json["script"] = Convert.ToBase64String(Script);
            json["manifest"] = Manifest.ToJson();
            return json;
        }

        public StackItem ToStackItem(ReferenceCounter referenceCounter)
        {
            return new Array(referenceCounter, new StackItem[] { Id, (int)UpdateCounter, Hash.ToArray(), Script, Manifest.ToString() });
        }
    }
}
