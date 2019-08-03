using Neo.Cryptography.ECC;
using Neo.IO;
using Neo.IO.Json;
using System;
using System.IO;

namespace Neo.Network.P2P.Payloads
{
    public class CosignerUsage : ISerializable
    {
        public WitnessScope Scope;
        public byte[] ScopeData;
        public UInt160 ScriptHash;

        public int Size =>
            sizeof(WitnessScope) +                      // Type
            (HasData ? ScopeData.GetVarSize() : 0) +    // ScopeData
            ScriptHash.Size;                            // ScriptHash

        public bool HasData => (Scope == WitnessScope.CustomScriptHash) || (Scope == WitnessScope.ExecutingGroupPubKey);

        void ISerializable.Deserialize(BinaryReader reader)
        {
            Scope = (WitnessScope)reader.ReadByte();
            ScopeData = (HasData ? reader.ReadVarBytes(65536) : new byte[0]);
            ScriptHash = reader.ReadSerializable<UInt160>();
        }

        void ISerializable.Serialize(BinaryWriter writer)
        {
            writer.Write((byte)Scope);
            if (HasData) writer.WriteVarBytes(ScopeData);
            writer.Write(ScriptHash);
        }

        public ECPoint GetPubKey()
        {
            using (MemoryStream ms = new MemoryStream(ScopeData, false))
            using (BinaryReader reader = new BinaryReader(ms))
            {
                return ECPoint.DeserializeFrom(reader, ECCurve.Secp256r1);
            }
        }

        public JObject ToJson()
        {
            JObject json = new JObject();
            json["scope"] = (new byte[] { (byte)Scope }).ToHexString();
            json["scopeData"] = ScopeData.ToHexString();
            json["scriptHash"] = ScriptHash.ToString();
            return json;
        }

        public static CosignerUsage FromJson(JObject json)
        {
            CosignerUsage usage = new CosignerUsage();
            usage.Scope = (WitnessScope)json["scope"].AsString().HexToBytes()[0];
            usage.ScopeData = json["scopeData"].AsString().HexToBytes();
            usage.ScriptHash = UInt160.Parse(json["scriptHash"].AsString());
            return usage;
        }
    }
}
