using AntShares.IO;
using AntShares.IO.Json;
using AntShares.Wallets;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AntShares.Core
{
    public class ClaimTransaction : Transaction
    {
        public TransactionInput[] Claims;

        public ClaimTransaction()
            : base(TransactionType.ClaimTransaction)
        {
        }

        protected override void DeserializeExclusiveData(BinaryReader reader)
        {
            Claims = reader.ReadSerializableArray<TransactionInput>();
            if (Claims.Length == 0) throw new FormatException();
            if (Claims.Length != Claims.Distinct().Count())
                throw new FormatException();
        }

        public override UInt160[] GetScriptHashesForVerifying()
        {
            HashSet<UInt160> hashes = new HashSet<UInt160>(base.GetScriptHashesForVerifying());
            foreach (var group in Claims.GroupBy(p => p.PrevHash))
            {
                Transaction tx = Blockchain.Default.GetTransaction(group.Key);
                if (tx == null) throw new InvalidOperationException();
                foreach (TransactionInput claim in group)
                {
                    if (tx.Outputs.Length <= claim.PrevIndex) throw new InvalidOperationException();
                    hashes.Add(tx.Outputs[claim.PrevIndex].ScriptHash);
                }
            }
            return hashes.OrderBy(p => p).ToArray();
        }

        protected override void SerializeExclusiveData(BinaryWriter writer)
        {
            writer.Write(Claims);
        }

        public override JObject ToJson()
        {
            JObject json = base.ToJson();
            json["claims"] = new JArray(Claims.Select(p => p.ToJson()).ToArray());
            return json;
        }

        public override bool Verify()
        {
            if (!base.Verify()) return false;
            TransactionResult result = GetTransactionResults().FirstOrDefault(p => p.AssetId == Blockchain.AntCoin.Hash);
            if (result == null || result.Amount > Fixed8.Zero) return false;
            try
            {
                return Wallet.CalculateClaimAmount(Claims) == -result.Amount;
            }
            catch (ArgumentException)
            {
                return false;
            }
            catch (NotSupportedException)
            {
                return false;
            }
        }
    }
}
