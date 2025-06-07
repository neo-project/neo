// Copyright (C) 2015-2025 The Neo Project.
//
// FasterDbStoreProvider.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Build.Core.Storage;
using Neo.Persistence;

namespace Neo.Build.Core.Providers.Storage
{
    internal class FasterDbStoreProvider : IStoreProvider
    {
        public string Name => nameof(FasterDbStore);

        public IStore GetStore(string path) => new FasterDbStore(path);
    }
}
