using System;
using System.Collections.Generic;
using System.Linq;

namespace AntShares.Core
{
    /// <summary>
    /// 用于分发资产的特殊交易
    /// </summary>
    public class IssueTransaction : Transaction
    {
        /// <summary>
        /// 系统费用
        /// </summary>
        public override Fixed8 SystemFee
        {
            get
            {
                if (Outputs.All(p => p.AssetId == Blockchain.AntShare.Hash || p.AssetId == Blockchain.AntCoin.Hash))
                    return Fixed8.Zero;
                return base.SystemFee;
            }
        }

        public IssueTransaction()
            : base(TransactionType.IssueTransaction)
        {
        }

        /// <summary>
        /// 获取需要校验的脚本散列值
        /// </summary>
        /// <returns>返回需要校验的脚本散列值</returns>
        public override UInt160[] GetScriptHashesForVerifying()
        {
            HashSet<UInt160> hashes = new HashSet<UInt160>(base.GetScriptHashesForVerifying());
            foreach (TransactionResult result in GetTransactionResults().Where(p => p.Amount < Fixed8.Zero))
            {
                RegisterTransaction tx = Blockchain.Default.GetTransaction(result.AssetId) as RegisterTransaction;
                if (tx == null) throw new InvalidOperationException();
                hashes.Add(tx.Admin);
            }
            return hashes.OrderBy(p => p).ToArray();
        }

        /// <summary>
        /// 验证交易
        /// </summary>
        /// <returns>返回验证后的结果</returns>
        public override bool Verify(IEnumerable<Transaction> mempool)
        {
            if (!base.Verify(mempool)) return false;
            TransactionResult[] results = GetTransactionResults()?.Where(p => p.Amount < Fixed8.Zero).ToArray();
            if (results == null) return false;
            foreach (TransactionResult r in results)
            {
                RegisterTransaction tx = Blockchain.Default.GetTransaction(r.AssetId) as RegisterTransaction;
                if (tx == null) return false;
                if (tx.Amount < Fixed8.Zero) continue;
                if (!Blockchain.Default.Ability.HasFlag(BlockchainAbility.Statistics))
                    return false;
                Fixed8 quantity_issued = Blockchain.Default.GetQuantityIssued(r.AssetId);
                quantity_issued += mempool.OfType<IssueTransaction>().Where(p => p != this).SelectMany(p => p.Outputs).Where(p => p.AssetId == r.AssetId).Sum(p => p.Value);
                if (tx.Amount - quantity_issued < -r.Amount) return false;
            }
            return true;
        }
    }
}
