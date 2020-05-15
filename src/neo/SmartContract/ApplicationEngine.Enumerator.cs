using Neo.SmartContract.Enumerators;
using Neo.SmartContract.Iterators;
using Neo.VM.Types;
using Array = Neo.VM.Types.Array;

namespace Neo.SmartContract
{
    partial class ApplicationEngine
    {
        [InteropService("System.Enumerator.Create", 0_00000400, TriggerType.All, CallFlags.None)]
        private bool Enumerator_Create()
        {
            IEnumerator enumerator;
            switch (Pop())
            {
                case Array array:
                    enumerator = new ArrayWrapper(array);
                    break;
                case PrimitiveType primitive:
                    enumerator = new ByteArrayWrapper(primitive);
                    break;
                default:
                    return false;
            }
            Push(StackItem.FromInterface(enumerator));
            return true;
        }

        [InteropService("System.Enumerator.Next", 0_01000000, TriggerType.All, CallFlags.None)]
        private bool Enumerator_Next()
        {
            if (!TryPopInterface(out IEnumerator enumerator)) return false;
            Push(enumerator.Next());
            return true;
        }

        [InteropService("System.Enumerator.Value", 0_00000400, TriggerType.All, CallFlags.None)]
        private bool Enumerator_Value()
        {
            if (!TryPopInterface(out IEnumerator enumerator)) return false;
            Push(enumerator.Value());
            return true;
        }

        [InteropService("System.Enumerator.Concat", 0_00000400, TriggerType.All, CallFlags.None)]
        private bool Enumerator_Concat()
        {
            if (!TryPopInterface(out IEnumerator first)) return false;
            if (!TryPopInterface(out IEnumerator second)) return false;
            IEnumerator result = new ConcatenatedEnumerator(first, second);
            Push(StackItem.FromInterface(result));
            return true;
        }
    }
}
