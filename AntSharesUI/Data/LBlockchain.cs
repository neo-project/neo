using AntShares.Core;
using AntShares.IO;
using AntShares.Properties;
using LevelDB;
using System.Collections.Generic;
using System.Linq;

namespace AntShares.Data
{
    internal class LBlockchain : Blockchain
    {
        private DB db;
        private object onblock_sync_obj = new object();

        public override BlockchainAbility Ability => BlockchainAbility.All;

        public override bool IsReadOnly => false;

        public LBlockchain()
        {
            Slice initialized;
            db = DB.Open(Settings.Default.DataDirectoryPath);
            if (!db.TryGet(ReadOptions.Default, SliceBuilder.Begin(DataEntryPrefix.Configuration).Add("initialized"), out initialized) || !initialized.ToBoolean())
            {
                OnBlock(Blockchain.GenesisBlock);
                db.Put(WriteOptions.Default, SliceBuilder.Begin(DataEntryPrefix.Configuration).Add("version"), 0);
                db.Put(WriteOptions.Default, SliceBuilder.Begin(DataEntryPrefix.Configuration).Add("initialized"), true);
            }
        }

        public override bool ContainsBlock(UInt256 hash)
        {
            if (base.ContainsBlock(hash)) return true;
            Slice value;
            //TODO: 尝试有没有更快的方法找出LevelDB中是否存在某一条记录，而不是尝试读取某一条记录后判断是否成功
            return db.TryGet(ReadOptions.Default, SliceBuilder.Begin(DataEntryPrefix.Block).Add(hash), out value);
        }

        public override bool ContainsTransaction(UInt256 hash)
        {
            if (base.ContainsTransaction(hash)) return true;
            Slice value;
            //TODO: 尝试有没有更快的方法找出LevelDB中是否存在某一条记录，而不是尝试读取某一条记录后判断是否成功
            return db.TryGet(ReadOptions.Default, SliceBuilder.Begin(DataEntryPrefix.Transaction).Add(hash), out value);
        }

        public override void Dispose()
        {
            base.Dispose();
            if (db != null)
            {
                db.Dispose();
                db = null;
            }
        }

        public override IEnumerable<RegisterTransaction> GetAssets()
        {
            yield return Blockchain.AntCoin;
            ReadOptions options = new ReadOptions();
            using (options.Snapshot = db.GetSnapshot())
            using (Iterator it = db.NewIterator(options))
            {
                for (it.Seek(SliceBuilder.Begin(DataEntryPrefix.IX_Asset)); it.Valid() && it.Key() < SliceBuilder.Begin(DataEntryPrefix.IX_Asset + 1); it.Next())
                {
                    UInt256 hash = new UInt256(it.Key().ToArray().Skip(2).ToArray());
                    yield return db.Get(options, SliceBuilder.Begin(DataEntryPrefix.Transaction).Add(hash)).ToArray().AsSerializable<RegisterTransaction>();
                }
            }
        }

        public override Block GetBlock(UInt256 hash)
        {
            Block block = base.GetBlock(hash);
            if (block == null)
            {
                Slice value;
                if (db.TryGet(ReadOptions.Default, SliceBuilder.Begin(DataEntryPrefix.Block).Add(hash), out value))
                {
                    block = value.ToArray().AsSerializable<Block>();
                }
            }
            return block;
        }

        public override IEnumerable<EnrollmentTransaction> GetEnrollments()
        {
            ReadOptions options = new ReadOptions();
            using (options.Snapshot = db.GetSnapshot())
            using (Iterator it = db.NewIterator(options))
            {
                for (it.Seek(SliceBuilder.Begin(DataEntryPrefix.IX_Enrollment)); it.Valid() && it.Key() < SliceBuilder.Begin(DataEntryPrefix.IX_Enrollment + 1); it.Next())
                {
                    UInt256 hash = new UInt256(it.Key().ToArray().Skip(1).ToArray());
                    yield return db.Get(options, SliceBuilder.Begin(DataEntryPrefix.Transaction).Add(hash)).ToArray().AsSerializable<EnrollmentTransaction>();
                }
            }
        }

        public override Fixed8 GetQuantityIssued(UInt256 asset_id)
        {
            Slice quantity = 0L;
            db.TryGet(ReadOptions.Default, SliceBuilder.Begin(DataEntryPrefix.ST_QuantityIssued).Add(asset_id), out quantity);
            return new Fixed8(quantity.ToInt64());
        }

        public override Transaction GetTransaction(UInt256 hash)
        {
            Transaction tx = base.GetTransaction(hash);
            if (tx == null)
            {
                Slice value;
                if (db.TryGet(ReadOptions.Default, SliceBuilder.Begin(DataEntryPrefix.Transaction).Add(hash), out value))
                {
                    tx = Transaction.DeserializeFrom(value.ToArray());
                }
            }
            return tx;
        }

        public override TransactionOutput GetUnspent(UInt256 hash, ushort index)
        {
            Slice value;
            if (!db.TryGet(ReadOptions.Default, SliceBuilder.Begin(DataEntryPrefix.Unspent).Add(hash).Add(index), out value))
                return null;
            return value.ToArray().AsSerializable<TransactionOutput>();
        }

        public override IEnumerable<TransactionOutput> GetUnspentAntShares()
        {
            using (Iterator it = db.NewIterator(ReadOptions.Default))
            {
                for (it.Seek(SliceBuilder.Begin(DataEntryPrefix.IX_AntShare)); it.Valid() && it.Key() < SliceBuilder.Begin(DataEntryPrefix.IX_AntShare + 1); it.Next())
                {
                    yield return it.Value().ToArray().AsSerializable<TransactionOutput>();
                }
            }
        }

