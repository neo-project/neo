
using Neo.VM.Types;

namespace Neo.SmartContract.Enumerators
{
    public class CollectionWrapper : IEnumerator
    {
        private System.Collections.Generic.IEnumerator<StackItem> enumerator;

        public CollectionWrapper(System.Collections.Generic.IEnumerator<StackItem> enumerator)
        {
            this.enumerator = enumerator;
        }

        public void Dispose()
        {
        }

        public bool Next()
        {
            return enumerator.MoveNext();
        }

        public StackItem Value()
        {
            return enumerator.Current;
        }
    }
}
