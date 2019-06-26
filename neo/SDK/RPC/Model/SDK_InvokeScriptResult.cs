using Neo.IO.Json;
using Newtonsoft.Json;
using System.Linq;

namespace Neo.SDK.RPC.Model
{
    public class SDK_InvokeScriptResult
    {
        [JsonProperty(PropertyName = "script")]
        public string Script { get; set; }

        [JsonProperty(PropertyName = "state")]
        public string State { get; set; }

        [JsonProperty(PropertyName = "gas_consumed")]
        public string GasConsumed { get; set; }

        [JsonProperty(PropertyName = "stack")]
        public SDK_StackJson[] Stack { get; set; }

        [JsonProperty(PropertyName = "tx")]
        public string Tx { get; set; }

        public JObject ToJson()
        {
            JObject json = new JObject();
            json["script"] = Script;
            json["state"] = State;
            json["gas_consumed"] = GasConsumed;
            json["stack"] = new JArray(Stack.Select(p => p.ToJson()));
            json["tx"] = Tx;
            return json;
        }

        public static SDK_InvokeScriptResult FromJson(JObject json)
        {
            SDK_InvokeScriptResult invokeScriptResult = new SDK_InvokeScriptResult();
            invokeScriptResult.Script = json["script"].AsString();
            invokeScriptResult.State = json["state"].AsString();
            invokeScriptResult.GasConsumed = json["gas_consumed"].AsString();
            invokeScriptResult.Tx = json["tx"].AsString();
            invokeScriptResult.Stack = ((JArray)json["stack"]).Select(p => SDK_StackJson.FromJson(p)).ToArray();
            return invokeScriptResult;
        }
    }

    public class SDK_StackJson
    {
        public string Type { get; set; }
        
        public string Value { get; set; }

        public JObject ToJson()
        {
            JObject json = new JObject();
            json["type"] = Type;
            json["value"] = Value;
            return json;
        }

        public static SDK_StackJson FromJson(JObject json)
        {
            SDK_StackJson stackJson = new SDK_StackJson();
            stackJson.Type = json["type"].AsString();
            stackJson.Value = json["value"].AsString();
            return stackJson;
        }
    }

}
