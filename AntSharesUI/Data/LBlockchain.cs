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
            if (!db.TryGet(ReadOptions.Default, "initialized", out initialized) || !initialized.ToBoolean())
            {
                OnBlock(Blockchain.GenesisBlock);
                db.Put(WriteOptions.Default, "initialized", true);
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
                for (it.Seek((byte)DataEntryPrefix.IX_Register); it.Valid() && it.Key() < (byte)DataEntryPrefix.IX_Register + 1; it.Next())
                {
                    yield return it.Value().ToArray().AsSerializable<RegisterTransaction>();
                }
            }
        }

        protected override void OnBlock(Block block)
        {
            WriteBatch batch = new WriteBatch();
            batch.Put(block.Key(), block.Trim());
            foreach (Transaction tx in block.Transactions)
            {
                batch.Put(tx.Key(), tx.ToArray());
                if (tx.Type == TransactionType.RegisterTransaction)
                {
                    RegisterTransaction reg_tx = (RegisterTransaction)tx;
                    batch.Put(reg_tx.IndexKey(), reg_tx.ToArray());
                }
                for (ushort index = 0; index < tx.Outputs.Length; index++)
                {
                    batch.Put(tx.UnspentKey(index), tx.Outputs[index].ToArray());
                }
            }
            foreach (TransactionInput input in block.Transactions.SelectMany(p => p.GetAllInputs()))
            {
                batch.Delete(input.UnspentKey());
            }
            db.Write(WriteOptions.Default, batch);
        }
    }
}
