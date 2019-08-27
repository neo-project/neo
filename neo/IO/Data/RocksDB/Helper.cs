using RocksDbSharp;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Neo.IO.Data.RocksDB
{
    public static class Helper
    {
        public static void Delete(this WriteBatch batch, byte prefix, ISerializable key)
        {
            batch.Delete(SliceBuilder.Begin(prefix).Add(key));
        }

        public static IEnumerable<T> Find<T>(this DB db, ReadOptions options, byte prefix) where T : class, ISerializable, new()
        {
            return Find(db, options, SliceBuilder.Begin(prefix), (k, v) => v.ToArray().AsSerializable<T>());
        }

        public static IEnumerable<T> Find<T>(this DB db, ReadOptions options, Slice prefix, Func<Slice, Slice, T> resultSelector)
        {
            using (var it = db.NewIterator(options))
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

        public static T Get<T>(this DB db, ReadOptions options, byte prefix, ISerializable key) where T : class, ISerializable, new()
        {
            return db.Get(options, SliceBuilder.Begin(prefix).Add(key)).AsSerializable<T>();
        }

        public static T Get<T>(this DB db, ReadOptions options, byte prefix, ISerializable key, Func<Slice, T> resultSelector)
        {
            return resultSelector(db.Get(options, SliceBuilder.Begin(prefix).Add(key)));
        }

        public static void Put(this WriteBatch batch, byte prefix, ISerializable key, ISerializable value)
        {
            batch.Put(SliceBuilder.Begin(prefix).Add(key), value.ToArray());
        }

        public static T TryGet<T>(this DB db, ReadOptions options, byte prefix, ISerializable key) where T : class, ISerializable, new()
        {
            if (!db.TryGet(options, SliceBuilder.Begin(prefix).Add(key), out var value))
                return null;
            return value.AsSerializable<T>();
        }

        public static T TryGet<T>(this DB db, ReadOptions options, byte prefix, ISerializable key, Func<Slice, T> resultSelector) where T : class
        {
            if (!db.TryGet(options, SliceBuilder.Begin(prefix).Add(key), out var value))
                return null;
            return resultSelector(value);
        }
    }
}
