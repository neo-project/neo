using Neo.VM;

namespace Neo.SmartContract.Enumerators
{
    internal class ConcatenatedEnumerator : IEnumerator
    {
        private readonly IEnumerator first, second;
        private IEnumerator current;

        public ConcatenatedEnumerator(IEnumerator first, IEnumerator second)
        {
            this.current = this.first = first;
            this.second = second;
        }

        public void Dispose()
        {
            first.Dispose();
            second.Dispose();
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
