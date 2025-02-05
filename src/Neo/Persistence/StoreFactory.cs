// Copyright (C) 2015-2025 The Neo Project.
//
// StoreFactory.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the repository
// or https://opensource.org/license/mit for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System.Collections.Generic;

namespace Neo.Persistence
{
    public static class StoreFactory
    {
        private static readonly Dictionary<string, IStoreProvider> providers = new();

        static StoreFactory()
        {
            var memProvider = new MemoryStoreProvider();
            RegisterProvider(memProvider);

            // Default cases
            providers.Add("", memProvider);
        }

        public static void RegisterProvider(IStoreProvider provider)
        {
            providers.Add(provider.Name, provider);
        }

        /// <summary>
        /// Get store provider by name
        /// </summary>
        /// <param name="name">Name</param>
        /// <returns>Store provider</returns>
        public static IStoreProvider GetStoreProvider(string name)
        {
            if (providers.TryGetValue(name, out var provider))
            {
                return provider;
            }

            return null;
        }

        /// <summary>
        /// Get store from name
        /// </summary>
        /// <param name="storageProvider">The storage engine used to create the <see cref="IStore"/> objects. If this parameter is <see langword="null"/>, a default in-memory storage engine will be used.</param>
        /// <param name="path">The path of the storage. If <paramref name="storageProvider"/> is the default in-memory storage engine, this parameter is ignored.</param>
        /// <returns>The storage engine.</returns>
        public static IStore GetStore(string storageProvider, string path)
        {
            return providers[storageProvider].GetStore(path);
        }
    }
}
