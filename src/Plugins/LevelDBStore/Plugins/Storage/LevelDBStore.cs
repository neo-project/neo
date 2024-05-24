// Copyright (C) 2015-2024 The Neo Project.
//
// LevelDBStore.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using LevelDB;
using Neo.Persistence;
using System;
using System.Linq;

namespace Neo.Plugins.Storage
{
    public class LevelDBStore : Plugin, IStoreProvider
    {
        public override string Description => "Uses LevelDB to store the blockchain data";

        public LevelDBStore()
        {
            StoreFactory.RegisterProvider(this);
        }

        public IStore GetStore(string path)
        {
            if (Environment.CommandLine.Split(' ').Any(p => p == "/repair" || p == "--repair"))
                DB.Repair(Options.Default, path);
            return new Store(path);
        }
    }
}
