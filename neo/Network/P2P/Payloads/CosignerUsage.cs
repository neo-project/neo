using Neo.Cryptography.ECC;
using Neo.IO;
using Neo.IO.Json;
using System.IO;

namespace Neo.Network.P2P.Payloads
{
    public class CosignerUsage : ISerializable
    {
        public WitnessScope Scope;
        public byte[] ScopeData;
        public UInt160 Account;

        public int Size =>
            sizeof(WitnessScope) +                      // Type
            (HasData ? ScopeData.GetVarSize() : 0) +    // ScopeData
            UInt160.Length;                             // ScriptHash

        public bool HasData => (Scope == WitnessScope.CustomScriptHash) || (Scope == WitnessScope.ExecutingGroupPubKey);

        void ISerializable.Deserialize(BinaryReader reader)
        {
            Scope = (WitnessScope)reader.ReadByte();
            ScopeData = (HasData ? reader.ReadVarBytes(65536) : new byte[0]);
            Account = reader.ReadSerializable<UInt160>();
        }

        void ISerializable.Serialize(BinaryWriter writer)
        {
            writer.Write((byte)Scope);
            if (HasData) writer.WriteVarBytes(ScopeData);
            writer.Write(Account);
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
            json["account"] = Account.ToString();
            return json;
        }

        public static CosignerUsage FromJson(JObject json)
        {
            CosignerUsage usage = new CosignerUsage();
            usage.Scope = (WitnessScope)json["scope"].AsString().HexToBytes()[0];
            usage.ScopeData = json["scopeData"].AsString().HexToBytes();
            usage.Account = UInt160.Parse(json["account"].AsString());
            return usage;
        }
    }
}
