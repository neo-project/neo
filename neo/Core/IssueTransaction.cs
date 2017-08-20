using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Neo.Core
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
                if (Version >= 1) return Fixed8.Zero;
                if (Outputs.All(p => p.AssetId == Blockchain.GoverningToken.Hash || p.AssetId == Blockchain.UtilityToken.Hash))
                    return Fixed8.Zero;
                return base.SystemFee;
            }
        }

        public IssueTransaction()
            : base(TransactionType.IssueTransaction)
        {
        }

        protected override void DeserializeExclusiveData(BinaryReader reader)
        {
            if (Version > 1) throw new FormatException();
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
                AssetState asset = Blockchain.Default.GetAssetState(result.AssetId);
                if (asset == null) throw new InvalidOperationException();
                hashes.Add(asset.Issuer);
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
                AssetState asset = Blockchain.Default.GetAssetState(r.AssetId);
                if (asset == null) return false;
                if (asset.Amount < Fixed8.Zero) continue;
                Fixed8 quantity_issued = asset.Available + mempool.OfType<IssueTransaction>().Where(p => p != this).SelectMany(p => p.Outputs).Where(p => p.AssetId == r.AssetId).Sum(p => p.Value);
                if (asset.Amount - quantity_issued < -r.Amount) return false;
            }
            return true;
        }
    }
}
