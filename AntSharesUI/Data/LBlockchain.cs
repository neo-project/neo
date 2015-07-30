using AntShares.Core;
using AntShares.IO;
using AntShares.Properties;
using AntShares.Wallets;
using LevelDB;
using System.Collections.Generic;
using System.Linq;
using Transaction = AntShares.Core.Transaction;

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
                    UInt256 hash = new UInt256(it.Key().ToArray().Skip(1).Take(32).ToArray());
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
            lock (onblock_sync_obj)
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
                                ushort index = tx.Outputs.IndexedSelect((p, i) => new
                                {
                                    Index = (ushort)i,
                                    p.AssetId,
                                    p.ScriptHash
                                }).First(p => p.AssetId == AntCoin.Hash && p.ScriptHash == enroll_tx.Miner).Index;
                                batch.Put(SliceBuilder.Begin(DataEntryPrefix.IX_Enrollment).Add(tx.Hash).Add(index), true);
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
                    batch.Delete(SliceBuilder.Begin(DataEntryPrefix.IX_Enrollment).Add(input.PrevTxId).Add(input.PrevIndex));
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
                db.Write(WriteOptions.Default, batch);
            }
        }
    }
}
