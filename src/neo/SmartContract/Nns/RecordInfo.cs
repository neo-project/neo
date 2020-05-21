using Neo.VM;
using Neo.VM.Types;

namespace Neo.SmartContract.Nns
{
    public class RecordInfo : IInteroperable
    {
        public RecordType Type { set; get; }
        public byte[] Text { get; set; }

        public void FromStackItem(StackItem stackItem)
        {
            Type = (RecordType)((Struct)stackItem)[0].GetSpan()[0];
            Text = ((Struct)stackItem)[1].GetSpan().ToArray();
        }

        public StackItem ToStackItem(ReferenceCounter referenceCounter)
        {
            return new Struct(referenceCounter) { new byte[] { (byte)Type }, Text };
        }
    }
}
