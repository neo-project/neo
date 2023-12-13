using Neo.Test.Converters;
using Newtonsoft.Json;

namespace Neo.Test.Types
{
    public class VMUTEntry
    {
        [JsonProperty(Order = 1)]
        public string Name { get; set; }

        [JsonProperty(Order = 2), JsonConverter(typeof(ScriptConverter))]
        public byte[] Script { get; set; }

        [JsonProperty(Order = 3)]
        public VMUTStep[] Steps { get; set; }
    }
}
