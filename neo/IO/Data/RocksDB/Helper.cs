using RocksDbSharp;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Neo.IO.Data.RocksDB
{
    public static class Helper
    {
        public static void Delete(this WriteBatch batch, ColumnFamilyHandle family, ISerializable key)
        {
            batch.Delete(key.ToArray(), family);
        }

        public static IEnumerable<T> Find<T>(this RocksDBCore db, ColumnFamilyHandle family, ReadOptions options) where T : class, ISerializable, new()
        {
            return Find(db, family, options, new byte[0], (k, v) => v.ToArray().AsSerializable<T>());
        }

        public static IEnumerable<T> Find<T>(this RocksDBCore db, ColumnFamilyHandle family, ReadOptions options, byte[] prefix, Func<byte[], byte[], T> resultSelector)
        {
            using (var it = db.NewIterator(family, options))
            {
                for (it.Seek(prefix); it.Valid(); it.Next())
                {
                    var key = it.Key();
                    byte[] y = prefix.ToArray();
                    if (key.Length < y.Length) break;
                    if (!key.Take(y.Length).SequenceEqual(y)) break;
                    yield return resultSelector(key, it.Value());
                }
            }
        }

        public static T Get<T>(this RocksDBCore db, ColumnFamilyHandle family, ReadOptions options, ISerializable key) where T : class, ISerializable, new()
        {
            return db.Get(family, options, key.ToArray()).AsSerializable<T>();
        }

        public static T Get<T>(this RocksDBCore db, ColumnFamilyHandle family, ReadOptions options, ISerializable key, Func<byte[], T> resultSelector)
        {
            return resultSelector(db.Get(family, options, key.ToArray()));
        }

        public static void Put(this WriteBatch batch, ColumnFamilyHandle family, ISerializable key, ISerializable value)
        {
            batch.Put(key.ToArray(), value.ToArray(), family);
        }

        public static T TryGet<T>(this RocksDBCore db, ColumnFamilyHandle family, ReadOptions options, ISerializable key) where T : class, ISerializable, new()
        {
            if (!db.TryGet(family, options, key.ToArray(), out var value))
                return null;
            return value.AsSerializable<T>();
        }

        public static T TryGet<T>(this RocksDBCore db, ColumnFamilyHandle family, ReadOptions options, ISerializable key, Func<byte[], T> resultSelector) where T : class
        {
            if (!db.TryGet(family, options, key.ToArray(), out var value))
                return null;
            return resultSelector(value);
        }
    }
}
