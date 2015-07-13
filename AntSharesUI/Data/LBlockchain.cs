using AntShares.Core;
using AntShares.IO;
using AntShares.Properties;
using LevelDB;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AntShares.Data
{
    internal class LBlockchain : Blockchain, IDisposable
    {
        private DB db;

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

        public void Dispose()
        {
            if (db != null)
            {
                db.Dispose();
                db = null;
            }
        }

        public override IEnumerable<RegisterTransaction> GetAssets()
        {
            yield return Blockchain.AntCoin;
            using (Iterator it = db.NewIterator(ReadOptions.Default))
            {
                for (it.Seek(SliceBuilder.Begin(DataEntryPrefix.IX_Register)); it.Valid() && it.Key() < SliceBuilder.Begin(DataEntryPrefix.IX_Register + 1); it.Next())
                {
                    yield return it.Value().ToArray().AsSerializable<RegisterTransaction>();
                }
            }
        }

        public override long GetQuantityIssued(UInt256 asset_type)
        {
            if (asset_type == AntCoin.Hash) throw new ArgumentException();
            Slice quantity;
            if (!db.TryGet(ReadOptions.Default, SliceBuilder.Begin(DataEntryPrefix.ST_QuantityIssued).Add(asset_type), out quantity))
                throw new ArgumentException();
            return quantity.ToInt64();
        }

        protected override void OnBlock(Block block)
        {
            WriteBatch batch = new WriteBatch();
            batch.Put(SliceBuilder.Begin(DataEntryPrefix.Block).Add(block.Hash), block.Trim());
            foreach (Transaction tx in block.Transactions)
            {
                batch.Put(SliceBuilder.Begin(DataEntryPrefix.Transaction).Add(tx.Hash), tx.ToArray());
                if (tx.Type == TransactionType.RegisterTransaction)
                {
                    RegisterTransaction reg_tx = (RegisterTransaction)tx;
                    batch.Put(SliceBuilder.Begin(DataEntryPrefix.IX_Register).Add((byte)reg_tx.RegisterType).Add(reg_tx.Hash), reg_tx.ToArray());
                }
                else if (tx.Type == TransactionType.IssueTransaction)
                {
                    //TODO: 统计发行量，并记录到数据库
                    //1. 从交易输出中找出本交易所涉及到的所有资产，并针对每一种类型的资产执行以下步骤：
                    //2. 对于存在负数输出的资产，必然是货币发行，发行量始终为0，所以不做记录；
                    //3. 对于其它种类的资产，对交易输出中的数量求和，得到本次的发行量；
                    //4. 将本次发行量增加到上一次的统计结果中，并写入数据库；
                }
                for (ushort index = 0; index < tx.Outputs.Length; index++)
                {
                    batch.Put(SliceBuilder.Begin(DataEntryPrefix.Unspent).Add(tx.Hash).Add(index), tx.Outputs[index].ToArray());
                }
            }
            foreach (TransactionInput input in block.Transactions.SelectMany(p => p.GetAllInputs()))
            {
                batch.Delete(SliceBuilder.Begin(DataEntryPrefix.Unspent).Add(input.PrevTxId).Add(input.PrevIndex));
            }
            db.Write(WriteOptions.Default, batch);
        }
    }
}
