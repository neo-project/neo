using Neo.IO.Caching;
using Neo.IO.Data.RocksDB;
using Neo.IO.Wrappers;
using Neo.Ledger;
using RocksDbSharp;

namespace Neo.Persistence.RocksDB
{
    internal class DbSnapshot : Snapshot
    {
        private readonly DB db;
        private readonly RocksDbSharp.Snapshot snapshot;
        private readonly WriteBatch batch;

        public override DataCache<UInt256, TrimmedBlock> Blocks { get; }
        public override DataCache<UInt256, TransactionState> Transactions { get; }
        public override DataCache<UInt160, ContractState> Contracts { get; }
        public override DataCache<StorageKey, StorageItem> Storages { get; }
        public override DataCache<UInt32Wrapper, HeaderHashList> HeaderHashList { get; }
        public override MetaDataCache<HashIndexState> BlockHashIndex { get; }
        public override MetaDataCache<HashIndexState> HeaderHashIndex { get; }

        public DbSnapshot(DB db)
        {
            this.db = db;
            this.snapshot = db.GetSnapshot();
            this.batch = new WriteBatch();

            var options = new ReadOptions();
            options.SetFillCache(false);
            options.SetSnapshot(snapshot);

            Blocks = new DbCache<UInt256, TrimmedBlock>(db, options, batch, db.DATA_Block);
            Transactions = new DbCache<UInt256, TransactionState>(db, options, batch, db.DATA_Transaction);
            Contracts = new DbCache<UInt160, ContractState>(db, options, batch, db.ST_Contract);
            Storages = new DbCache<StorageKey, StorageItem>(db, options, batch, db.ST_Storage);
            HeaderHashList = new DbCache<UInt32Wrapper, HeaderHashList>(db, options, batch, db.IX_HeaderHashList);
            BlockHashIndex = new DbMetaDataCache<HashIndexState>(db, options, batch, db.IX_CurrentBlock);
            HeaderHashIndex = new DbMetaDataCache<HashIndexState>(db, options, batch, db.IX_CurrentHeader);
        }

        public override void Commit()
        {
            base.Commit();
            db.Write(DB.WriteDefault, batch);
        }

        public override void Dispose()
        {
            snapshot.Dispose();
            batch.Dispose();
        }
    }
}
