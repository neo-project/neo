using Neo.VM;
using Neo.VM.Types;

namespace Neo.SmartContract.Native.Tokens
{
    public class Nep11TokenState : IInteroperable
    {
        public string Name { set; get; }
        public virtual void FromStackItem(StackItem stackItem)
        {
            Struct @struct = (Struct)stackItem;
            Name = System.Text.Encoding.UTF8.GetString(@struct[0].GetSpan().ToArray());
        }
        public virtual StackItem ToStackItem(ReferenceCounter referenceCounter)
        {
            Struct @struct = new Struct(referenceCounter);
            @struct.Add(System.Text.Encoding.UTF8.GetBytes(Name));
            return @struct;
        }
    }
}
