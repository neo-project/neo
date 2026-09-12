// Copyright (C) 2015-2026 The Neo Project.
//
// UT_TransactionRouter_Records.cs file belongs to the neo project and is free
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
    public class UT_TransactionRouter_Records
    {
        private static Transaction SampleTx() => new()
        {
            Version = 0,
            Nonce = 1,
            SystemFee = 0,
            NetworkFee = 0,
            ValidUntilBlock = 10,
            Attributes = [],
            Signers = [new Signer { Account = UInt160.Zero, Scopes = WitnessScope.None }],
            Script = new byte[] { 0x40 },
            Witnesses = []
        };

        [TestMethod]
        public void Preverify_Record_HoldsFields()
        {
            var tx = SampleTx();
            var msg = new TransactionRouter.Preverify(tx, Relay: true);
            Assert.AreSame(tx, msg.Transaction);
            Assert.IsTrue(msg.Relay);
        }

        [TestMethod]
        public void PreverifyCompleted_Record_HoldsFields()
        {
            var tx = SampleTx();
            var msg = new TransactionRouter.PreverifyCompleted(tx, Relay: false, VerifyResult.Succeed);
            Assert.AreSame(tx, msg.Transaction);
            Assert.IsFalse(msg.Relay);
            Assert.AreEqual(VerifyResult.Succeed, msg.Result);
        }
    }
}
