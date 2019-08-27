using RocksDbSharp;
using System;

namespace Neo.IO.Data.RocksDB
{
    public class DB : IDisposable
    {
        public static readonly Options OptionsDefault = new Options();
        public static readonly ReadOptions ReadDefault = new ReadOptions();
        public static readonly WriteOptions WriteDefault = new WriteOptions();
        public static readonly WriteOptions WriteDefaultSync = new WriteOptions();

        static DB()
        {
            WriteDefaultSync.SetSync(true);
        }

        private readonly RocksDb _rocksDb;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="db">Database</param>
        private DB(RocksDb db)
        {
            _rocksDb = db;
        }

        /// <summary>
        /// Open database
        /// </summary>
        /// <returns>DB</returns>
        public static DB Open()
        {
            return Open(OptionsDefault);
        }

        /// <summary>
        /// Open database
        /// </summary>
        /// <param name="config">Configuration</param>
        /// <returns>DB</returns>
        public static DB Open(Options config)
        {
            return new DB(RocksDb.Open(config.Build(), config.FilePath));
        }

        /// <summary>
        /// Free resources
        /// </summary>
        public void Dispose()
        {
            _rocksDb?.Dispose();
        }

        public void Delete(WriteOptions options, Slice key)
        {
            _rocksDb.Remove(key.ToArray(), null, options);
        }

        public byte[] Get(ReadOptions options, Slice key)
        {
            var value = _rocksDb.Get(key.ToArray(), null, options);

            if (value == null)
                throw new RocksDbSharpException("not found");

            return value;
        }

        public bool TryGet(ReadOptions options, Slice key, out byte[] value)
        {
            value = _rocksDb.Get(key.ToArray(), null, options);
            return value != null;
        }

        public void Put(WriteOptions options, Slice key, byte[] value)
        {
            _rocksDb.Put(key.ToArray(), value, null, options);
        }

        public Snapshot GetSnapshot()
        {
            return _rocksDb.CreateSnapshot();
        }

        public Iterator NewIterator(ReadOptions options)
        {
            return _rocksDb.NewIterator(null, options);
        }

        public void Write(WriteOptions options, WriteBatch batch)
        {
            _rocksDb.Write(batch, options);
        }
    }
}
