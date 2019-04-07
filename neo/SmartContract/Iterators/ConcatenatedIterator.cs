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

        public StackItem Key() => current.Key();
        public StackItem Value() => current.Value();

        public bool Next()
        {
            if (current.Next()) return true;

            current = second;
            return current.Next();
        }


        public void Dispose()
        {
            first.Dispose();
            second.Dispose();
        }
    }
}
