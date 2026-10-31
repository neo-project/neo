// Copyright (C) 2015-2026 The Neo Project.
//
// UT_TransactionRemovedEventArgs.cs file belongs to the neo project and is free
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

namespace Neo.UnitTests.Ledger
{
    [TestClass]
    public class UT_TransactionRemovedEventArgs
    {
        [TestMethod]
        public void Properties_RoundTrip()
        {
            var tx = new Transaction
            {
                Version = 0,
                Nonce = 1,
                SystemFee = 0,
                NetworkFee = 0,
                ValidUntilBlock = 100,
                Attributes = [],
                Signers = [new Signer { Account = UInt160.Zero, Scopes = WitnessScope.None }],
                Script = new byte[] { 0x40 },
                Witnesses = []
            };

            var args = new TransactionRemovedEventArgs
            {
                Transactions = [tx],
                Reason = TransactionRemovalReason.CapacityExceeded
            };

            Assert.AreEqual(1, args.Transactions.Count);
            Assert.AreSame(tx, System.Linq.Enumerable.First(args.Transactions));
            Assert.AreEqual(TransactionRemovalReason.CapacityExceeded, args.Reason);
        }
    }
}
