using AntShares.Cryptography.ECC;
using AntShares.IO;
using AntShares.VM;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AntShares.Core
{
    public class AccountState : StateBase, ICloneable<AccountState>
    {
        public UInt160 ScriptHash;
        public bool IsFrozen;
        public ECPoint[] Votes;
        public Dictionary<UInt256, Fixed8> Balances;

        public override int Size => base.Size + ScriptHash.Size + sizeof(bool) + Votes.GetVarSize()
            + IO.Helper.GetVarSize(Balances.Count) + Balances.Count * (32 + 8);

        AccountState ICloneable<AccountState>.Clone()
        {
            return new AccountState
            {
                ScriptHash = ScriptHash,
                IsFrozen = IsFrozen,
                Votes = Votes,
                Balances = Balances.ToDictionary(p => p.Key, p => p.Value)
            };
        }

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);
            ScriptHash = reader.ReadSerializable<UInt160>();
            IsFrozen = reader.ReadBoolean();
            Votes = new ECPoint[reader.ReadVarInt()];
            for (int i = 0; i < Votes.Length; i++)
                Votes[i] = ECPoint.DeserializeFrom(reader, ECCurve.Secp256r1);
            int count = (int)reader.ReadVarInt();
            Balances = new Dictionary<UInt256, Fixed8>(count);
            for (int i = 0; i < count; i++)
            {
                UInt256 assetId = reader.ReadSerializable<UInt256>();
                Fixed8 value = reader.ReadSerializable<Fixed8>();
                Balances.Add(assetId, value);
            }
        }

        void ICloneable<AccountState>.FromReplica(AccountState replica)
        {
            ScriptHash = replica.ScriptHash;
            IsFrozen = replica.IsFrozen;
            Votes = replica.Votes;
            Balances = replica.Balances;
        }

        public override void Serialize(BinaryWriter writer)
        {
            base.Serialize(writer);
            writer.Write(ScriptHash);
            writer.Write(IsFrozen);
            writer.Write(Votes);
            var balances = Balances.Where(p => p.Value > Fixed8.Zero).ToArray();
            writer.WriteVarInt(balances.Length);
            foreach (var pair in balances)
            {
                writer.Write(pair.Key);
                writer.Write(pair.Value);
            }
        }
    }
}
