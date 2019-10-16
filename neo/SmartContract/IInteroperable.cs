using Neo.VM;

namespace Neo.SmartContract
{
    public interface IInteroperable
    {
        StackItem ToStackItem();
    }
}
