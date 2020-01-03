using Neo.IO.Json;
using Neo.VM;

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
                var item = engine.CurrentContext.EvaluationStack.Pop();
                try
                {
                    var json = JsonSerializer.SerializeToByteArray(item, engine.MaxItemSize);
                    engine.CurrentContext.EvaluationStack.Push(json);
                    return true;
                }
                catch
                {
                    return false;
                }
            }

            private static bool Json_Deserialize(ApplicationEngine engine)
            {
                var json = engine.CurrentContext.EvaluationStack.Pop().GetSpan();
                try
                {
                    var obj = JObject.Parse(json, 10);
                    var item = JsonSerializer.Deserialize(obj, engine.ReferenceCounter);
                    engine.CurrentContext.EvaluationStack.Push(item);
                    return true;
                }
                catch
                {
                    return false;
                }
            }
        }
    }
}
