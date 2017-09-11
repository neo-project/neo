using System;
using System.Collections.Generic;
using System.Text;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Core;
using Neo.IO.Json;
using Neo.Wallets;
using Neo.VM;
using Neo.SmartContract;

namespace Neo.UnitTests
{
    [TestClass]
    public class UT_IssueTransaction
    {
        IssueTransaction uut;

        [TestInitialize]
        public void TestSetup()
        {
            uut = new IssueTransaction();
        }

        [TestMethod]
        public void SystemFee_Get()
        {
            uut.Version = 1;
            uut.SystemFee.Should().Be(Fixed8.Zero);
        }

        [TestMethod]
        public void SystemFee_Get_Version_0_Share()
        {
            uut = TestUtils.GetIssueTransaction(false, 10, Blockchain.GoverningToken.Hash);            
            uut.Version = 0;
            
            uut.SystemFee.Should().Be(Fixed8.Zero);
        }

        [TestMethod]
        public void SystemFee_Get_Version_0_Coin()
        {
            uut = TestUtils.GetIssueTransaction(false, 10, Blockchain.UtilityToken.Hash);
            uut.Version = 0;

            uut.SystemFee.Should().Be(Fixed8.Zero);
        }

        [TestMethod]
        public void SystemFee_Get_Version_0_OtherAsset()
        {
            uut = TestUtils.GetIssueTransaction(false, 10, new UInt256(TestUtils.GetByteArray(32,0x42)));
            uut.Version = 0;
             
            uut.SystemFee.Should().Be(Fixed8.FromDecimal(500));
        }

        [TestMethod]
        public void GetScriptHashesForVerifying()
        {
            TestUtils.SetupTestBlockchain(UInt256.Zero);
            uut = TestUtils.GetIssueTransaction(false, 10, Blockchain.UtilityToken.Hash);
            UInt160[] res = uut.GetScriptHashesForVerifying();
            res.Length.Should().Be(1);
            res[0].Should().Be(new UInt160(TestUtils.GetByteArray(20, 0xe7)));
        }

        [TestMethod]
        public void GetScriptHashesForVerifying_ThrowsException_NullAsset()
        {
            TestUtils.SetupTestBlockchain(UInt256.Zero);
            uut = TestUtils.GetIssueTransaction(false, 10, UInt256.Zero);
            Action test = () => uut.GetScriptHashesForVerifying();
            test.ShouldThrow<InvalidOperationException>();
        }

        [TestMethod]
        public void GetScriptHashesForVerifying_Ordered()
        {
            TestUtils.SetupTestBlockchain(UInt256.Zero);
            uut =  new IssueTransaction
            {
                Attributes = new TransactionAttribute[0],
                Inputs = new CoinReference[0],
                Outputs = new[]
                {
                    new TransactionOutput
                    {
                        AssetId = Blockchain.UtilityToken.Hash,
                        Value = Fixed8.FromDecimal(10),
                        ScriptHash = Contract.CreateMultiSigRedeemScript(1, TestUtils.StandbyValidators).ToScriptHash()
                    },
                    new TransactionOutput
                    {
                        AssetId = Blockchain.GoverningToken.Hash,
                        Value = Fixed8.FromDecimal(10),
                        ScriptHash = Contract.CreateMultiSigRedeemScript(1, TestUtils.StandbyValidators).ToScriptHash()
                    },
                },
                Scripts = new[]
                {
                    new Witness
                    {
                        InvocationScript = new byte[0],
                        VerificationScript = new[] { (byte)OpCode.PUSHT }
                    }
                }
            };
            UInt160[] res = uut.GetScriptHashesForVerifying();
            res.Length.Should().Be(2);
            res[0].Should().Be(new UInt160(TestUtils.GetByteArray(20, 0x9b)));
            res[1].Should().Be(new UInt160(TestUtils.GetByteArray(20, 0xe7)));
        }

    }
}
