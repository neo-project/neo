using Neo.IO.Json;
using System.Linq;

namespace Neo.SDK.RPC.Model
{
    public class SDK_Plugin
    {
        public string Name { get; set; }
        
        public string Version { get; set; }
        
        public string[] Interfaces { get; set; }

        public JObject ToJson()
        {
            JObject json = new JObject();
            json["name"] = Name;
            json["version"] = Version;
            json["interfaces"] = new JArray(Interfaces.Select(p => (JObject)p));
            return json;
        }

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
