using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AntShares.Core
{
    public class IssueTransaction : Transaction
    {
        public override Fixed8 SystemFee => Fixed8.FromDecimal(500);

        public IssueTransaction()
            : base(TransactionType.IssueTransaction)
        {
        }

        protected override void DeserializeExclusiveData(BinaryReader reader)
        {
        }

        public override UInt160[] GetScriptHashesForVerifying()
        {
            HashSet<UInt160> hashes = new HashSet<UInt160>(base.GetScriptHashesForVerifying());
            foreach (var group in Outputs.GroupBy(p => p.AssetId))
            {
                RegisterTransaction tx = Blockchain.Default.GetTransaction(group.Key) as RegisterTransaction;
                if (tx == null) throw new InvalidOperationException();
                hashes.Add(tx.Admin);
                if (tx.AssetType == AssetType.Share)
                {
                    hashes.UnionWith(group.Select(p => p.ScriptHash));
                }
            }
            return hashes.OrderBy(p => p).ToArray();
        }

        protected override void SerializeExclusiveData(BinaryWriter writer)
        {
        }

        public override bool Verify()
        {
            if (!base.Verify()) return false;
            TransactionResult[] results = GetTransactionResults()?.Where(p => p.Amount < Fixed8.Zero).ToArray();
            if (results == null) return false;
            foreach (TransactionResult r in results)
            {
                RegisterTransaction tx = Blockchain.Default.GetTransaction(r.AssetId) as RegisterTransaction;
                if (tx == null) return false;
                if (tx.Amount < Fixed8.Zero) continue;
                if (tx.Amount == Fixed8.Zero) return false;
                if (!Blockchain.Default.Ability.HasFlag(BlockchainAbility.Statistics))
                    return false;
                Fixed8 quantity_issued = Blockchain.Default.GetQuantityIssued(r.AssetId); //TODO: 已发行量是否应考虑内存池内未被写入区块链的交易，以防止“双重发行”
                if (tx.Amount - quantity_issued < -r.Amount) return false;
            }
            return true;
        }
    }
}
