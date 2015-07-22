using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AntShares.Core
{
    public class IssueTransaction : Transaction
    {
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
                if (tx.RegisterType == RegisterType.Share)
                {
                    hashes.UnionWith(group.Select(p => p.ScriptHash));
                }
            }
            return hashes.OrderBy(p => p).ToArray();
        }

        protected override void SerializeExclusiveData(BinaryWriter writer)
        {
        }

        internal override bool VerifyBalance()
        {
            IDictionary<UInt256, TransactionResult> results = GetTransactionResults();
            if (!results.ContainsKey(Blockchain.AntCoin.Hash) || results[Blockchain.AntCoin.Hash].Amount < SystemFee)
                return false;
            foreach (TransactionResult result in results.Values.Where(p => p.AssetId != Blockchain.AntCoin.Hash))
            {
                if (result.Amount > 0) return false;
                RegisterTransaction tx = Blockchain.Default.GetTransaction(result.AssetId) as RegisterTransaction;
                if (tx == null) return false;
                if (tx.Amount < 0) continue;
                if (tx.Amount == 0) return false;
                long quantity_issued = Blockchain.Default.GetQuantityIssued(result.AssetId); //TODO: 已发行量是否应考虑内存池内未被写入区块链的交易，以防止“双重发行”
                if (tx.Amount - quantity_issued < -result.Amount)
                    return false;
            }
            return true;
        }
    }
}
