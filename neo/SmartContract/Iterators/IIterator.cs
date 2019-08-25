using Neo.SmartContract.Enumerators;
using Neo.VM;

namespace Neo.SmartContract.Iterators
{
    internal interface IIterator : IEnumerator
    {
        StackItem Key();
    }
}
