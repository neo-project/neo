using Neo.Test.Converters;
using Neo.VM;
using Newtonsoft.Json;

namespace Neo.Test.Types
{
    public class VMUTExecutionContextState
    {
        [JsonProperty]
        public int InstructionPointer { get; set; }

        [JsonProperty, JsonConverter(typeof(UppercaseEnum))]
        public OpCode NextInstruction { get; set; }

        // Stacks

        [JsonProperty]
        public VMUTStackItem[] EvaluationStack { get; set; }

        // Slots

        [JsonProperty]
        public VMUTStackItem[] StaticFields { get; set; }

        [JsonProperty]
        public VMUTStackItem[] Arguments { get; set; }

        [JsonProperty]
        public VMUTStackItem[] LocalVariables { get; set; }
    }
}
