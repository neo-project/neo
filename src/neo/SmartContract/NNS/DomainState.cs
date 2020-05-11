using Neo.IO;
using Neo.SmartContract.Native.Tokens;
using Neo.VM;
using Neo.VM.Types;

namespace Neo.SmartContract.NNS
{
    public class DomainState : Nep11TokenState
    {
        public UInt160 Operator { set; get; }
        public uint TimeToLive { set; get; }

        public override void FromStackItem(StackItem stackItem)
        {
            Struct @struct = (Struct)stackItem;
            Operator = @struct[0].GetSpan().AsSerializable<UInt160>();
            TimeToLive = (uint)@struct[1].GetBigInteger();
            Name = System.Text.Encoding.UTF8.GetString(@struct[2].GetSpan().ToArray()).ToLower();
        }

        public override StackItem ToStackItem(ReferenceCounter referenceCounter)
        {
            Struct @struct = new Struct(referenceCounter);
            @struct.Add(Operator.ToArray());
            @struct.Add(TimeToLive);
            @struct.Add(System.Text.Encoding.UTF8.GetBytes(Name));
            return @struct;
        }
    }
}
