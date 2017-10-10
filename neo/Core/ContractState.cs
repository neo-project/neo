using Neo.IO;
using Neo.IO.Json;
using Neo.SmartContract;
using System.IO;
using System.Linq;

namespace Neo.Core
{
    public class ContractState : StateBase, ICloneable<ContractState>
    {
        public byte[] Script;
        public ContractParameterType[] ParameterList;
        public ContractParameterType ReturnType;
        public bool HasStorage;
        public string Name;
        public string CodeVersion;
        public string Author;
        public string Email;
        public string Description;

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

        public override int Size => base.Size + Script.GetVarSize() + ParameterList.GetVarSize() + sizeof(ContractParameterType) + sizeof(bool) + Name.GetVarSize() + CodeVersion.GetVarSize() + Author.GetVarSize() + Email.GetVarSize() + Description.GetVarSize();

        ContractState ICloneable<ContractState>.Clone()
        {
            return new ContractState
            {
                Script = Script,
                ParameterList = ParameterList,
                ReturnType = ReturnType,
                HasStorage = HasStorage,
                Name = Name,
                CodeVersion = CodeVersion,
                Author = Author,
                Email = Email,
                Description = Description
            };
        }

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);
            Script = reader.ReadVarBytes();
            ParameterList = reader.ReadVarBytes().Select(p => (ContractParameterType)p).ToArray();
            ReturnType = (ContractParameterType)reader.ReadByte();
            HasStorage = reader.ReadBoolean();
            Name = reader.ReadVarString();
            CodeVersion = reader.ReadVarString();
            Author = reader.ReadVarString();
            Email = reader.ReadVarString();
            Description = reader.ReadVarString();
        }

        void ICloneable<ContractState>.FromReplica(ContractState replica)
        {
            Script = replica.Script;
            ParameterList = replica.ParameterList;
            ReturnType = replica.ReturnType;
            HasStorage = replica.HasStorage;
            Name = replica.Name;
            CodeVersion = replica.CodeVersion;
            Author = replica.Author;
            Email = replica.Email;
            Description = replica.Description;
        }

        public override void Serialize(BinaryWriter writer)
        {
            base.Serialize(writer);
            writer.WriteVarBytes(Script);
            writer.WriteVarBytes(ParameterList.Cast<byte>().ToArray());
            writer.Write((byte)ReturnType);
            writer.Write(HasStorage);
            writer.WriteVarString(Name);
            writer.WriteVarString(CodeVersion);
            writer.WriteVarString(Author);
            writer.WriteVarString(Email);
            writer.WriteVarString(Description);
        }

        public override JObject ToJson()
        {
            JObject json = base.ToJson();
            json["hash"] = ScriptHash.ToString();
            json["script"] = Script.ToHexString();
            json["parameters"] = new JArray(ParameterList.Select(p => (JObject)p));
            json["returntype"] = ReturnType;
            json["storage"] = HasStorage;
            json["name"] = Name;
            json["code_version"] = CodeVersion;
            json["author"] = Author;
            json["email"] = Email;
            json["description"] = Description;
            return json;
        }
    }
}
