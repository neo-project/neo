using Neo.VM;
using Neo.VM.Types;

namespace Neo.SmartContract.Nns
{
    public class RecordInfo : IInteroperable
    {
        public RecordType Type { set; get; }
        public string Text { get; set; }

        public void FromStackItem(StackItem stackItem)
        {
            Type = (RecordType)((Struct)stackItem)[0].GetSpan()[0];
            Text = ((Struct)stackItem)[1].GetString();
        }

        public StackItem ToStackItem(ReferenceCounter referenceCounter)
        {
            Struct @struct = new Struct(referenceCounter);
            @struct.Add(new byte[] { (byte)Type });
            @struct.Add(Text);
            return @struct;
        }
    }
}
