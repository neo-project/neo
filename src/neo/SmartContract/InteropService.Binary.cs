using Neo.VM;
using Neo.VM.Types;
using System;
using System.IO;

namespace Neo.SmartContract
{
    partial class InteropService
    {
        public static class Binary
        {
            public static readonly InteropDescriptor Serialize = Register("System.Binary.Serialize", Binary_Serialize, 0_00100000, TriggerType.All);
            public static readonly InteropDescriptor Deserialize = Register("System.Binary.Deserialize", Binary_Deserialize, 0_00500000, TriggerType.All);

            private static bool Binary_Serialize(ApplicationEngine engine)
            {
                byte[] serialized;
                try
                {
                    serialized = BinarySerializer.Serialize(engine.CurrentContext.EvaluationStack.Pop(), engine.MaxItemSize);
                }
                catch
                {
                    return false;
                }
                engine.CurrentContext.EvaluationStack.Push(serialized);
                return true;
            }

            private static bool Binary_Deserialize(ApplicationEngine engine)
            {
                StackItem item;
                try
                {
                    item = BinarySerializer.Deserialize(engine.CurrentContext.EvaluationStack.Pop().GetSpan(), engine.MaxItemSize, engine.ReferenceCounter);
                }
                catch (FormatException)
                {
                    return false;
                }
                catch (IOException)
                {
                    return false;
                }
                engine.CurrentContext.EvaluationStack.Push(item);
                return true;
            }
        }
    }
}
