using AntShares.IO;
using AntShares.IO.Json;
using System.IO;
using System.Linq;

namespace AntShares.Core
{
    public class PublishTransaction : Transaction
    {
        public FunctionCode Code;
        public bool NeedStorage;
        public string Name;
        public string CodeVersion;
        public string Author;
        public string Email;
        public string Description;

        public override int Size => base.Size + Code.Size + Name.GetVarSize() + CodeVersion.GetVarSize() + Author.GetVarSize() + Email.GetVarSize() + Description.GetVarSize();

        public PublishTransaction()
            : base(TransactionType.PublishTransaction)
        {
        }

        protected override void DeserializeExclusiveData(BinaryReader reader)
        {
            Code = reader.ReadSerializable<FunctionCode>();
            if (Version == 0)
                NeedStorage = false;
            else
                NeedStorage = reader.ReadBoolean();
            Name = reader.ReadVarString(252);
            CodeVersion = reader.ReadVarString(252);
            Author = reader.ReadVarString(252);
            Email = reader.ReadVarString(252);
            Description = reader.ReadVarString(65536);
        }

        protected override void SerializeExclusiveData(BinaryWriter writer)
        {
            writer.Write(Code);
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
            json["contract"]["hash"] = Code.ScriptHash.ToString();
            json["contract"]["script"] = Code.Script.ToHexString();
            json["contract"]["parameters"] = new JArray(Code.ParameterList.Select(p => (JObject)p));
            json["contract"]["returntype"] = Code.ReturnType;
            json["contract"]["needstorage"] = NeedStorage;
            json["contract"]["name"] = Name;
            json["contract"]["version"] = CodeVersion;
            json["contract"]["author"] = Author;
            json["contract"]["email"] = Email;
            json["contract"]["description"] = Description;
            return json;
        }
    }
}
