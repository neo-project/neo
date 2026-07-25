// Copyright (C) 2015-2026 The Neo Project.
//
// UT_NewTransactionEventArgs.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.Persistence.Providers;

namespace Neo.UnitTests.Ledger
{
    [TestClass]
    public class UT_NewTransactionEventArgs
    {
        [TestMethod]
        public void Properties_And_Cancel()
        {
            var tx = new Transaction
            {
                Version = 0,
                Nonce = 2,
                SystemFee = 0,
                NetworkFee = 0,
                ValidUntilBlock = 10,
                Attributes = [],
                Signers = [new Signer { Account = UInt160.Zero, Scopes = WitnessScope.None }],
                Script = new byte[] { 0x41 },
                Witnesses = []
            };

            using var store = new MemoryStore();
            using var cache = new StoreCache(store);
            var args = new NewTransactionEventArgs
            {
                Transaction = tx,
                Snapshot = cache
            };

            Assert.AreSame(tx, args.Transaction);
            Assert.AreSame(cache, args.Snapshot);
            Assert.IsFalse(args.Cancel);

            args.Cancel = true;
            Assert.IsTrue(args.Cancel);
        }
    }
}
