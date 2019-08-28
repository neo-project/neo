using Neo.IO.Caching;
using Neo.IO.Data.RocksDB;
using Neo.IO.Wrappers;
using Neo.Ledger;
using RocksDbSharp;
using System;
using System.Reflection;
using System.Text;

namespace Neo.Persistence.RocksDB
{
    public class RocksDBStore : Store, IDisposable
    {
        private readonly RocksDBCore db;

        public RocksDBStore(string path)
        {
            db = RocksDBCore.Open(new Options { CreateIfMissing = true, FilePath = path });

            if (db.TryGet(db.SYS_Version, RocksDBCore.ReadDefault, new byte[0], out var value) &&
                Version.TryParse(Encoding.UTF8.GetString(value), out var version) && version >= Version.Parse("2.9.1"))
                return;

            using (var batch = new WriteBatch())
            {
                var options = new ReadOptions();
                options.SetFillCache(false);

                // Clean entries

                using (var it = db.NewIterator(db.SYS_Version, options))
                {
                    for (it.SeekToFirst(); it.Valid(); it.Next())
                    {
                        batch.Delete(it.Key());
                    }
                }

                // Update version

                db.Put(db.SYS_Version, RocksDBCore.WriteDefault, new byte[0], Encoding.UTF8.GetBytes(Assembly.GetExecutingAssembly().GetName().Version.ToString()));
                db.Write(RocksDBCore.WriteDefault, batch);
            }
        }

        public override void Dispose()
        {
            db.Dispose();
        }

        public override DataCache<UInt256, TrimmedBlock> GetBlocks()
        {
            return new DbCache<UInt256, TrimmedBlock>(db, db.DATA_Block);
        }

        public override DataCache<UInt160, ContractState> GetContracts()
        {
            return new DbCache<UInt160, ContractState>(db, db.ST_Contract);
        }

        public override Snapshot GetSnapshot()
        {
            return new DbSnapshot(db);
        }

        public override DataCache<StorageKey, StorageItem> GetStorages()
        {
            return new DbCache<StorageKey, StorageItem>(db, db.ST_Storage);
        }

        public override DataCache<UInt256, TransactionState> GetTransactions()
        {
            return new DbCache<UInt256, TransactionState>(db, db.DATA_Transaction);
        }

        public override DataCache<UInt32Wrapper, HeaderHashList> GetHeaderHashList()
        {
            return new DbCache<UInt32Wrapper, HeaderHashList>(db, db.IX_HeaderHashList);
        }

        public override MetaDataCache<HashIndexState> GetBlockHashIndex()
        {
            return new DbMetaDataCache<HashIndexState>(db, db.IX_CurrentBlock);
        }

        public override MetaDataCache<HashIndexState> GetHeaderHashIndex()
        {
            return new DbMetaDataCache<HashIndexState>(db, db.IX_CurrentHeader);
        }

        public override byte[] Get(byte[] key)
        {
            if (!db.TryGet(db.DefaultFamily, RocksDBCore.ReadDefault, key, out var value))
                return null;
            return value;
        }

        public override void Put(byte[] key, byte[] value)
        {
            db.Put(db.DefaultFamily, RocksDBCore.WriteDefault, key, value);
        }

        public override void PutSync(byte[] key, byte[] value)
        {
            db.Put(db.DefaultFamily, RocksDBCore.WriteDefaultSync, key, value);
        }
    }
}
