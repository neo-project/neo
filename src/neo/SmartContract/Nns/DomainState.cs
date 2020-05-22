using Neo.IO;
using Neo.Persistence;
using Neo.SmartContract.Native.Tokens;
using Neo.VM;
using Neo.VM.Types;

namespace Neo.SmartContract.Nns
{
    public class DomainState : Nep11TokenState
    {
        public uint TimeToLive { set; get; }

        public RecordType Type { set; get; }

        public byte[] Text { get; set; }

        public override void FromStackItem(StackItem stackItem)
        {
            base.FromStackItem(stackItem);
            Struct @struct = (Struct)stackItem;
            TimeToLive = (uint)@struct[1].GetBigInteger();
            Type = (RecordType)@struct[2].GetSpan().ToArray()[0];
            Text = @struct[3].GetSpan().ToArray();
        }

        public override StackItem ToStackItem(ReferenceCounter referenceCounter)
        {
            Struct @struct = (Struct)base.ToStackItem(referenceCounter);
            @struct.Add(TimeToLive);
            @struct.Add(new byte[] { (byte)Type });
            @struct.Add(Text);
            return @struct;
        }

        public bool IsExpired(StoreView snapshot)
        {
            return snapshot.Height.CompareTo(TimeToLive) > 0;
        }
    }
}
