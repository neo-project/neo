using Neo.VM;
using Neo.VM.Types;
using System.Numerics;

namespace Neo.SmartContract.Native.Tokens
{
    public class Nep5AccountState
    {
        public BigInteger Balance;

        public Nep5AccountState()
        {
        }

        public Nep5AccountState(byte[] data)
        {
            FromByteArray(data);
        }

        public void FromByteArray(byte[] data)
        {
            FromStruct((Struct)BinarySerializer.Deserialize(data, 16, 34));
        }

        protected virtual void FromStruct(Struct @struct)
        {
            Balance = @struct[0].GetBigInteger();
        }

        public byte[] ToByteArray()
        {
            return BinarySerializer.Serialize(ToStruct(), 4096);
        }

        protected virtual Struct ToStruct()
        {
            return new Struct(new StackItem[] { Balance });
        }
    }
}
