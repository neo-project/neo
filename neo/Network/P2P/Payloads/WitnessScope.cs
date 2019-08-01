using Neo.IO;
using Neo.IO.Json;
using System.IO;

namespace Neo.Network.P2P.Payloads
{
    public class WitnessScope : ISerializable, ICloneable<WitnessScope>
    {
        public static readonly WitnessScope Global = new WitnessScope()
        {
            Type = WitnessScopeType.Global,
            ScopeData = new byte[0]
        };

        public WitnessScopeType Type;
        public byte[] ScopeData;

        public bool HasData => (Type == WitnessScopeType.CustomScriptHash) || (Type == WitnessScopeType.ExecutingGroupPubKey);

        public int Size =>
            sizeof(WitnessScopeType) +              // Type
            (HasData ? ScopeData.GetVarSize() : 0); // ScopeData

        void ISerializable.Deserialize(BinaryReader reader)
        {
            Type = (WitnessScopeType)reader.ReadByte();
            ScopeData = (HasData ? reader.ReadVarBytes(65536) : new byte[0]);
        }

        void ISerializable.Serialize(BinaryWriter writer)
        {
            writer.Write((byte)Type);
            if (HasData)
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

        public WitnessScope Clone()
        {
            return new WitnessScope()
            {
                Type = Type,
                ScopeData = ScopeData
            };
        }

        public void FromReplica(WitnessScope replica)
        {
            Type = replica.Type;
            ScopeData = replica.ScopeData;
        }
    }
}
