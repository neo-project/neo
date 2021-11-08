// Copyright (C) 2015-2021 The Neo Project.
// 
// The neo is free software distributed under the MIT software license, 
// see the accompanying file LICENSE in the main directory of the
// project or http://www.opensource.org/licenses/mit-license.php 
// for more details.
// 
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Collections.Generic;
using System.Reflection;

namespace Neo.IO.Caching
{
    internal static class ReflectionCache<T> where T : Enum
    {
        private static readonly Dictionary<T, Type> dictionary = new();

        public static int Count => dictionary.Count;

        static ReflectionCache()
        {
            foreach (FieldInfo field in typeof(T).GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                // Get attribute
                ReflectionCacheAttribute attribute = field.GetCustomAttribute<ReflectionCacheAttribute>();
                if (attribute == null) continue;

                // Append to cache
                dictionary.Add((T)field.GetValue(null), attribute.Type);
            }
        }

        public static object CreateInstance(T key, object def = null)
        {
            // Get Type from cache
            if (dictionary.TryGetValue(key, out Type t))
                return Activator.CreateInstance(t);

            // return null
            return def;
        }

        public static ISerializable CreateSerializable(T key, byte[] data)
        {
            if (dictionary.TryGetValue(key, out Type t))
                return data.AsSerializable(t);
            return null;
        }
    }
}
