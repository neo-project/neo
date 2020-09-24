using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Neo.IO.Caching
{
    internal static class ReflectionCache<T> where T : Enum
    {
        private class Entry
        {
            public Type Type;
            public bool IsSerializable;
        }

        private static readonly Dictionary<T, Entry> dictionary = new Dictionary<T, Entry>();

        public static int Count => dictionary.Count;

        static ReflectionCache()
        {
            foreach (FieldInfo field in typeof(T).GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                // Get attribute
                ReflectionCacheAttribute attribute = field.GetCustomAttribute<ReflectionCacheAttribute>();
                if (attribute == null) continue;

                // Append to cache
                dictionary.Add((T)field.GetValue(null), new Entry()
                {
                    Type = attribute.Type,
                    IsSerializable = typeof(ISerializable).GetTypeInfo().IsAssignableFrom(attribute.Type),
                });
            }
        }

        public static object CreateInstance(T key, object def = null)
        {
            // Get Type from cache
            if (dictionary.TryGetValue(key, out var t))
                return Activator.CreateInstance(t.Type);

            // return null
            return def;
        }

        public static ISerializable CreateSerializable(T key, byte[] data)
        {
            if (dictionary.TryGetValue(key, out var t))
            {
                if (!t.IsSerializable)
                    throw new InvalidCastException();
                ISerializable serializable = (ISerializable)Activator.CreateInstance(t.Type);
                using (MemoryStream ms = new MemoryStream(data, false))
                using (BinaryReader reader = new BinaryReader(ms, Utility.StrictUTF8))
                {
                    serializable.Deserialize(reader);
                }
                return serializable;
            }
            return null;
        }
    }
}
