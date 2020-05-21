using Neo.IO;
using Neo.Persistence;
using Neo.SmartContract.Native.Tokens;
using Neo.VM;
using Neo.VM.Types;

namespace Neo.SmartContract.Nns
{
    public class DomainState : Nep11TokenState
    {
        public UInt160 Operator { set; get; }
        public uint TimeToLive { set; get; }

        public override void FromStackItem(StackItem stackItem)
        {
            base.FromStackItem(stackItem);
            Struct @struct = (Struct)stackItem;
            Operator = @struct[1].GetSpan().AsSerializable<UInt160>();
            TimeToLive = (uint)@struct[2].GetBigInteger();
        }

        public override StackItem ToStackItem(ReferenceCounter referenceCounter)
        {
            Struct @struct = (Struct)base.ToStackItem(referenceCounter);
            @struct.Add(Operator.ToArray());
            @struct.Add(TimeToLive);
            return @struct;
        }

        public bool IsExpired(StoreView snapshot)
        {
            return snapshot.Height.CompareTo(TimeToLive) > 0;
        }
    }
}
