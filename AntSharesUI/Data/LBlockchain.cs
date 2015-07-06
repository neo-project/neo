using AntShares.Core;
using AntShares.IO;
using AntShares.Properties;
using LevelDB;
using System;
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

        protected override void OnBlock(Block block)
        {
            WriteBatch batch = new WriteBatch();
            batch.Put(block.Hash.ToArray(), block.Trim());
            foreach (Transaction tx in block.Transactions)
            {
                batch.Put(tx.Hash.ToArray(), tx.ToArray());
                for (ushort index = 0; index < tx.Outputs.Length; index++)
                {
                    byte[] key = tx.Hash.ToArray().Concat(BitConverter.GetBytes(index)).ToArray();
                    batch.Put(key, tx.Outputs[index].ToArray());
                }
            }
            foreach (TransactionInput input in block.Transactions.SelectMany(p => p.GetAllInputs()))
            {
                batch.Delete(input.ToArray());
            }
            db.Write(WriteOptions.Default, batch);
        }
    }
}
