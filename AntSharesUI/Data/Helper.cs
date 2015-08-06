using LevelDB;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AntShares.Data
{
    internal static class Helper
    {
        public static IEnumerable<T> Find<T>(this DB db, ReadOptions options, Slice prefix, Func<Slice, Slice, T> resultSelector)
        {
            using (Iterator it = db.NewIterator(options))
            {
                for (it.Seek(prefix); it.Valid(); it.Next())
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

        public static ushort[] GetUInt16Array(this byte[] source)
        {
            if (source == null) throw new ArgumentNullException();
            int rem;
            int size = Math.DivRem(source.Length, sizeof(ushort), out rem);
            if (rem != 0) throw new ArgumentException();
            ushort[] dst = new ushort[size];
            Buffer.BlockCopy(source, 0, dst, 0, source.Length);
            return dst;
        }

        public static IEnumerable<TResult> IndexedSelect<T, TResult>(this IEnumerable<T> source, Func<T, int, TResult> selector)
        {
            int index = 0;
            foreach (T item in source)
            {
                yield return selector(item, index++);
            }
        }

        public static IEnumerable<ushort> Range(ushort start, int count)
        {
            if (count < 0 || start + count > ushort.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(count));
            for (ushort i = 0; i < count; i++)
                yield return (ushort)(i + start);
        }

        public static byte[] ToByteArray(this IEnumerable<ushort> source)
        {
            ushort[] src = source.ToArray();
            byte[] dst = new byte[src.Length * sizeof(ushort)];
            Buffer.BlockCopy(src, 0, dst, 0, dst.Length);
            return dst;
        }
    }
}
