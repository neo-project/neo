using Neo.IO.Data.LevelDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Neo.Persistence.LevelDB
{
    public class Store : IStore
    {
        private const byte SYS_Version = 0xf0;
        private readonly DB db;

        public Store(string path)
        {
            this.db = DB.Open(path, new Options { CreateIfMissing = true });
            if (db.TryGet(ReadOptions.Default, SliceBuilder.Begin(SYS_Version), out Slice value) && Version.TryParse(value.ToString(), out Version version) && version >= Version.Parse("2.9.1"))
                return;
            WriteBatch batch = new WriteBatch();
            ReadOptions options = new ReadOptions { FillCache = false };
            using (Iterator it = db.NewIterator(options))
            {
                for (it.SeekToFirst(); it.Valid(); it.Next())
                {
                    batch.Delete(it.Key());
                }
            }
            db.Put(WriteOptions.Default, SliceBuilder.Begin(SYS_Version), Assembly.GetExecutingAssembly().GetName().Version.ToString());
            db.Write(WriteOptions.Default, batch);
        }

        public void Delete(byte table, byte[] key)
        {
            db.Delete(WriteOptions.Default, SliceBuilder.Begin(table).Add(key));
        }

        public void Dispose()
        {
            db.Dispose();
        }

        public IEnumerable<(byte[], byte[])> Find(byte table, byte[] prefix)
        {
            return db.Find(ReadOptions.Default, SliceBuilder.Begin(table).Add(prefix), (k, v) => (k.ToArray().Skip(1).ToArray(), v.ToArray()));
        }

        public ISnapshot GetSnapshot()
        {
            return new Snapshot(db);
        }

        public void Put(byte table, byte[] key, byte[] value)
        {
            db.Put(WriteOptions.Default, SliceBuilder.Begin(table).Add(key), value);
        }

        public void PutSync(byte table, byte[] key, byte[] value)
        {
            db.Put(new WriteOptions { Sync = true }, SliceBuilder.Begin(table).Add(key), value);
        }

        public byte[] TryGet(byte table, byte[] key)
        {
            if (!db.TryGet(ReadOptions.Default, SliceBuilder.Begin(table).Add(key), out Slice slice))
                return null;
            return slice.ToArray();
        }
    }
}