        public override IEnumerable<TransactionInput> GetVotes()
        {
            using (Iterator it = db.NewIterator(ReadOptions.Default))
            {
                for (it.Seek(SliceBuilder.Begin(DataEntryPrefix.IX_Vote)); it.Valid() && it.Key() < SliceBuilder.Begin(DataEntryPrefix.IX_Vote + 1); it.Next())
                {
                    yield return it.Key().ToArray().Skip(1).ToArray().AsSerializable<TransactionInput>();
                }
            }
        }

        public override bool IsDoubleSpend(Transaction tx)
        {
            TransactionInput[] inputs = tx.GetAllInputs().ToArray();
            if (inputs.Length == 0) return false;
            ReadOptions options = new ReadOptions();
            using (options.Snapshot = db.GetSnapshot())
            {
                foreach (TransactionInput input in inputs)
                {
                    Slice value;
                    if (!db.TryGet(options, SliceBuilder.Begin(DataEntryPrefix.Unspent).Add(input.PrevTxId).Add(input.PrevIndex), out value))
                        return true;
                }
            }
            return false;
        }

        protected override void OnBlock(Block block)
        {
            base.OnBlock(block);
            lock (onblock_sync_obj)
            {
                Dictionary<UInt256, Fixed8> assets = new Dictionary<UInt256, Fixed8>();
                WriteBatch batch = new WriteBatch();
                batch.Put(SliceBuilder.Begin(DataEntryPrefix.Block).Add(block.Hash), block.Trim());
                foreach (Transaction tx in block.Transactions)
                {
                    batch.Put(SliceBuilder.Begin(DataEntryPrefix.Transaction).Add(tx.Hash), tx.ToArray());
                    switch (tx.Type)
                    {
                        case TransactionType.IssueTransaction:
                            foreach (var asset in tx.Outputs.GroupBy(p => p.AssetId).Where(g => g.All(p => p.Value > Fixed8.Zero)).Select(g => new
                            {
                                AssetId = g.Key,
                                Sum = g.Sum(p => p.Value)
                            }))
                            {
                                if (assets.ContainsKey(asset.AssetId))
                                {
                                    assets[asset.AssetId] += asset.Sum;
                                }
                                else
                                {
                                    assets.Add(asset.AssetId, asset.Sum);
                                }
                            }
                            break;
                        case TransactionType.EnrollTransaction:
                            batch.Put(SliceBuilder.Begin(DataEntryPrefix.IX_Enrollment).Add(tx.Hash), true);
                            break;
                        case TransactionType.VotingTransaction:
                            for (ushort index = 0; index < tx.Outputs.Length; index++)
                            {
                                if (tx.Outputs[index].AssetId == AntShare.Hash)
                                {
                                    batch.Put(SliceBuilder.Begin(DataEntryPrefix.IX_Vote).Add(tx.Hash).Add(index), true);
                                }
                            }
                            break;
                        case TransactionType.RegisterTransaction:
                            RegisterTransaction reg_tx = (RegisterTransaction)tx;
                            batch.Put(SliceBuilder.Begin(DataEntryPrefix.IX_Asset).Add((byte)reg_tx.AssetType).Add(reg_tx.Hash), true);
                            break;
                    }
                    for (ushort index = 0; index < tx.Outputs.Length; index++)
                    {
                        batch.Put(SliceBuilder.Begin(DataEntryPrefix.Unspent).Add(tx.Hash).Add(index), tx.Outputs[index].ToArray());
                        if (tx.Outputs[index].AssetId == AntShare.Hash)
                        {
                            batch.Put(SliceBuilder.Begin(DataEntryPrefix.IX_AntShare).Add(tx.Hash).Add(index), tx.Outputs[index].ToArray());
                        }
                    }
                }
                foreach (TransactionInput input in block.Transactions.SelectMany(p => p.GetAllInputs()))
                {
                    batch.Delete(SliceBuilder.Begin(DataEntryPrefix.Unspent).Add(input.PrevTxId).Add(input.PrevIndex));
                    batch.Delete(SliceBuilder.Begin(DataEntryPrefix.IX_Enrollment).Add(input.PrevTxId));
                    batch.Delete(SliceBuilder.Begin(DataEntryPrefix.IX_AntShare).Add(input.PrevTxId).Add(input.PrevIndex));
                    batch.Delete(SliceBuilder.Begin(DataEntryPrefix.IX_Vote).Add(input.PrevTxId).Add(input.PrevIndex));
                }
                //统计AntCoin的发行量
                {
                    Fixed8 amount_in = block.Transactions.SelectMany(p => p.References.Values.Where(o => o.AssetId == Blockchain.AntCoin.Hash)).Sum(p => p.Value);
                    Fixed8 amount_out = block.Transactions.SelectMany(p => p.Outputs.Where(o => o.AssetId == Blockchain.AntCoin.Hash)).Sum(p => p.Value);
                    if (amount_in != amount_out)
                    {
                        Fixed8 quantity = GetQuantityIssued(AntCoin.Hash) - amount_in + amount_out;
                        assets.Add(AntCoin.Hash, quantity);
                    }
                }
                foreach (var asset in assets)
                {
                    Slice amount = 0L;
                    db.TryGet(ReadOptions.Default, SliceBuilder.Begin(DataEntryPrefix.ST_QuantityIssued).Add(asset.Key), out amount);
                    batch.Put(SliceBuilder.Begin(DataEntryPrefix.ST_QuantityIssued).Add(asset.Key), amount.ToInt64() + asset.Value.GetData());
                }
                db.Write(WriteOptions.Default, batch);
            }
        }
    }
}
