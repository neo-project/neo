using System;
using System.IO;
using System.Linq;
using Neo.Cryptography.ECC;
using Neo.IO;
using Neo.IO.Json;

namespace Neo.Models
{
    public class Signer : ISerializable
    {
        // This limits maximum number of AllowedContracts or AllowedGroups here
        private const int MaxSubitems = 16;

        public UInt160 Account;
        public WitnessScope Scopes;
        public UInt160[] AllowedContracts;
        public ECPoint[] AllowedGroups;

        int ISerializable.Size =>
            UInt160.Length +            // Account
            sizeof(WitnessScope) +      // Scopes
            (Scopes.HasFlag(WitnessScope.CustomContracts) ? AllowedContracts.GetVarSize() : 0) +    // AllowedContracts
            (Scopes.HasFlag(WitnessScope.CustomGroups) ? AllowedGroups.GetVarSize() : 0);           // AllowedGroups

        void ISerializable.Deserialize(BinaryReader reader)
        {
            Account = reader.ReadSerializable<UInt160>();
            Scopes = (WitnessScope)reader.ReadByte();
            if ((Scopes & ~(WitnessScope.CalledByEntry | WitnessScope.CustomContracts | WitnessScope.CustomGroups | WitnessScope.Global)) != 0)
                throw new FormatException();
            if (Scopes.HasFlag(WitnessScope.Global) && Scopes != WitnessScope.Global)
                throw new FormatException();
            AllowedContracts = Scopes.HasFlag(WitnessScope.CustomContracts)
                ? reader.ReadSerializableArray<UInt160>(MaxSubitems)
                : new UInt160[0];
            AllowedGroups = Scopes.HasFlag(WitnessScope.CustomGroups)
                ? reader.ReadSerializableArray<ECPoint>(MaxSubitems)
                : new ECPoint[0];
        }

        void ISerializable.Serialize(BinaryWriter writer)
        {
            writer.Write(Account);
            writer.Write((byte)Scopes);
            if (Scopes.HasFlag(WitnessScope.CustomContracts))
                writer.Write(AllowedContracts);
            if (Scopes.HasFlag(WitnessScope.CustomGroups))
                writer.Write(AllowedGroups);
        }

        public JObject ToJson()
        {
            return new JObject()
            {
                ["account"] = Account.ToString(),
                ["scopes"] = Scopes,
                ["allowedcontracts"] = Scopes.HasFlag(WitnessScope.CustomContracts)
                    ? AllowedContracts.Select(p => (JObject)p.ToString()).ToArray()
                    : JObject.Null,
                ["allowedgroups"] = Scopes.HasFlag(WitnessScope.CustomGroups)
                    ? AllowedGroups.Select(p => (JObject)p.ToString()).ToArray()
                    : JObject.Null,
            };
        }

        public static Signer FromJson(JObject json, byte addressVersion)
        {
            return new Signer
            {
                Account = json["account"].ToScriptHash(addressVersion),
                Scopes = (WitnessScope)Enum.Parse(typeof(WitnessScope), json["scopes"].AsString()),
                AllowedContracts = (json["allowedContracts"] as JArray)?.Select(p => p.ToScriptHash(addressVersion)).ToArray(),
                AllowedGroups = (json["allowedGroups"] as JArray)?.Select(p => ECPoint.Parse(p.AsString(), ECCurve.Secp256r1)).ToArray()
            };
        }
    }
}
