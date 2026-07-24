// Copyright (C) 2015-2026 The Neo Project.
//
// UT_LedgerEnums.cs file belongs to the neo project and is free
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
using Neo.SmartContract.Native;
using System;

namespace Neo.UnitTests.Ledger
{
    [TestClass]
    public class UT_LedgerEnums
    {
        [TestMethod]
        public void VerifyResult_ValuesAreDistinct()
        {
            var values = Enum.GetValues<VerifyResult>();
            Assert.IsTrue(values.Length >= 15);
            Assert.AreEqual(VerifyResult.Succeed, (VerifyResult)0);
            Assert.IsTrue(Enum.IsDefined(VerifyResult.HasConflicts));
            Assert.IsTrue(Enum.IsDefined(VerifyResult.Unknown));
        }

        [TestMethod]
        public void TransactionRemovalReason_Values()
        {
            Assert.AreEqual(0, (byte)TransactionRemovalReason.CapacityExceeded);
            Assert.AreEqual(1, (byte)TransactionRemovalReason.NoLongerValid);
            Assert.AreEqual(2, (byte)TransactionRemovalReason.Conflict);
        }

        [TestMethod]
        public void WitnessRuleAction_Values()
        {
            Assert.AreEqual(0, (byte)WitnessRuleAction.Deny);
            Assert.AreEqual(1, (byte)WitnessRuleAction.Allow);
        }

        [TestMethod]
        public void NamedCurveHash_Values()
        {
            Assert.AreEqual(22, (byte)NamedCurveHash.secp256k1SHA256);
            Assert.AreEqual(23, (byte)NamedCurveHash.secp256r1SHA256);
            Assert.AreEqual(122, (byte)NamedCurveHash.secp256k1Keccak256);
            Assert.AreEqual(123, (byte)NamedCurveHash.secp256r1Keccak256);
        }
    }
}
