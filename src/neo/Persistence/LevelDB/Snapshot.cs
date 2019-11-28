using Neo.IO.Data.LevelDB;
using System.Collections.Generic;
using System.Linq;
using LSnapshot = Neo.IO.Data.LevelDB.Snapshot;

namespace Neo.Persistence.LevelDB
{
    internal class Snapshot : ISnapshot
    {
        private readonly DB db;
        private readonly LSnapshot snapshot;
        private readonly ReadOptions options;
        private readonly WriteBatch batch;

        public Snapshot(DB db)
        {
            this.db = db;
            this.snapshot = db.GetSnapshot();
            this.options = new ReadOptions { FillCache = false, Snapshot = snapshot };
            this.batch = new WriteBatch();
        }

        public void Commit()
        {
            db.Write(WriteOptions.Default, batch);
        }

        public void Delete(byte table, byte[] key)
        {
            batch.Delete(SliceBuilder.Begin(table).Add(key));
        }

        public void Dispose()
        {
            snapshot.Dispose();
        }

        public IEnumerable<(byte[] Key, byte[] Value)> Find(byte table, byte[] prefix)
        {
            return db.Find(options, SliceBuilder.Begin(table).Add(prefix), (k, v) => (k.ToArray().Skip(1).ToArray(), v.ToArray()));
        }

        public void Put(byte table, byte[] key, byte[] value)
        {
            batch.Put(SliceBuilder.Begin(table).Add(key), value);
        }

        public byte[] TryGet(byte table, byte[] key)
        {
            if (!db.TryGet(options, SliceBuilder.Begin(table).Add(key), out Slice slice))
                return null;
            return slice.ToArray();
        }
    }
}
