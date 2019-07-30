using Neo.IO;
using Neo.IO.Json;
using System.IO;

namespace Neo.Network.P2P.Payloads
{
    public class WitnessScope : ISerializable
    {
        public WitnessScopeType Type;
        public byte[] ScopeData;

        public int Size => 1 + ScopeData.GetVarSize();

        void ISerializable.Deserialize(BinaryReader reader)
        {
            Type = (WitnessScopeType)reader.ReadByte();
            ScopeData = reader.ReadVarBytes(65536);
        }

        void ISerializable.Serialize(BinaryWriter writer)
        {
            writer.Write((byte)Type);
            writer.WriteVarBytes(ScopeData);
        }

        public JObject ToJson()
        {
            JObject json = new JObject();
            json["type"] = (new byte[] { (byte)Type }).ToHexString();
            json["scopeData"] = ScopeData.ToHexString();
            return json;
        }

        public static WitnessScope FromJson(JObject json)
        {
            WitnessScope scope = new WitnessScope();
            scope.Type = (WitnessScopeType)json["type"].AsString().HexToBytes()[0];
            scope.ScopeData = json["scopeData"].AsString().HexToBytes();
            return scope;
        }
    }
}
