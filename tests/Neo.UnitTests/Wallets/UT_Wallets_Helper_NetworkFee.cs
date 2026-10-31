// Copyright (C) 2015-2026 The Neo Project.
//
// UT_Wallets_Helper_NetworkFee.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Cryptography.ECC;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.VM;
using Neo.Wallets;
using System;
// Witness is in Neo.Network.P2P.Payloads

namespace Neo.UnitTests.Wallets
{
    [TestClass]
    public class UT_Wallets_Helper_NetworkFee
    {
        private DataCache _snapshot;

        [TestInitialize]
        public void Setup()
        {
            _snapshot = TestBlockchain.GetTestSnapshotCache().CloneCache();
        }

        private static Transaction MakeTx(UInt160 account, byte[] script = null)
        {
            return new Transaction
            {
                Version = 0,
                Nonce = 1,
                SystemFee = 0,
                NetworkFee = 0,
                ValidUntilBlock = 100,
                Attributes = [],
                Signers = [new Signer { Account = account, Scopes = WitnessScope.CalledByEntry }],
                Script = script ?? [(byte)OpCode.RET],
                Witnesses = []
            };
        }

        [TestMethod]
        public void CalculateNetworkFee_SignatureContract_Positive()
        {
            var key = new KeyPair(new byte[32]
            {
                0x01,0x02,0x03,0x04,0x05,0x06,0x07,0x08,
                0x09,0x0a,0x0b,0x0c,0x0d,0x0e,0x0f,0x10,
                0x11,0x12,0x13,0x14,0x15,0x16,0x17,0x18,
                0x19,0x1a,0x1b,0x1c,0x1d,0x1e,0x1f,0x20
            });
            var account = Contract.CreateSignatureRedeemScript(key.PublicKey).ToScriptHash();
            var verification = Contract.CreateSignatureRedeemScript(key.PublicKey);
            var tx = MakeTx(account);

            var fee = tx.CalculateNetworkFee(
                _snapshot,
                TestProtocolSettings.Default,
                hash => hash.Equals(account) ? verification : null);

            Assert.IsTrue(fee > 0);
        }

        [TestMethod]
        public void CalculateNetworkFee_MultiSig_Positive()
        {
            ECPoint[] keys =
            [
                TestProtocolSettings.Default.StandbyCommittee[0],
                TestProtocolSettings.Default.StandbyCommittee[1],
                TestProtocolSettings.Default.StandbyCommittee[2]
            ];
            var script = Contract.CreateMultiSigRedeemScript(2, keys);
            var account = script.ToScriptHash();
            var tx = MakeTx(account);

            var fee = tx.CalculateNetworkFee(
                _snapshot,
                TestProtocolSettings.Default,
                hash => hash.Equals(account) ? script : null);

            Assert.IsTrue(fee > 0);
        }

        [TestMethod]
        public void CalculateNetworkFee_MissingAccount_Throws()
        {
            var account = UInt160.Parse("0x0101010101010101010101010101010101010101");
            var tx = MakeTx(account);
            // Provide a witness slot so fee calc reaches the missing-contract path
            // instead of indexing an empty Witnesses array.
            tx.Witnesses = [new Witness { InvocationScript = Array.Empty<byte>(), VerificationScript = Array.Empty<byte>() }];

            Assert.ThrowsExactly<ArgumentException>(() =>
                tx.CalculateNetworkFee(_snapshot, TestProtocolSettings.Default, _ => null));
        }

        [TestMethod]
        public void Sign_ProducesNonEmptySignature()
        {
            var key = new KeyPair(new byte[32]
            {
                0x21,0x22,0x23,0x24,0x25,0x26,0x27,0x28,
                0x29,0x2a,0x2b,0x2c,0x2d,0x2e,0x2f,0x30,
                0x31,0x32,0x33,0x34,0x35,0x36,0x37,0x38,
                0x39,0x3a,0x3b,0x3c,0x3d,0x3e,0x3f,0x40
            });
            var account = Contract.CreateSignatureRedeemScript(key.PublicKey).ToScriptHash();
            var tx = MakeTx(account);
            var sig = tx.Sign(key, TestProtocolSettings.Default.Network);
            Assert.IsNotNull(sig);
            Assert.IsTrue(sig.Length > 0);
        }
    }
}
