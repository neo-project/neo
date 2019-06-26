using Neo.IO.Json;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Neo.SDK.RPC.Model
{
    public class SDK_RawMemPool
    {
        [JsonProperty(PropertyName = "height")]
        public uint Height { get; set; }

        [JsonProperty(PropertyName = "verified")]
        public string[] Verified { get; set; }

        [JsonProperty(PropertyName = "unverified")]
        public string[] UnVerified { get; set; }

        public JObject ToJson()
        {
            JObject json = new JObject();
            json["height"] = Height;
            json["verified"] = new JArray(Verified.Select(p=>(JObject)p));
            json["unverified"] = new JArray(UnVerified.Select(p => (JObject)p));
            return json;
        }

        public static SDK_RawMemPool FromJson(JObject json)
        {
            SDK_RawMemPool rawMemPool = new SDK_RawMemPool();
            rawMemPool.Height = uint.Parse(json["height"].AsString());
            rawMemPool.Verified = ((JArray)json["verified"]).Select(p => p.AsString()).ToArray();
            rawMemPool.UnVerified = ((JArray)json["unverified"]).Select(p => p.AsString()).ToArray();
            return rawMemPool;
        }
    }
}
