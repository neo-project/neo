using Neo.SmartContract.Enumerators;
using Neo.SmartContract.Iterators;
using Neo.VM.Types;
using Array = Neo.VM.Types.Array;

namespace Neo.SmartContract
{
    partial class ApplicationEngine
    {
        [InteropService("System.Iterator.Create", 0_00000400, TriggerType.All, CallFlags.None)]
        private bool Iterator_Create()
        {
            IIterator iterator;
            switch (Pop())
            {
                case Array array:
                    iterator = new ArrayWrapper(array);
                    break;
                case Map map:
                    iterator = new MapWrapper(map);
                    break;
                case PrimitiveType primitive:
                    iterator = new ByteArrayWrapper(primitive);
                    break;
                default:
                    return false;
            }
            Push(StackItem.FromInterface(iterator));
            return true;
        }

        [InteropService("System.Iterator.Key", 0_00000400, TriggerType.All, CallFlags.None)]
        private bool Iterator_Key()
        {
            if (!TryPopInterface(out IIterator iterator)) return false;
            Push(iterator.Key());
            return true;
        }

        [InteropService("System.Iterator.Keys", 0_00000400, TriggerType.All, CallFlags.None)]
        private bool Iterator_Keys()
        {
            if (!TryPopInterface(out IIterator iterator)) return false;
            Push(StackItem.FromInterface(new IteratorKeysWrapper(iterator)));
            return true;
        }

        [InteropService("System.Iterator.Values", 0_00000400, TriggerType.All, CallFlags.None)]
        private bool Iterator_Values()
        {
            if (!TryPopInterface(out IIterator iterator)) return false;
            Push(StackItem.FromInterface(new IteratorValuesWrapper(iterator)));
            return true;
        }

        [InteropService("System.Iterator.Concat", 0_00000400, TriggerType.All, CallFlags.None)]
        private bool Iterator_Concat()
        {
            if (!TryPopInterface(out IIterator first)) return false;
            if (!TryPopInterface(out IIterator second)) return false;
            IIterator result = new ConcatenatedIterator(first, second);
            Push(StackItem.FromInterface(result));
            return true;
        }
    }
}
