using Neo.IO.Json;
using Neo.VM.Types;
using System;

namespace Neo.SmartContract
{
    partial class ApplicationEngine
    {
        [InteropService("System.Json.Serialize", 0_00100000, TriggerType.All, CallFlags.None)]
        private bool Json_Serialize()
        {
            if (!TryPop(out StackItem item)) return false;
            Push(JsonSerializer.SerializeToByteArray(item, MaxItemSize));
            return true;
        }

        [InteropService("System.Json.Deserialize", 0_00500000, TriggerType.All, CallFlags.None)]
        private bool Json_Deserialize()
        {
            if (!TryPop(out ReadOnlySpan<byte> json)) return false;
            Push(JsonSerializer.Deserialize(JObject.Parse(json, 10), ReferenceCounter));
            return true;
        }
    }
}
