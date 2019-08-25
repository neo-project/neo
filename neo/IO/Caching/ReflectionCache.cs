using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Neo.IO.Caching
{
    public class ReflectionCache<T> : Dictionary<T, Type>
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public ReflectionCache() { }
        /// <summary>
        /// Constructor
        /// </summary>
        /// <typeparam name="EnumType">Enum type</typeparam>
        public static ReflectionCache<T> CreateFromEnum<EnumType>() where EnumType : struct, IConvertible
        {
            Type enumType = typeof(EnumType);

            if (!enumType.GetTypeInfo().IsEnum)
                throw new ArgumentException("K must be an enumerated type");

            // Cache all types
            ReflectionCache<T> r = new ReflectionCache<T>();

            foreach (object t in Enum.GetValues(enumType))
            {
                // Get enumn member
                MemberInfo[] memInfo = enumType.GetMember(t.ToString());
                if (memInfo == null || memInfo.Length != 1)
                    throw (new FormatException());

                // Get attribute
                ReflectionCacheAttribute attribute = memInfo[0].GetCustomAttributes(typeof(ReflectionCacheAttribute), false)
                    .Cast<ReflectionCacheAttribute>()
                    .FirstOrDefault();

                if (attribute == null)
                    throw (new FormatException());

                // Append to cache
                r.Add((T)t, attribute.Type);
            }
            return r;
        }
        /// <summary>
        /// Create object from key
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="def">Default value</param>
        public object CreateInstance(T key, object def = null)
        {
            Type tp;

            // Get Type from cache
            if (TryGetValue(key, out tp)) return Activator.CreateInstance(tp);

            // return null
            return def;
        }
        /// <summary>
        /// Create object from key
        /// </summary>
        /// <typeparam name="K">Type</typeparam>
        /// <param name="key">Key</param>
        /// <param name="def">Default value</param>
        public K CreateInstance<K>(T key, K def = default(K))
        {
            Type tp;

            // Get Type from cache
            if (TryGetValue(key, out tp)) return (K)Activator.CreateInstance(tp);

            // return null
            return def;
        }
    }
}