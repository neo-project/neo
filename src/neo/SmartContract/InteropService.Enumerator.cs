using Neo.SmartContract.Enumerators;
using Neo.SmartContract.Iterators;
using Neo.VM.Types;
using Array = Neo.VM.Types.Array;

namespace Neo.SmartContract
{
    partial class InteropService
    {
        public static class Enumerator
        {
            public static readonly InteropDescriptor Create = Register("System.Enumerator.Create", Enumerator_Create, 0_00000400, TriggerType.All, CallFlags.None);
            public static readonly InteropDescriptor Next = Register("System.Enumerator.Next", Enumerator_Next, 0_01000000, TriggerType.All, CallFlags.None);
            public static readonly InteropDescriptor Value = Register("System.Enumerator.Value", Enumerator_Value, 0_00000400, TriggerType.All, CallFlags.None);
            public static readonly InteropDescriptor Concat = Register("System.Enumerator.Concat", Enumerator_Concat, 0_00000400, TriggerType.All, CallFlags.None);

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
        }
    }
}
