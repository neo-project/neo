using Neo.IO.Json;
using Newtonsoft.Json;
using System.Linq;

namespace Neo.SDK.RPC.Model
{
    public class SDK_Plugin
    {
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "version")]
        public string Version { get; set; }

        [JsonProperty(PropertyName = "interfaces")]
        public string[] Interfaces { get; set; }

        public static SDK_Plugin FromJson(JObject json)
        {
            SDK_Plugin plugin = new SDK_Plugin();
            plugin.Name = json["name"].AsString();
            plugin.Version = json["version"].AsString();
            plugin.Interfaces = ((JArray)json["interfaces"]).Select(p => p.AsString()).ToArray();
            return plugin;
        }
    }

}
