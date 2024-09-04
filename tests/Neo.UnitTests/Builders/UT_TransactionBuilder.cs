// Copyright (C) 2015-2024 The Neo Project.
//
// UT_TransactionBuilder.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Builders;
using Neo.Network.P2P.Payloads;
using Neo.VM;
using Neo.Wallets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neo.UnitTests.Builders
{
    [TestClass]
    public class UT_TransactionBuilder
    {
        [TestMethod]
        public void TestVersion()
        {
            byte expectedVersion = 1;
            var tx = TransactionBuilder.CreateEmpty()
                .Version(expectedVersion)
                .Build();

            Assert.AreEqual(expectedVersion, tx.Version);
            Assert.IsNotNull(tx.Hash);
        }

        [TestMethod]
        public void TestNonce()
        {
            var expectedNonce = (uint)Random.Shared.Next();
            var tx = TransactionBuilder.CreateEmpty()
                .Nonce(expectedNonce)
                .Build();

            Assert.AreEqual(expectedNonce, tx.Nonce);
            Assert.IsNotNull(tx.Hash);
        }

        [TestMethod]
        public void TestSystemFee()
        {
            var expectedSystemFee = (uint)Random.Shared.Next();
            var tx = TransactionBuilder.CreateEmpty()
                .SystemFee(expectedSystemFee)
                .Build();

            Assert.AreEqual(expectedSystemFee, tx.SystemFee);
            Assert.IsNotNull(tx.Hash);
        }

        [TestMethod]
        public void TestNetworkFee()
        {
            var expectedNetworkFee = (uint)Random.Shared.Next();
            var tx = TransactionBuilder.CreateEmpty()
                .NetworkFee(expectedNetworkFee)
                .Build();

            Assert.AreEqual(expectedNetworkFee, tx.NetworkFee);
            Assert.IsNotNull(tx.Hash);
        }

        [TestMethod]
        public void TestValidUntilBlock()
        {
            var expectedValidUntilBlock = (uint)Random.Shared.Next();
            var tx = TransactionBuilder.CreateEmpty()
                .ValidUntil(expectedValidUntilBlock)
                .Build();

            Assert.AreEqual(expectedValidUntilBlock, tx.ValidUntilBlock);
            Assert.IsNotNull(tx.Hash);
        }

        [TestMethod]
        public void TestAttachScript()
        {
            byte[] expectedScript = [(byte)OpCode.NOP];
            var tx = TransactionBuilder.CreateEmpty()
                .AttachSystem(sb => sb.Emit(OpCode.NOP))
                .Build();

            CollectionAssert.AreEqual(expectedScript, tx.Script.ToArray());
            Assert.IsNotNull(tx.Hash);
        }

        [TestMethod]
        public void TestTransactionAttributes()
        {
            var tx = TransactionBuilder.CreateEmpty()
                .AddAttributes(ab => ab.AddHighPriority())
                .Build();

            Assert.AreEqual(1, tx.Attributes.Length);
            Assert.IsInstanceOfType<HighPriorityAttribute>(tx.Attributes[0]);
            Assert.IsNotNull(tx.Hash);
        }

        [TestMethod]
        public void TestWitness()
        {
            var tx = TransactionBuilder.CreateEmpty()
                .AddWitness(wb =>
                {
                    // Contract signature
                    wb.AddInvocation([]);
                    wb.AddVerification([]);
                })
                .Build();

            Assert.AreEqual(1, tx.Witnesses.Length);
            Assert.AreEqual(0, tx.Witnesses[0].InvocationScript.Length);
            Assert.AreEqual(0, tx.Witnesses[0].VerificationScript.Length);
            Assert.IsNotNull(tx.Hash);
        }

        [TestMethod]
        public void TestWitnessWithTransactionParameter()
        {
            var tx = TransactionBuilder.CreateEmpty()
                .AddWitness((wb, tx) =>
                {
                    // Checks to make sure the transaction is hash able
                    // NOTE: transaction can be used for signing here
                    Assert.IsNotNull(tx.Hash);
                })
                .Build();
        }

        [TestMethod]
        public void TestEmptyTransaction()
        {
            var tx = TransactionBuilder.CreateEmpty()
                .Build();

            Assert.IsNotNull(tx.Hash);
        }
    }
}
