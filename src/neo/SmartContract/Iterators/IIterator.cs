using Neo.SmartContract.Enumerators;
using Neo.VM.Types;

namespace Neo.SmartContract.Iterators
{
    public interface IIterator : IEnumerator
    {
        PrimitiveType Key();
    }
}
