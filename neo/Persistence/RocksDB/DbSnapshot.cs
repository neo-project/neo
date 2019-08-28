using Neo.IO.Caching;
using Neo.IO.Data.RocksDB;
using Neo.IO.Wrappers;
using Neo.Ledger;
using RocksDbSharp;

namespace Neo.Persistence.RocksDB
{
    internal class DbSnapshot : Snapshot
    {
        private readonly RocksDBCore db;
        private readonly RocksDbSharp.Snapshot snapshot;
        private readonly WriteBatch batch;

        public override DataCache<UInt256, TrimmedBlock> Blocks { get; }
        public override DataCache<UInt256, TransactionState> Transactions { get; }
        public override DataCache<UInt160, ContractState> Contracts { get; }
        public override DataCache<StorageKey, StorageItem> Storages { get; }
        public override DataCache<UInt32Wrapper, HeaderHashList> HeaderHashList { get; }
        public override MetaDataCache<HashIndexState> BlockHashIndex { get; }
        public override MetaDataCache<HashIndexState> HeaderHashIndex { get; }

        public DbSnapshot(RocksDBCore db)
        {
            this.db = db;
            this.snapshot = db.GetSnapshot();
            this.batch = new WriteBatch();

            var options = new ReadOptions();
            options.SetFillCache(false);
            options.SetSnapshot(snapshot);

            Blocks = new DbCache<UInt256, TrimmedBlock>(db, db.DATA_Block, options, batch);
            Transactions = new DbCache<UInt256, TransactionState>(db, db.DATA_Transaction, options, batch);
            Contracts = new DbCache<UInt160, ContractState>(db, db.ST_Contract, options, batch);
            Storages = new DbCache<StorageKey, StorageItem>(db, db.ST_Storage, options, batch);
            HeaderHashList = new DbCache<UInt32Wrapper, HeaderHashList>(db, db.IX_HeaderHashList, options, batch);
            BlockHashIndex = new DbMetaDataCache<HashIndexState>(db, db.IX_CurrentBlock, options, batch);
            HeaderHashIndex = new DbMetaDataCache<HashIndexState>(db, db.IX_CurrentHeader, options, batch);
        }

        public override void Commit()
        {
            base.Commit();
            db.Write(RocksDBCore.WriteDefault, batch);
        }

        public override void Dispose()
        {
            snapshot.Dispose();
            batch.Dispose();
        }
    }
}
