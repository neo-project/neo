using AntShares.IO;
using AntShares.IO.Json;
using System.IO;
using System.Linq;

namespace AntShares.Core
{
    public class PublishTransaction : Transaction
    {
        public byte[] Script;
        public ContractParameterType[] ParameterList;
        public ContractParameterType ReturnType;
        public string Name;
        public byte CodeVersion;
        public string Author;
        public string Email;
        public string Description;

        public override int Size => base.Size + Script.GetVarSize() + ParameterList.GetVarSize() + sizeof(ContractParameterType) + Name.GetVarSize() + sizeof(byte) + Author.GetVarSize() + Email.GetVarSize() + Description.GetVarSize();

        public override Fixed8 SystemFee => Fixed8.FromDecimal(500);

        public PublishTransaction()
            : base(TransactionType.PublishTransaction)
        {
        }

        protected override void DeserializeExclusiveData(BinaryReader reader)
        {
            Script = reader.ReadVarBytes(65536);
            ParameterList = reader.ReadVarBytes(252).Select(p => (ContractParameterType)p).ToArray();
            ReturnType = (ContractParameterType)reader.ReadByte();
            Name = reader.ReadVarString(252);
            CodeVersion = reader.ReadByte();
            Author = reader.ReadVarString(252);
            Email = reader.ReadVarString(252);
            Description = reader.ReadVarString(65536);
        }

        protected override void SerializeExclusiveData(BinaryWriter writer)
        {
            writer.WriteVarBytes(Script);
            writer.WriteVarBytes(ParameterList.Cast<byte>().ToArray());
            writer.Write((byte)ReturnType);
            writer.WriteVarString(Name);
            writer.Write(CodeVersion);
            writer.WriteVarString(Author);
            writer.WriteVarString(Email);
            writer.WriteVarString(Description);
        }

        public override JObject ToJson()
        {
            JObject json = base.ToJson();
            json["contract"] = new JObject();
            json["contract"]["script"] = Script.ToHexString();
            json["contract"]["parameters"] = new JArray(ParameterList.Select(p => (JObject)p));
            json["contract"]["returntype"] = ReturnType;
            json["contract"]["name"] = Name;
            json["contract"]["version"] = CodeVersion;
            json["contract"]["author"] = Author;
            json["contract"]["email"] = Email;
            json["contract"]["description"] = Description;
            return json;
        }
    }
}
