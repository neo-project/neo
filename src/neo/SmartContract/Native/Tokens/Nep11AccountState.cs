using Neo.IO;
using Neo.VM;
using Neo.VM.Types;
using System.IO;
using System.Numerics;

namespace Neo.SmartContract.Native.Tokens
{
    public class Nep11AccountState : IInteroperable, ISerializable
    {
        public BigInteger Balance;

        public virtual void FromStackItem(StackItem stackItem)
        {
            Balance = ((Struct)stackItem)[0].GetBigInteger();
        }

        public virtual StackItem ToStackItem(ReferenceCounter referenceCounter)
        {
            return new Struct(referenceCounter) { Balance };
        }

        public virtual int Size => Balance.GetVarSize();

        public virtual void Deserialize(BinaryReader reader)
        {
            Balance=new BigInteger(reader.ReadVarBytes());
        }

        public virtual void Serialize(BinaryWriter writer)
        {
            writer.WriteVarBytes(Balance.ToByteArray());
        }
    }
}
