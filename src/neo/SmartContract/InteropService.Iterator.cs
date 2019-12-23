using Neo.SmartContract.Enumerators;
using Neo.SmartContract.Iterators;
using Neo.VM.Types;
using Array = Neo.VM.Types.Array;

namespace Neo.SmartContract
{
    partial class InteropService
    {
        public static class Iterator
        {
            public static readonly InteropDescriptor Create = Register("System.Iterator.Create", Iterator_Create, 0_00000400, TriggerType.All, CallFlags.None);
            public static readonly InteropDescriptor Key = Register("System.Iterator.Key", Iterator_Key, 0_00000400, TriggerType.All, CallFlags.None);
            public static readonly InteropDescriptor Keys = Register("System.Iterator.Keys", Iterator_Keys, 0_00000400, TriggerType.All, CallFlags.None);
            public static readonly InteropDescriptor Values = Register("System.Iterator.Values", Iterator_Values, 0_00000400, TriggerType.All, CallFlags.None);
            public static readonly InteropDescriptor Concat = Register("System.Iterator.Concat", Iterator_Concat, 0_00000400, TriggerType.All, CallFlags.None);

            private static bool Iterator_Create(ApplicationEngine engine)
            {
                IIterator iterator;
                switch (engine.CurrentContext.EvaluationStack.Pop())
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
                engine.CurrentContext.EvaluationStack.Push(StackItem.FromInterface(iterator));
                return true;
            }

            private static bool Iterator_Key(ApplicationEngine engine)
            {
                if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
                {
                    IIterator iterator = _interface.GetInterface<IIterator>();
                    engine.CurrentContext.EvaluationStack.Push(iterator.Key());
                    return true;
                }
                return false;
            }

            private static bool Iterator_Keys(ApplicationEngine engine)
            {
                if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
                {
                    IIterator iterator = _interface.GetInterface<IIterator>();
                    engine.CurrentContext.EvaluationStack.Push(StackItem.FromInterface(new IteratorKeysWrapper(iterator)));
                    return true;
                }
                return false;
            }

            private static bool Iterator_Values(ApplicationEngine engine)
            {
                if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
                {
                    IIterator iterator = _interface.GetInterface<IIterator>();
                    engine.CurrentContext.EvaluationStack.Push(StackItem.FromInterface(new IteratorValuesWrapper(iterator)));
                    return true;
                }
                return false;
            }

            private static bool Iterator_Concat(ApplicationEngine engine)
            {
                if (!(engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface1)) return false;
                if (!(engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface2)) return false;
                IIterator first = _interface1.GetInterface<IIterator>();
                IIterator second = _interface2.GetInterface<IIterator>();
                IIterator result = new ConcatenatedIterator(first, second);
                engine.CurrentContext.EvaluationStack.Push(StackItem.FromInterface(result));
                return true;
            }
        }
    }
}
