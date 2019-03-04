using Neo.VM;

namespace Neo.SmartContract.Iterators
{
    internal class ConcatenatedIterator : IIterator
    {
        private readonly IIterator first, second;
        private IIterator current;

        public ConcatenatedIterator(IIterator first, IIterator second)
        {
            this.current = this.first = first;
            this.second = second;
        }

        public void Dispose()
        {
            first.Dispose();
            second.Dispose();
        }

        public StackItem Key()
        {
            return current.Key();
        }

        public bool Next()
        {
            if (current.Next()) return true;
            current = second;
            return current.Next();
        }

        public StackItem Value()
        {
            return current.Value();
        }
    }
}
