using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AntShares.Core
{
    /// <summary>
    /// 用于分发资产的特殊交易
    /// </summary>
    public class IssueTransaction : Transaction
    {
        /// <summary>
        /// 随机数
        /// </summary>
        public uint Nonce;

        /// <summary>
        /// 系统费用
        /// </summary>
        public override Fixed8 SystemFee
        {
            get
            {
                if (Outputs.All(p => p.AssetId == Blockchain.AntShare.Hash || p.AssetId == Blockchain.AntCoin.Hash))
                    return Fixed8.Zero;
#if TESTNET
                return Fixed8.Zero;
#else
                return Fixed8.FromDecimal(500);
#endif
            }
        }

        public IssueTransaction()
            : base(TransactionType.IssueTransaction)
        {
        }

        /// <summary>
        /// 反序列化交易中的额外数据
        /// </summary>
        /// <param name="reader">数据来源</param>
        protected override void DeserializeExclusiveData(BinaryReader reader)
        {
            this.Nonce = reader.ReadUInt32();
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
        /// 序列化交易中的额外数据
        /// </summary>
        /// <param name="writer">存放序列化后的结果</param>
        protected override void SerializeExclusiveData(BinaryWriter writer)
        {
            writer.Write(Nonce);
        }

        /// <summary>
        /// 验证交易
        /// </summary>
        /// <returns>返回验证后的结果</returns>
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
                if (!Blockchain.Default.Ability.HasFlag(BlockchainAbility.Statistics))
                    return false;
                Fixed8 quantity_issued = Blockchain.Default.GetQuantityIssued(r.AssetId); //TODO: 已发行量是否应考虑内存池内未被写入区块链的交易，以防止“双重发行”
                if (tx.Amount - quantity_issued < -r.Amount) return false;
            }
            return true;
        }
    }
}
