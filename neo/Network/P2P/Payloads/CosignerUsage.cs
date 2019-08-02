using Neo.IO;
using Neo.IO.Json;
using System.IO;

namespace Neo.Network.P2P.Payloads
{
    public class CosignerUsage : ISerializable
    {
        public WitnessScope Scope;
        public UInt160 ScriptHash;
        public int Size =>
          Scope.Size +
          ScriptHash.Size;

        void ISerializable.Deserialize(BinaryReader reader)
        {
            Scope = reader.ReadSerializable<WitnessScope>();
            ScriptHash = reader.ReadSerializable<UInt160>();
        }

        void ISerializable.Serialize(BinaryWriter writer)
        {
            writer.Write(Scope);
            writer.Write(ScriptHash);
        }

        public JObject ToJson()
        {
            JObject json = new JObject();
            json["scope"] = Scope.ToJson();
            json["scriptHash"] = ScriptHash.ToString();
            return json;
        }

        public static CosignerUsage FromJson(JObject json)
        {
            CosignerUsage usage = new CosignerUsage();
            usage.Scope = WitnessScope.FromJson(json["scope"]);
            usage.ScriptHash = UInt160.Parse(json["scriptHash"].AsString());
            return usage;
        }
    }
}
