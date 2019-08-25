using Neo.IO;
using Neo.SmartContract;
using Neo.VM;
using System;
using System.IO;
using System.Linq;

namespace Neo.Wallets.SQLite
{
    public class VerificationContract : SmartContract.Contract, IEquatable<VerificationContract>, ISerializable
    {
        public int Size => 20 + ParameterList.GetVarSize() + Script.GetVarSize();

        public void Deserialize(BinaryReader reader)
        {
            reader.ReadSerializable<UInt160>();
            ParameterList = reader.ReadVarBytes().Select(p => (ContractParameterType)p).ToArray();
            Script = reader.ReadVarBytes();
        }

        public bool Equals(VerificationContract other)
        {
            if (ReferenceEquals(this, other)) return true;
            if (other is null) return false;
            return ScriptHash.Equals(other.ScriptHash);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as VerificationContract);
        }

        public override int GetHashCode()
        {
            return ScriptHash.GetHashCode();
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(new UInt160());
            writer.WriteVarBytes(ParameterList.Select(p => (byte)p).ToArray());
            writer.WriteVarBytes(Script);
        }
    }
}
