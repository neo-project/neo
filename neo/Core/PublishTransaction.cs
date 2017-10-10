using Neo.IO;
using Neo.IO.Json;
using Neo.SmartContract;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Neo.Core
{
    [Obsolete]
    public class PublishTransaction : Transaction
    {
        public byte[] Script;
        public ContractParameterType[] ParameterList;
        public ContractParameterType ReturnType;
        public bool NeedStorage;
        public string Name;
        public string CodeVersion;
        public string Author;
        public string Email;
        public string Description;

        private UInt160 _scriptHash;
        internal UInt160 ScriptHash
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

        public override int Size => base.Size + Script.GetVarSize() + ParameterList.GetVarSize() + sizeof(ContractParameterType) + Name.GetVarSize() + CodeVersion.GetVarSize() + Author.GetVarSize() + Email.GetVarSize() + Description.GetVarSize();

        public PublishTransaction()
            : base(TransactionType.PublishTransaction)
        {
        }

        protected override void DeserializeExclusiveData(BinaryReader reader)
        {
            if (Version > 1) throw new FormatException();
            Script = reader.ReadVarBytes();
            ParameterList = reader.ReadVarBytes().Select(p => (ContractParameterType)p).ToArray();
            ReturnType = (ContractParameterType)reader.ReadByte();
            if (Version >= 1)
                NeedStorage = reader.ReadBoolean();
            else
                NeedStorage = false;
            Name = reader.ReadVarString(252);
            CodeVersion = reader.ReadVarString(252);
            Author = reader.ReadVarString(252);
            Email = reader.ReadVarString(252);
            Description = reader.ReadVarString(65536);
        }

        protected override void SerializeExclusiveData(BinaryWriter writer)
        {
            writer.WriteVarBytes(Script);
            writer.WriteVarBytes(ParameterList.Cast<byte>().ToArray());
            writer.Write((byte)ReturnType);
            if (Version >= 1) writer.Write(NeedStorage);
            writer.WriteVarString(Name);
            writer.WriteVarString(CodeVersion);
            writer.WriteVarString(Author);
            writer.WriteVarString(Email);
            writer.WriteVarString(Description);
        }

        public override JObject ToJson()
        {
            JObject json = base.ToJson();
            json["contract"] = new JObject();
            json["contract"]["code"] = new JObject();
            json["contract"]["code"]["hash"] = ScriptHash.ToString();
            json["contract"]["code"]["script"] = Script.ToHexString();
            json["contract"]["code"]["parameters"] = new JArray(ParameterList.Select(p => (JObject)p));
            json["contract"]["code"]["returntype"] = ReturnType;
            json["contract"]["needstorage"] = NeedStorage;
            json["contract"]["name"] = Name;
            json["contract"]["version"] = CodeVersion;
            json["contract"]["author"] = Author;
            json["contract"]["email"] = Email;
            json["contract"]["description"] = Description;
            return json;
        }

        public override bool Verify(IEnumerable<Transaction> mempool)
        {
            return false;
        }
    }
}
