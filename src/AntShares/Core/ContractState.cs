using AntShares.IO;
using System;
using System.IO;

namespace AntShares.Core
{
    public class ContractState : ISerializable
    {
        public const byte StateVersion = 0;
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

        int ISerializable.Size => sizeof(byte) + Script.GetVarSize() + sizeof(bool);

        void ISerializable.Deserialize(BinaryReader reader)
        {
            if (reader.ReadByte() != StateVersion) throw new FormatException();
            Script = reader.ReadVarBytes();
            HasStorage = reader.ReadBoolean();
        }

        void ISerializable.Serialize(BinaryWriter writer)
        {
            writer.Write(StateVersion);
            writer.WriteVarBytes(Script);
            writer.Write(HasStorage);
        }
    }
}
