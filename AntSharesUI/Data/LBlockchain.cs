using AntShares.Core;
using AntShares.IO;
using AntShares.Properties;
using LevelDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Transaction = AntShares.Core.Transaction;

namespace AntShares.Data
{
    internal class LBlockchain : Blockchain
    {
        private DB db;
        private Thread thread_persistence;
        private MultiValueDictionary<UInt256, Block> cache = new MultiValueDictionary<UInt256, Block>();
        private object persistence_sync_obj = new object();
        private bool disposed = false;

        public override BlockchainAbility Ability => BlockchainAbility.All;

        public override bool IsReadOnly => false;

        public LBlockchain()
        {
            Slice initialized;
            db = DB.Open(Settings.Default.DataDirectoryPath);
            if (!db.TryGet(ReadOptions.Default, SliceBuilder.Begin(DataEntryPrefix.Configuration).Add("initialized"), out initialized) || !initialized.ToBoolean())
            {
                Rebuild();
            }
            thread_persistence = new Thread(PersistBlocks);
            thread_persistence.Name = "LBlockchain.PersistBlocks";
            thread_persistence.Start();
        }

        private void AddBlockToChain(Block block)
        {
            MultiValueDictionary<UInt256, ushort> unspents = new MultiValueDictionary<UInt256, ushort>(hash =>
            {
                Slice value = new byte[0];
                db.TryGet(ReadOptions.Default, SliceBuilder.Begin(DataEntryPrefix.IX_Unspent).Add(hash), out value);
                return new HashSet<ushort>(value.ToArray().GetUInt16Array());
            });
            MultiValueDictionary<UInt256, ushort> unspent_antshares = new MultiValueDictionary<UInt256, ushort>(hash =>
            {
                Slice value = new byte[0];
                db.TryGet(ReadOptions.Default, SliceBuilder.Begin(DataEntryPrefix.IX_AntShare).Add(hash), out value);
                return new HashSet<ushort>(value.ToArray().GetUInt16Array());
            });
            MultiValueDictionary<UInt256, ushort> unspent_votes = new MultiValueDictionary<UInt256, ushort>(hash =>
            {
                Slice value = new byte[0];
                db.TryGet(ReadOptions.Default, SliceBuilder.Begin(DataEntryPrefix.IX_Vote).Add(hash), out value);
                return new HashSet<ushort>(value.ToArray().GetUInt16Array());
            });
            Dictionary<UInt256, Fixed8> quantities = new Dictionary<UInt256, Fixed8>();
            WriteBatch batch = new WriteBatch();
            batch.Put(SliceBuilder.Begin(DataEntryPrefix.Block).Add(block.Hash), block.Trim());
            batch.Put(SliceBuilder.Begin(DataEntryPrefix.IX_PrevBlock).Add(block.PrevBlock), block.Hash.ToArray());
            foreach (Transaction tx in block.Transactions)
            {
                batch.Put(SliceBuilder.Begin(DataEntryPrefix.Transaction).Add(tx.Hash), tx.ToArray());
                switch (tx.Type)
                {
                    case TransactionType.IssueTransaction:
                        foreach (var group in tx.Outputs.GroupBy(p => p.AssetId).Where(g => g.All(p => p.Value > Fixed8.Zero)).Select(g => new
                        {
                            AssetId = g.Key,
                            Sum = g.Sum(p => p.Value)
                        }))
                        {
                            if (quantities.ContainsKey(group.AssetId))
                            {
                                quantities[group.AssetId] += group.Sum;
                            }
                            else
                            {
                                quantities.Add(group.AssetId, group.Sum);
                            }
                        }
                        break;
                    case TransactionType.EnrollmentTransaction:
                        {
                            EnrollmentTransaction enroll_tx = (EnrollmentTransaction)tx;
                            batch.Put(SliceBuilder.Begin(DataEntryPrefix.IX_Enrollment).Add(tx.Hash), true);
                        }
                        break;
                    case TransactionType.VotingTransaction:
                        unspent_votes.AddEmpty(tx.Hash);
                        for (ushort index = 0; index < tx.Outputs.Length; index++)
                        {
                            if (tx.Outputs[index].AssetId == AntShare.Hash)
                            {
                                unspent_votes.Add(tx.Hash, index);
                            }
                        }
                        break;
                    case TransactionType.RegisterTransaction:
                        {
                            RegisterTransaction reg_tx = (RegisterTransaction)tx;
                            batch.Put(SliceBuilder.Begin(DataEntryPrefix.IX_Asset).Add((byte)reg_tx.AssetType).Add(reg_tx.Hash), true);
                        }
                        break;
                }
                unspents.AddEmpty(tx.Hash);
                unspent_antshares.AddEmpty(tx.Hash);
                for (ushort index = 0; index < tx.Outputs.Length; index++)
                {
                    unspents.Add(tx.Hash, index);
                    if (tx.Outputs[index].AssetId == AntShare.Hash)
                    {
                        unspent_antshares.Add(tx.Hash, index);
                    }
                }
            }
            foreach (TransactionInput input in block.Transactions.SelectMany(p => p.GetAllInputs()))
            {
                if (input.PrevIndex == 0)
                {
                    batch.Delete(SliceBuilder.Begin(DataEntryPrefix.IX_Enrollment).Add(input.PrevTxId));
                }
                unspents.Remove(input.PrevTxId, input.PrevIndex);
                unspent_antshares.Remove(input.PrevTxId, input.PrevIndex);
                unspent_votes.Remove(input.PrevTxId, input.PrevIndex);
            }
            //统计AntCoin的发行量
            {
                Fixed8 amount_in = block.Transactions.SelectMany(p => p.References.Values.Where(o => o.AssetId == Blockchain.AntCoin.Hash)).Sum(p => p.Value);
                Fixed8 amount_out = block.Transactions.SelectMany(p => p.Outputs.Where(o => o.AssetId == Blockchain.AntCoin.Hash)).Sum(p => p.Value);
                if (amount_in != amount_out)
                {
                    quantities.Add(AntCoin.Hash, amount_out - amount_in);
                }
            }
            foreach (var unspent in unspents)
            {
                if (unspent.Value.Count == 0)
                {
                    batch.Delete(SliceBuilder.Begin(DataEntryPrefix.IX_Unspent).Add(unspent.Key));
                }
                else
                {
                    batch.Put(SliceBuilder.Begin(DataEntryPrefix.IX_Unspent).Add(unspent.Key), unspent.Value.ToByteArray());
                }
            }
            foreach (var unspent in unspent_antshares)
            {
                if (unspent.Value.Count == 0)
                {
                    batch.Delete(SliceBuilder.Begin(DataEntryPrefix.IX_AntShare).Add(unspent.Key));
                }
                else
                {
                    batch.Put(SliceBuilder.Begin(DataEntryPrefix.IX_AntShare).Add(unspent.Key), unspent.Value.ToByteArray());
                }
            }
            foreach (var unspent in unspent_votes)
            {
                if (unspent.Value.Count == 0)
                {
                    batch.Delete(SliceBuilder.Begin(DataEntryPrefix.IX_Vote).Add(unspent.Key));
                }
                else
                {
                    batch.Put(SliceBuilder.Begin(DataEntryPrefix.IX_Vote).Add(unspent.Key), unspent.Value.ToByteArray());
                }
            }
            foreach (var quantity in quantities)
            {
                batch.Put(SliceBuilder.Begin(DataEntryPrefix.ST_QuantityIssued).Add(quantity.Key), (GetQuantityIssued(quantity.Key) + quantity.Value).GetData());
            }
            if (block.Hash == GenesisBlock.Hash)
            {
                batch.Put(SliceBuilder.Begin(DataEntryPrefix.ST_Height), SliceBuilder.Begin().Add(block.Hash).Add(0u));
            }
            else
            {
                byte[] value = db.Get(ReadOptions.Default, SliceBuilder.Begin(DataEntryPrefix.ST_Height)).ToArray();
                uint height = BitConverter.ToUInt32(value, 32) + 1;
                batch.Put(SliceBuilder.Begin(DataEntryPrefix.ST_Height), SliceBuilder.Begin().Add(block.Hash).Add(height));
            }
            db.Write(WriteOptions.Default, batch);
        }

