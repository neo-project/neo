// Copyright (C) 2015-2024 The Neo Project.
//
// UT_NotaryAssisted.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO;
using Neo.Network.P2P.Payloads;
using Neo.SmartContract;
using Neo.SmartContract.Native;
using Neo.VM;
using System;

namespace Neo.UnitTests.Network.P2P.Payloads
{
    [TestClass]
    public class UT_NotaryAssisted
    {
        // Use the hard-coded Notary hash value from NeoGo to ensure hashes are compatible.
        private static readonly UInt160 notaryHash = UInt160.Parse("0xc1e14f19c3e60d0b9244d06dd7ba9b113135ec3b");

        [TestMethod]
        public void Size_Get()
        {
            var attr = new NotaryAssisted() { NKeys = 4 };
            attr.Size.Should().Be(1 + 1);
        }

        [TestMethod]
        public void ToJson()
        {
            var attr = new NotaryAssisted() { NKeys = 4 };
            var json = attr.ToJson().ToString();
            Assert.AreEqual(@"{""type"":""NotaryAssisted"",""nkeys"":4}", json);
        }

        [TestMethod]
        public void DeserializeAndSerialize()
        {
            var attr = new NotaryAssisted() { NKeys = 4 };

            var clone = attr.ToArray().AsSerializable<NotaryAssisted>();
            Assert.AreEqual(clone.Type, attr.Type);

            // As transactionAttribute
            byte[] buffer = attr.ToArray();
            var reader = new MemoryReader(buffer);
            clone = TransactionAttribute.DeserializeFrom(ref reader) as NotaryAssisted;
            Assert.AreEqual(clone.Type, attr.Type);

            // Wrong type
            buffer[0] = 0xff;
            Assert.ThrowsException<FormatException>(() =>
            {
                var reader = new MemoryReader(buffer);
                TransactionAttribute.DeserializeFrom(ref reader);
            });
        }

        [TestMethod]
        public void Verify()
        {
            var attr = new NotaryAssisted() { NKeys = 4 };

            // Temporary use Notary contract hash stub for valid transaction.
            var txGood = new Transaction { Signers = new Signer[] { new Signer() { Account = notaryHash } } };
            var txBad = new Transaction { Signers = new Signer[] { new Signer() { Account = UInt160.Parse("0xa400ff00ff00ff00ff00ff00ff00ff00ff00ff01") } } };
            var snapshot = TestBlockchain.GetTestSnapshot();

            Assert.IsTrue(attr.Verify(snapshot, txGood));
            Assert.IsFalse(attr.Verify(snapshot, txBad));
        }

        [TestMethod]
        public void CalculateNetworkFee()
        {
            var snapshot = TestBlockchain.GetTestSnapshot();
            var attr = new NotaryAssisted() { NKeys = 4 };
            var tx = new Transaction { Signers = new Signer[] { new Signer() { Account = notaryHash } } };

            Assert.AreEqual((4 + 1) * 1000_0000, attr.CalculateNetworkFee(snapshot, tx));
        }
    }
}
