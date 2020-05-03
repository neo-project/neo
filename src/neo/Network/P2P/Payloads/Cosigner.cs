using Neo.Cryptography.ECC;
using Neo.IO;
using Neo.IO.Json;
using System.IO;
using System.Linq;

namespace Neo.Network.P2P.Payloads
{
    public class Cosigner : TransactionAttribute
    {
        // This limits maximum number of AllowedContracts or AllowedGroups here
        private const int MaxSubitems = 16;

        public UInt160 Account;
        public WitnessScope Scopes;
        public UInt160[] AllowedContracts;
        public ECPoint[] AllowedGroups;

        public override TransactionAttributeUsage Usage => TransactionAttributeUsage.Cosigner;

        public Cosigner()
        {
            this.Scopes = WitnessScope.Global;
        }

        public override int Size =>
            /*Account*/             UInt160.Length +
            /*Scopes*/              sizeof(WitnessScope) +
            /*AllowedContracts*/    (Scopes.HasFlag(WitnessScope.CustomContracts) ? AllowedContracts.GetVarSize() : 0) +
            /*AllowedGroups*/       (Scopes.HasFlag(WitnessScope.CustomGroups) ? AllowedGroups.GetVarSize() : 0);

        public override void Deserialize(BinaryReader reader)
        {
            Account = reader.ReadSerializable<UInt160>();
            Scopes = (WitnessScope)reader.ReadByte();
            AllowedContracts = Scopes.HasFlag(WitnessScope.CustomContracts)
                ? reader.ReadSerializableArray<UInt160>(MaxSubitems)
                : new UInt160[0];
            AllowedGroups = Scopes.HasFlag(WitnessScope.CustomGroups)
                ? reader.ReadSerializableArray<ECPoint>(MaxSubitems)
                : new ECPoint[0];
        }

        public override void Serialize(BinaryWriter writer)
        {
            writer.Write(Account);
            writer.Write((byte)Scopes);
            if (Scopes.HasFlag(WitnessScope.CustomContracts))
                writer.Write(AllowedContracts);
            if (Scopes.HasFlag(WitnessScope.CustomGroups))
                writer.Write(AllowedGroups);
        }

        protected override void Deserialize(JObject json)
        {
            Account = UInt160.Parse(json["account"].AsString());
            Scopes = (WitnessScope)Enum.Parse(typeof(WitnessScope), json["scopes"].AsString());
            AllowedContracts = ((JArray)json["allowedContracts"])?.Select(p => UInt160.Parse(p.AsString())).ToArray();
            AllowedGroups = ((JArray)json["allowedGroups"])?.Select(p => ECPoint.Parse(p.AsString(), ECCurve.Secp256r1)).ToArray();
        }

        protected override void Serialize(JObject json)
        {
            json["account"] = Account.ToString();
            json["scopes"] = Scopes;
            if (Scopes.HasFlag(WitnessScope.CustomContracts))
                json["allowedContracts"] = AllowedContracts.Select(p => (JObject)p.ToString()).ToArray();
            if (Scopes.HasFlag(WitnessScope.CustomGroups))
                json["allowedGroups"] = AllowedGroups.Select(p => (JObject)p.ToString()).ToArray();
            return json;
        }
    }
}
