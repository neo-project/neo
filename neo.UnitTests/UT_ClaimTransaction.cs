using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Core;

namespace Neo.UnitTests
{
    [TestClass]
    public class UT_ClaimTransaction
    {
        ClaimTransaction uut;

        [TestInitialize]
        public void TestSetup()
        {
            uut = new ClaimTransaction();
        }

        [TestMethod]
        public void Claims_Get()
        {
            uut.Claims.Should().BeNull();            
        }

        [TestMethod]
        public void Claims_Set()
        {
            CoinReference val = new CoinReference();
            CoinReference[] refs = new CoinReference[] { val };
            uut.Claims = refs;
            uut.Claims.Length.Should().Be(1);
            uut.Claims[0].Should().Be(val);
        }

        [TestMethod]
        public void NetworkFee_Get()
        {
            uut.NetworkFee.Should().Be(Fixed8.Zero);
        }

        [TestMethod]
        public void Size__Get_0_Claims()
        {
            CoinReference[] refs = new CoinReference[0];
            uut.Claims = refs;

            uut.Attributes = new TransactionAttribute[0];
            uut.Inputs = new CoinReference[0];
            uut.Outputs = new TransactionOutput[0];
            uut.Scripts = new Witness[0];

            uut.Size.Should().Be(7); // 1, 1, 1, 1, 1, 1 + claims 1
        }

        [TestMethod]
        public void Size__Get_1_Claims()
        {
            CoinReference[] refs = new[] { TestUtils.GetCoinReference(null) };
            uut.Claims = refs;
            uut.Attributes = new TransactionAttribute[0];
            uut.Inputs = new CoinReference[0];
            uut.Outputs = new TransactionOutput[0];
            uut.Scripts = new Witness[0];

            uut.Size.Should().Be(41); // 1, 1, 1, 1, 1, 1 + claims 35
        }

        [TestMethod]
        public void Size__Get_3_Claims()
        {
            CoinReference[] refs = new[] { TestUtils.GetCoinReference(null), TestUtils.GetCoinReference(null), TestUtils.GetCoinReference(null) };
            uut.Claims = refs;
            uut.Attributes = new TransactionAttribute[0];
            uut.Inputs = new CoinReference[0];
            uut.Outputs = new TransactionOutput[0];
            uut.Scripts = new Witness[0];

            uut.Size.Should().Be(109); // 1, 1, 1, 1, 1, 1 + claims 103
        }

        [TestMethod]
        public void GetScriptHashesForVerifying_0_Claims()
        {            
            uut.Claims = new CoinReference[0];
            uut.Attributes = new TransactionAttribute[0];
            uut.Inputs = new CoinReference[0];
            uut.Outputs = new TransactionOutput[0];
            uut.Scripts = new Witness[0];

            uut.GetScriptHashesForVerifying().Length.Should().Be(0);
        }

        [TestMethod]
        public void GetScriptHashesForVerifying_1_Claim()
        {
            CoinReference[] refs = new[] { TestUtils.GetCoinReference(new UInt256(TestUtils.GetByteArray(32, 0x42))) };
            uut.Claims = refs;
            uut.Attributes = new TransactionAttribute[0];
            uut.Inputs = new CoinReference[0];
            uut.Outputs = new TransactionOutput[0];
            uut.Scripts = new Witness[0];

            TestUtils.SetupTestBlockchain(UInt256.Zero);

            UInt160[] res = uut.GetScriptHashesForVerifying();
            res.Length.Should().Be(1);
        }


        [TestMethod]
        public void GetScriptHashesForVerifying_2_Claim()
        {
            CoinReference[] refs = new[] { TestUtils.GetCoinReference(new UInt256(TestUtils.GetByteArray(32, 0x42))), TestUtils.GetCoinReference(new UInt256(TestUtils.GetByteArray(32, 0x48))) };
            uut.Claims = refs;
            uut.Attributes = new TransactionAttribute[0];
            uut.Inputs = new CoinReference[0];
            uut.Outputs = new TransactionOutput[0];
            uut.Scripts = new Witness[0];

            TestUtils.SetupTestBlockchain(UInt256.Zero);

            UInt160[] res = uut.GetScriptHashesForVerifying();
            res.Length.Should().Be(2);
        }

    }
}
