using AntShares.IO;
using System.IO;

namespace AntShares.Core
{
    public class ContractState : StateBase, ICloneable<ContractState>
    {
        public FunctionCode Code;
        public bool HasStorage;
        public string Name;
        public string CodeVersion;
        public string Author;
        public string Email;
        public string Description;

        public UInt160 ScriptHash => Code.ScriptHash;

        public override int Size => base.Size + Code.Size + sizeof(bool) + Name.GetVarSize() + CodeVersion.GetVarSize() + Author.GetVarSize() + Email.GetVarSize() + Description.GetVarSize();

        ContractState ICloneable<ContractState>.Clone()
        {
            return new ContractState
            {
                Code = Code,
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
            Code = reader.ReadSerializable<FunctionCode>();
            HasStorage = reader.ReadBoolean();
            Name = reader.ReadVarString();
            CodeVersion = reader.ReadVarString();
            Author = reader.ReadVarString();
            Email = reader.ReadVarString();
            Description = reader.ReadVarString();
        }

        void ICloneable<ContractState>.FromReplica(ContractState replica)
        {
            Code = replica.Code;
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
            writer.Write(Code);
            writer.Write(HasStorage);
            writer.WriteVarString(Name);
            writer.WriteVarString(CodeVersion);
            writer.WriteVarString(Author);
            writer.WriteVarString(Email);
            writer.WriteVarString(Description);
        }
    }
}
