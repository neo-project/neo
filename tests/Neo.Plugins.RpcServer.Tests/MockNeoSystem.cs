// Copyright (C) 2015-2024 The Neo Project.
//
// MockNeoSystem.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Ledger;
using Neo.Persistence;

namespace Neo.Plugins.RpcServer.Tests
{
    public class MockNeoSystem : NeoSystem
    {
        public SnapshotCache SnapshotCache { get; }
        public MemoryPool MemoryPool { get; }

        public MockNeoSystem(SnapshotCache snapshotCache, MemoryPool memoryPool)
            : base(TestProtocolSettings.Default, new TestBlockchain.StoreProvider())
        {
            SnapshotCache = snapshotCache;
            MemoryPool = memoryPool;
        }

        public override SnapshotCache GetSnapshot()
        {
            return SnapshotCache;
        }
    }
}
