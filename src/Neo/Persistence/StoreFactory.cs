// Copyright (C) 2015-2024 The Neo Project.
//
// StoreFactory.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System.Collections.Generic;

namespace Neo.Persistence;

public static class StoreFactory
{
    private class MemoryStoreProvider : IStoreProvider
    {
        public string Name => nameof(MemoryStore);
        public IStore GetStore(string path) => new MemoryStore();
    }

    private static readonly Dictionary<string, IStoreProvider> providers = new();

    static StoreFactory()
    {
        var memProvider = new MemoryStoreProvider();
        RegisterProvider(memProvider);

        // Default cases

        providers.Add("", memProvider);
        providers.Add(null, memProvider);
    }

    public static void RegisterProvider(IStoreProvider provider)
    {
        providers.Add(provider.Name, provider);
    }

    /// <summary>
    /// Get store from name
    /// </summary>
    /// <param name="storageEngine">The storage engine used to create the <see cref="IStore"/> objects. If this parameter is <see langword="null"/>, a default in-memory storage engine will be used.</param>
    /// <param name="path">The path of the storage. If <paramref name="storageEngine"/> is the default in-memory storage engine, this parameter is ignored.</param>
    /// <returns>The storage engine.</returns>
    public static IStore GetStore(string storageEngine, string path)
    {
        return providers[storageEngine].GetStore(path);
    }
}
