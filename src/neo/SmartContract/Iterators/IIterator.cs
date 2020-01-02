using Neo.SmartContract.Enumerators;
using Neo.VM.Types;

namespace Neo.SmartContract.Iterators
{
    internal interface IIterator : IEnumerator
    {
        PrimitiveType Key();
    }
}
