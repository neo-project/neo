// Copyright (C) 2015-2025 The Neo Project.
//
// Options.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the repository
// or https://opensource.org/license/mit for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using RocksDbSharp;

namespace Neo.Plugins.Storage
{
    public static class Options
    {
        public static readonly DbOptions Default = CreateDbOptions();
        public static readonly ReadOptions ReadDefault = new();
        public static readonly WriteOptions WriteDefault = new();
        public static readonly WriteOptions WriteDefaultSync = new WriteOptions().SetSync(true);

        public static DbOptions CreateDbOptions()
        {
            var options = new DbOptions();
            options.SetCreateMissingColumnFamilies(true);
            options.SetCreateIfMissing(true);
            options.SetErrorIfExists(false);
            options.SetMaxOpenFiles(1000);
            options.SetParanoidChecks(false);
            options.SetWriteBufferSize(4 << 20);
            options.SetBlockBasedTableFactory(new BlockBasedTableOptions().SetBlockSize(4096));
            return options;
        }
    }
}
