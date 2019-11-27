using Neo.VM.Types;

namespace Neo.SmartContract
{
    public interface IInteroperable
    {
        StackItem ToStackItem();
    }
}
