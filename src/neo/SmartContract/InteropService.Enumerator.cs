using Neo.SmartContract.Enumerators;
using Neo.SmartContract.Iterators;
using Neo.VM.Types;
using Array = Neo.VM.Types.Array;

namespace Neo.SmartContract
{
    partial class InteropService
    {
        public static readonly uint System_Enumerator_Create = Register("System.Enumerator.Create", Enumerator_Create, 0_00000400, TriggerType.All);
        public static readonly uint System_Enumerator_Next = Register("System.Enumerator.Next", Enumerator_Next, 0_01000000, TriggerType.All);
        public static readonly uint System_Enumerator_Value = Register("System.Enumerator.Value", Enumerator_Value, 0_00000400, TriggerType.All);
        public static readonly uint System_Enumerator_Concat = Register("System.Enumerator.Concat", Enumerator_Concat, 0_00000400, TriggerType.All);
        public static readonly uint System_Iterator_Create = Register("System.Iterator.Create", Iterator_Create, 0_00000400, TriggerType.All);
        public static readonly uint System_Iterator_Key = Register("System.Iterator.Key", Iterator_Key, 0_00000400, TriggerType.All);
        public static readonly uint System_Iterator_Keys = Register("System.Iterator.Keys", Iterator_Keys, 0_00000400, TriggerType.All);
        public static readonly uint System_Iterator_Values = Register("System.Iterator.Values", Iterator_Values, 0_00000400, TriggerType.All);
        public static readonly uint System_Iterator_Concat = Register("System.Iterator.Concat", Iterator_Concat, 0_00000400, TriggerType.All);

        private static bool Enumerator_Create(ApplicationEngine engine)
        {
            IEnumerator enumerator;
            switch (engine.CurrentContext.EvaluationStack.Pop())
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
            engine.CurrentContext.EvaluationStack.Push(StackItem.FromInterface(enumerator));
            return true;
        }

        private static bool Enumerator_Next(ApplicationEngine engine)
        {
            if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
            {
                IEnumerator enumerator = _interface.GetInterface<IEnumerator>();
                engine.CurrentContext.EvaluationStack.Push(enumerator.Next());
                return true;
            }
            return false;
        }

        private static bool Enumerator_Value(ApplicationEngine engine)
        {
            if (engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface)
            {
                IEnumerator enumerator = _interface.GetInterface<IEnumerator>();
                engine.CurrentContext.EvaluationStack.Push(enumerator.Value());
                return true;
            }
            return false;
        }

        private static bool Enumerator_Concat(ApplicationEngine engine)
        {
            if (!(engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface1)) return false;
            if (!(engine.CurrentContext.EvaluationStack.Pop() is InteropInterface _interface2)) return false;
            IEnumerator first = _interface1.GetInterface<IEnumerator>();
            IEnumerator second = _interface2.GetInterface<IEnumerator>();
            IEnumerator result = new ConcatenatedEnumerator(first, second);
            engine.CurrentContext.EvaluationStack.Push(StackItem.FromInterface(result));
            return true;
        }

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
