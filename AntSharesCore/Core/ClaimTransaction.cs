using AntShares.IO;
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

        public override bool Verify()
        {
            if (!Blockchain.Default.Ability.HasFlag(BlockchainAbility.UnspentIndexes))
                return false;
            if (!base.Verify()) return false;
            List<Claimable> unclaimed = new List<Claimable>();
            foreach (var group in Claims.GroupBy(p => p.PrevHash))
            {
                Dictionary<ushort, Claimable> claimable = Blockchain.Default.GetUnclaimed(group.Key);
                if (claimable == null || claimable.Count == 0) return false;
                foreach (TransactionInput claim in group)
                {
                    if (!claimable.ContainsKey(claim.PrevIndex)) return false;
                    unclaimed.Add(claimable[claim.PrevIndex]);
                }
            }
            Fixed8 amount_claimed = Fixed8.Zero;
            foreach (var group in unclaimed.GroupBy(p => new { p.StartHeight, p.EndHeight }))
            {
                uint amount = 0;
                uint ustart = group.Key.StartHeight / Blockchain.DecrementInterval;
                if (ustart < Blockchain.MintingAmount.Length)
                {
                    uint istart = group.Key.StartHeight % Blockchain.DecrementInterval;
                    uint uend = group.Key.EndHeight / Blockchain.DecrementInterval;
                    uint iend = group.Key.EndHeight % Blockchain.DecrementInterval;
                    if (uend >= Blockchain.MintingAmount.Length)
                    {
                        uend = (uint)Blockchain.MintingAmount.Length;
                        iend = 0;
                    }
                    if (iend == 0)
                    {
                        uend--;
                        iend = Blockchain.DecrementInterval;
                    }
                    while (ustart < uend)
                    {
                        amount += (Blockchain.DecrementInterval - istart) * Blockchain.MintingAmount[ustart];
                        ustart++;
                        istart = 0;
                    }
                    amount += (iend - istart) * Blockchain.MintingAmount[ustart];
                }
                amount += (uint)(Blockchain.Default.GetSysFeeAmount(group.Key.EndHeight - 1) - (group.Key.StartHeight == 0 ? 0 : Blockchain.Default.GetSysFeeAmount(group.Key.StartHeight - 1)));
                amount_claimed += group.Sum(p => p.Value) / 100000000 * amount;
            }
            TransactionResult result = GetTransactionResults().FirstOrDefault(p => p.AssetId == Blockchain.AntCoin.Hash);
            if (result == null || result.Amount > Fixed8.Zero) return true;
            return amount_claimed >= -result.Amount;
        }
    }
}
