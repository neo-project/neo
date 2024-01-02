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
        RegisterProvider(new MemoryStoreProvider());
    }

    public static void RegisterProvider(IStoreProvider provider)
    {
        providers.Add(provider.Name, provider);
    }

    public static IStore GetStore(string storageEngine, string path)
    {
        return providers[storageEngine].GetStore(path);
    }
}
