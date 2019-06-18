using Newtonsoft.Json;

namespace Neo.SDK.RPC.Model
{
    public class Plugin
    {
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "version")]
        public string Version { get; set; }

        [JsonProperty(PropertyName = "interfaces")]
        public string[] Interfaces { get; set; }

    }

}
