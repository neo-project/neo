using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AntShares.Core
{
    public class IssueTransaction : Transaction
    {
        public uint Nonce;

        public override Fixed8 SystemFee => Outputs.All(p => p.AssetId == Blockchain.AntShare.Hash || p.AssetId == Blockchain.AntCoin.Hash) ? Fixed8.Zero : Fixed8.FromDecimal(500);

        public IssueTransaction()
            : base(TransactionType.IssueTransaction)
        {
        }

        protected override void DeserializeExclusiveData(BinaryReader reader)
        {
            this.Nonce = reader.ReadUInt32();
        }

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

        protected override void SerializeExclusiveData(BinaryWriter writer)
        {
            writer.Write(Nonce);
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
                if (!Blockchain.Default.Ability.HasFlag(BlockchainAbility.Statistics))
                    return false;
                Fixed8 quantity_issued = Blockchain.Default.GetQuantityIssued(r.AssetId); //TODO: 已发行量是否应考虑内存池内未被写入区块链的交易，以防止“双重发行”
                if (tx.Amount - quantity_issued < -r.Amount) return false;
            }
            return true;
        }
    }
}