        public override bool ContainsBlock(UInt256 hash)
        {
            if (base.ContainsBlock(hash)) return true;
            Slice value;
            //TODO: 尝试有没有更快的方法找出LevelDB中是否存在某一条记录
            //目前只能通过尝试读取某一条记录后判断是否成功，没有其它的好办法
            //对每一条记录添加一个只包含hash的索引记录，应该能提升判断的速度
            //但是这个方法会增加数据库的大小，需要测试一下优劣
            return db.TryGet(ReadOptions.Default, SliceBuilder.Begin(DataEntryPrefix.Block).Add(hash), out value);
        }

        public override bool ContainsTransaction(UInt256 hash)
        {
            if (base.ContainsTransaction(hash)) return true;
            Slice value;
            return db.TryGet(ReadOptions.Default, SliceBuilder.Begin(DataEntryPrefix.Transaction).Add(hash), out value);
        }

        public override bool ContainsUnspent(UInt256 hash, ushort index)
        {
            Slice value;
            if (!db.TryGet(ReadOptions.Default, SliceBuilder.Begin(DataEntryPrefix.IX_Unspent).Add(hash), out value))
                return false;
            return value.ToArray().GetUInt16Array().Contains(index);
        }

        public override void Dispose()
        {
            base.Dispose();
            disposed = true;
            thread_persistence.Join();
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
                using (Snapshot snapshot = db.GetSnapshot())
                {
                    block = GetBlock(hash, snapshot);
                }
            }
            return block;
        }

