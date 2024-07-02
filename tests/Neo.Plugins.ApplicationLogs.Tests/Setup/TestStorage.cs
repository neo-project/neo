// Copyright (C) 2015-2024 The Neo Project.
//
// StorageFixture.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Persistence;
using Neo.Plugins.ApplicationLogs.Store;
using Neo.Plugins.Storage;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Neo.Plugins.ApplicationsLogs.Tests.Setup
{
    public class TestStorage
    {
        private static readonly string s_dirPath = Path.GetRandomFileName();
        private static readonly RocksDBStore rocksDbStore = new RocksDBStore();
        public static readonly IStore Store = rocksDbStore.GetStore(s_dirPath);
    }
}
