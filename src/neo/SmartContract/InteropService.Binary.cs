using Neo.VM.Types;
using System;

namespace Neo.SmartContract
{
    partial class InteropService
    {
        public static class Binary
        {
            public static readonly InteropDescriptor Serialize = Register("System.Binary.Serialize", Binary_Serialize, 0_00100000, TriggerType.All, CallFlags.None);
            public static readonly InteropDescriptor Deserialize = Register("System.Binary.Deserialize", Binary_Deserialize, 0_00500000, TriggerType.All, CallFlags.None);

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
                if (!engine.TryPop(out ReadOnlySpan<byte> data)) return false;
                StackItem item = BinarySerializer.Deserialize(data, engine.MaxStackSize, engine.MaxItemSize, engine.ReferenceCounter);
                engine.CurrentContext.EvaluationStack.Push(item);
                return true;
            }
        }
    }
}