        private Block GetBlock(UInt256 hash, Snapshot snapshot)
        {
            ReadOptions options = new ReadOptions { Snapshot = snapshot };
            Slice value;
            if (!db.TryGet(options, SliceBuilder.Begin(DataEntryPrefix.Block).Add(hash), out value))
                return null;
            return Block.FromTrimmedData(value.ToArray(), p => Transaction.DeserializeFrom(db.Get(options, SliceBuilder.Begin(DataEntryPrefix.Transaction).Add(p)).ToArray()));
        }

        public override IEnumerable<EnrollmentTransaction> GetEnrollments()
        {
            ReadOptions options = new ReadOptions();
            using (options.Snapshot = db.GetSnapshot())
            using (Iterator it = db.NewIterator(options))
            {
                for (it.Seek(SliceBuilder.Begin(DataEntryPrefix.IX_Enrollment)); it.Valid() && it.Key() < SliceBuilder.Begin(DataEntryPrefix.IX_Enrollment + 1); it.Next())
                {
                    UInt256 hash = new UInt256(it.Key().ToArray().Skip(1).Take(32).ToArray());
                    yield return db.Get(options, SliceBuilder.Begin(DataEntryPrefix.Transaction).Add(hash)).ToArray().AsSerializable<EnrollmentTransaction>();
                }
            }
        }

