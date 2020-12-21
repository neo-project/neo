using Neo.IO.Json;
using Neo.VM.Types;

namespace Neo.SmartContract
{
    partial class ApplicationEngine
    {
        public static readonly InteropDescriptor System_Json_Serialize = Register("System.Json.Serialize", nameof(JsonSerialize), 1 << 12, CallFlags.None, true);
        public static readonly InteropDescriptor System_Json_Deserialize = Register("System.Json.Deserialize", nameof(JsonDeserialize), 1 << 14, CallFlags.None, true);

        protected internal byte[] JsonSerialize(StackItem item)
        {
            return JsonSerializer.SerializeToByteArray(item, Limits.MaxItemSize);
        }

        protected internal StackItem JsonDeserialize(byte[] json)
        {
            return JsonSerializer.Deserialize(JObject.Parse(json, 10), ReferenceCounter);
        }
    }
}
