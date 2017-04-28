using AntShares.IO;
using System.IO;

namespace AntShares.Core
{
    public class ContractState : StateBase, ICloneable<ContractState>
    {
        public byte[] Script;
        public bool HasStorage;

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

        public override int Size => base.Size + Script.GetVarSize() + sizeof(bool);

        ContractState ICloneable<ContractState>.Clone()
        {
            return new ContractState
            {
                Script = Script,
                HasStorage = HasStorage,
                _scriptHash = _scriptHash
            };
        }

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);
            Script = reader.ReadVarBytes();
            HasStorage = reader.ReadBoolean();
        }

        void ICloneable<ContractState>.FromReplica(ContractState replica)
        {
            Script = replica.Script;
            HasStorage = replica.HasStorage;
            _scriptHash = replica._scriptHash;
        }

        public override void Serialize(BinaryWriter writer)
        {
            base.Serialize(writer);
            writer.WriteVarBytes(Script);
            writer.Write(HasStorage);
        }
    }
}
