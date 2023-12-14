using Neo.Test.Converters;
using Neo.VM;
using Newtonsoft.Json;

namespace Neo.Test.Types
{
    public class VMUTExecutionEngineState
    {
        [JsonProperty, JsonConverter(typeof(UppercaseEnum))]
        public VMState State { get; set; }

        [JsonProperty]
        public VMUTStackItem[] ResultStack { get; set; }

        [JsonProperty]
        public VMUTExecutionContextState[] InvocationStack { get; set; }

        [JsonProperty]
        public string ExceptionMessage { get; set; }
    }
}
