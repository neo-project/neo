using Neo.Cryptography.ECC;
using Neo.IO;
using Neo.IO.Json;
using System;
using System.IO;
using System.Linq;

namespace Neo.Network.P2P.Payloads
{
    public class Signer : ISerializable
    {
        // This limits maximum number of AllowedContracts or AllowedGroups here
        private const int MaxSubitems = 16;

        public UInt160 Account;
        public WitnessScope Scopes;
        public UInt160[] AllowedContracts;
        public ECPoint[] AllowedGroups;

        public int Size =>
            /*Account*/             UInt160.Length +
            /*Scopes*/              sizeof(WitnessScope) +
            /*AllowedContracts*/    (Scopes.HasFlag(WitnessScope.CustomContracts) ? AllowedContracts.GetVarSize() : 0) +
            /*AllowedGroups*/       (Scopes.HasFlag(WitnessScope.CustomGroups) ? AllowedGroups.GetVarSize() : 0);

        public void Deserialize(BinaryReader reader)
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

        public void Serialize(BinaryWriter writer)
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
            var json = new JObject();
            json["account"] = Account.ToString();
            json["scopes"] = Scopes;
            if (Scopes.HasFlag(WitnessScope.CustomContracts))
                json["allowedcontracts"] = AllowedContracts.Select(p => (JObject)p.ToString()).ToArray();
            if (Scopes.HasFlag(WitnessScope.CustomGroups))
                json["allowedgroups"] = AllowedGroups.Select(p => (JObject)p.ToString()).ToArray();
            return json;
        }
    }
}
