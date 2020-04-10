using Neo.IO.Json;
using Neo.VM.Types;
using System;

namespace Neo.SmartContract
{
    partial class InteropService
    {
        public static class Json
        {
            public static readonly InteropDescriptor Serialize = Register("System.Json.Serialize", Json_Serialize, 0_00100000, TriggerType.All, CallFlags.None);
            public static readonly InteropDescriptor Deserialize = Register("System.Json.Deserialize", Json_Deserialize, 0_00500000, TriggerType.All, CallFlags.None);

            private static bool Json_Serialize(ApplicationEngine engine)
            {
                if (!engine.TryPop(out StackItem item)) return false;
                byte[] json = JsonSerializer.SerializeToByteArray(item, engine.MaxItemSize);
                engine.Push(json);
                return true;
            }

            private static bool Json_Deserialize(ApplicationEngine engine)
            {
                if (!engine.TryPop(out ReadOnlySpan<byte> json)) return false;
                StackItem item = JsonSerializer.Deserialize(JObject.Parse(json, 10), engine.ReferenceCounter);
                engine.Push(item);
                return true;
            }
        }
    }
}
