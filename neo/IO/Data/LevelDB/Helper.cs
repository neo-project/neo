using LevelDB;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Neo.IO.Data.LevelDB
{
    public static class Helper
    {
        public static void Delete(this WriteBatch batch, byte prefix, ISerializable key)
        {
            batch.Delete(((Slice)SliceBuilder.Begin(prefix).Add(key)).ToArray());
        }

        public static IEnumerable<T> Find<T>(this DB db, ReadOptions options, byte prefix) where T : class, ISerializable, new()
        {
            return Find(db, options, SliceBuilder.Begin(prefix), (k, v) => v.ToArray().AsSerializable<T>());
        }

        public static IEnumerable<T> Find<T>(this DB db, ReadOptions options, Slice prefix, Func<Slice, Slice, T> resultSelector)
        {
            using (Iterator it = db.CreateIterator(options))
            {
                for (it.Seek(prefix.ToArray()); it.IsValid(); it.Next())
                {
                    Slice key = it.Key();
                    byte[] x = key.ToArray();
                    byte[] y = prefix.ToArray();
                    if (x.Length < y.Length) break;
                    if (!x.Take(y.Length).SequenceEqual(y)) break;
                    yield return resultSelector(key, it.Value());
                }
            }
        }

        public static T Get<T>(this DB db, ReadOptions options, byte prefix, ISerializable key) where T : class, ISerializable, new()
        {
            var value = db.Get(SliceBuilder.Begin(prefix).Add(key).ToArray(), options);
            if (value == null) throw new LevelDBException("not found");
            return value.AsSerializable<T>();
        }

        public static T Get<T>(this DB db, ReadOptions options, byte prefix, ISerializable key, Func<Slice, T> resultSelector)
        {
            var value = db.Get(SliceBuilder.Begin(prefix).Add(key).ToArray(), options);
            if (value == null) throw new LevelDBException("not found");
            return resultSelector(value);
        }

        public static void Put(this WriteBatch batch, byte prefix, ISerializable key, ISerializable value)
        {
            batch.Put(SliceBuilder.Begin(prefix).Add(key).ToArray(), value.ToArray());
        }

        public static T TryGet<T>(this DB db, ReadOptions options, byte prefix, ISerializable key) where T : class, ISerializable, new()
        {
            var value = db.Get(SliceBuilder.Begin(prefix).Add(key).ToArray(), options);
            return value?.AsSerializable<T>();
        }

        public static T TryGet<T>(this DB db, ReadOptions options, byte prefix, ISerializable key, Func<Slice, T> resultSelector) where T : class
        {
            var value = db.Get(SliceBuilder.Begin(prefix).Add(key).ToArray(), options);
            return value == null ? null : resultSelector(value);
        }
    }
}
