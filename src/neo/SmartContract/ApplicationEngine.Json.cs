using Neo.IO.Json;
using Neo.VM.Types;

namespace Neo.SmartContract
{
    partial class ApplicationEngine
    {
        public static readonly InteropDescriptor System_Json_Serialize = Register("System.Json.Serialize", nameof(JsonSerialize), 0_00100000, TriggerType.All, CallFlags.None);
        public static readonly InteropDescriptor System_Json_Deserialize = Register("System.Json.Deserialize", nameof(JsonDeserialize), 0_00500000, TriggerType.All, CallFlags.None);

        internal byte[] JsonSerialize(StackItem item)
        {
            return JsonSerializer.SerializeToByteArray(item, MaxItemSize);
        }

        internal StackItem JsonDeserialize(byte[] json)
        {
            return JsonSerializer.Deserialize(JObject.Parse(json, 10), ReferenceCounter);
        }
    }
}