        public override Block GetNextBlock(UInt256 hash)
        {
            ReadOptions options = new ReadOptions();
            using (options.Snapshot = db.GetSnapshot())
            {
                Slice value;
                if (!db.TryGet(options, SliceBuilder.Begin(DataEntryPrefix.IX_PrevBlock).Add(hash), out value))
                    return null;
                hash = new UInt256(value.ToArray());
                return GetBlock(hash, options.Snapshot);
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
            ReadOptions options = new ReadOptions();
            using (options.Snapshot = db.GetSnapshot())
            {
                Slice value;
                if (!db.TryGet(options, SliceBuilder.Begin(DataEntryPrefix.IX_Unspent).Add(hash), out value))
                    return null;
                if (!value.ToArray().GetUInt16Array().Contains(index))
                    return null;
                Transaction tx = Transaction.DeserializeFrom(db.Get(options, SliceBuilder.Begin(DataEntryPrefix.Transaction).Add(hash)).ToArray());
                return tx.Outputs[index];
            }
        }

        public override IEnumerable<TransactionOutput> GetUnspentAntShares()
        {
            ReadOptions options = new ReadOptions();
            using (options.Snapshot = db.GetSnapshot())
            using (Iterator it = db.NewIterator(options))
            {
                for (it.Seek(SliceBuilder.Begin(DataEntryPrefix.IX_AntShare)); it.Valid() && it.Key() < SliceBuilder.Begin(DataEntryPrefix.IX_AntShare + 1); it.Next())
                {
                    UInt256 hash = new UInt256(it.Key().ToArray().Skip(1).ToArray());
                    ushort[] indexes = it.Value().ToArray().GetUInt16Array();
                    Transaction tx = Transaction.DeserializeFrom(db.Get(options, SliceBuilder.Begin(DataEntryPrefix.Transaction).Add(hash)).ToArray());
                    foreach (ushort index in indexes)
                    {
                        yield return tx.Outputs[index];
                    }
                }
            }
        }

        public override IEnumerable<Vote> GetVotes()
        {
            ReadOptions options = new ReadOptions();
            using (options.Snapshot = db.GetSnapshot())
            using (Iterator it = db.NewIterator(options))
            {
                for (it.Seek(SliceBuilder.Begin(DataEntryPrefix.IX_Vote)); it.Valid() && it.Key() < SliceBuilder.Begin(DataEntryPrefix.IX_Vote + 1); it.Next())
                {
                    UInt256 hash = new UInt256(it.Key().ToArray().Skip(1).ToArray());
                    ushort[] indexes = it.Value().ToArray().GetUInt16Array();
                    VotingTransaction tx = db.Get(options, SliceBuilder.Begin(DataEntryPrefix.Transaction).Add(hash)).ToArray().AsSerializable<VotingTransaction>();
                    yield return new Vote
                    {
                        Enrollments = tx.Enrollments,
                        Count = indexes.Sum(p => tx.Outputs[p].Value)
                    };
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
                foreach (var group in inputs.GroupBy(p => p.PrevTxId))
                {
                    Slice value;
                    if (!db.TryGet(options, SliceBuilder.Begin(DataEntryPrefix.IX_Unspent).Add(group.Key), out value))
                        return true;
                    HashSet<ushort> unspents = new HashSet<ushort>(value.ToArray().GetUInt16Array());
                    if (group.Any(p => !unspents.Contains(p.PrevIndex)))
                        return true;
                }
            }
            return false;
        }

        protected override void OnBlock(Block block)
        {
            base.OnBlock(block);
            lock (cache)
            {
                //TODO: 需要清理缓存
                cache.Add(block.PrevBlock, block);
            }
        }

        private void PersistBlocks()
        {
            while (!disposed)
            {
                Block block = null;
                lock (persistence_sync_obj)
                {
                    UInt256 hash = new UInt256(db.Get(ReadOptions.Default, SliceBuilder.Begin(DataEntryPrefix.ST_Height)).ToArray().Take(32).ToArray());
                    lock (cache)
                    {
                        if (cache.ContainsKey(hash) && cache[hash].Count > 0)
                        {
                            block = cache[hash].First();
                        }
                    }
                    if (block != null)
                    {
                        //TODO: 要考虑到有多个候选区块的情况
                        //TODO: 要考虑分叉的情况
                        AddBlockToChain(block);
                    }
                }
                if (block == null)
                {
                    Thread.Sleep(TimeSpan.FromSeconds(5));
                }
                else
                {
                    lock (cache)
                    {
                        cache.Remove(block.Hash);
                    }
                }
            }
        }

        private void Rebuild()
        {
            WriteBatch batch = new WriteBatch();
            using (Iterator it = db.NewIterator(ReadOptions.Default))
            {
                for (it.SeekToFirst(); it.Valid(); it.Next())
                {
                    batch.Delete(it.Key());
                }
            }
            batch.Put(SliceBuilder.Begin(DataEntryPrefix.Configuration).Add("version"), 0);
            db.Write(WriteOptions.Default, batch);
            AddBlockToChain(GenesisBlock);
            db.Put(WriteOptions.Default, SliceBuilder.Begin(DataEntryPrefix.Configuration).Add("initialized"), true);
        }

        private void Rollback(UInt256 hash)
        {
            lock (persistence_sync_obj)
            {
                byte[] data = db.Get(ReadOptions.Default, SliceBuilder.Begin(DataEntryPrefix.ST_Height)).ToArray();
                UInt256 hash_current = new UInt256(data.Take(32).ToArray());
                if (hash_current == hash) return;
                uint height = BitConverter.ToUInt32(data, 32);
                List<Block> blocks = new List<Block>();
                while (hash_current != hash)
                {
                    if (hash_current == GenesisBlock.Hash)
                        throw new InvalidOperationException();
                    Block block = GetBlock(hash_current, null);
                    blocks.Add(block);
                    hash_current = block.PrevBlock;
                }
                WriteBatch batch = new WriteBatch();
                foreach (Block block in blocks)
                {
                    batch.Delete(SliceBuilder.Begin(DataEntryPrefix.Block).Add(block.Hash));
                    batch.Delete(SliceBuilder.Begin(DataEntryPrefix.IX_PrevBlock).Add(block.PrevBlock));
                    foreach (Transaction tx in block.Transactions)
                    {
                        batch.Delete(SliceBuilder.Begin(DataEntryPrefix.Transaction).Add(tx.Hash));
                        //TODO: 回滚期间删除及恢复交易相关数据、索引及统计信息
                    }
                }
                batch.Put(SliceBuilder.Begin(DataEntryPrefix.ST_Height), SliceBuilder.Begin().Add(hash_current).Add(height - (uint)blocks.Count));
                db.Write(WriteOptions.Default, batch);
            }
        }
    }
}
