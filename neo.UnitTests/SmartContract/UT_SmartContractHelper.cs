using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Neo.Ledger;
using Neo.Network.P2P.Payloads;
using Neo.Persistence;
using Neo.SmartContract;
using Neo.VM;
using Neo.VM.Types;
using Neo.Wallets;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace Neo.UnitTests.SmartContract
{
    [TestClass]
    public class UT_SmartContractHelper
    {
        [TestMethod]
        public void TestIsMultiSigContract()
        {
            Neo.Cryptography.ECC.ECPoint[] publicKeys1 = new Neo.Cryptography.ECC.ECPoint[20];
            for (int i = 0; i < 20; i++)
            {
                byte[] privateKey1 = new byte[32];
                RandomNumberGenerator rng1 = RandomNumberGenerator.Create();
                rng1.GetBytes(privateKey1);
                KeyPair key1 = new KeyPair(privateKey1);
                publicKeys1[i] = key1.PublicKey;
            }
            byte[] script1 = Contract.CreateMultiSigRedeemScript(20, publicKeys1);
            Assert.AreEqual(true, Neo.SmartContract.Helper.IsMultiSigContract(script1, out int m1, out int n1));

            Neo.Cryptography.ECC.ECPoint[] publicKeys2 = new Neo.Cryptography.ECC.ECPoint[256];
            for (int i = 0; i < 256; i++)
            {
                byte[] privateKey2 = new byte[32];
                RandomNumberGenerator rng2 = RandomNumberGenerator.Create();
                rng2.GetBytes(privateKey2);
                KeyPair key2 = new KeyPair(privateKey2);
                publicKeys2[i] = key2.PublicKey;
            }
            byte[] script2 = Contract.CreateMultiSigRedeemScript(256, publicKeys2);
            Assert.AreEqual(true, Neo.SmartContract.Helper.IsMultiSigContract(script2, out int m2, out int n2));

            Neo.Cryptography.ECC.ECPoint[] publicKeys3 = new Neo.Cryptography.ECC.ECPoint[3];
            for (int i = 0; i < 3; i++)
            {
                byte[] privateKey3 = new byte[32];
                RandomNumberGenerator rng3 = RandomNumberGenerator.Create();
                rng3.GetBytes(privateKey3);
                KeyPair key3 = new KeyPair(privateKey3);
                publicKeys3[i] = key3.PublicKey;
            }
            byte[] script3 = Contract.CreateMultiSigRedeemScript(3, publicKeys3);
            Assert.AreEqual(true, Neo.SmartContract.Helper.IsMultiSigContract(script3, out int m3, out int n3));

            Neo.Cryptography.ECC.ECPoint[] publicKeys4 = new Neo.Cryptography.ECC.ECPoint[3];
            for (int i = 0; i < 3; i++)
            {
                byte[] privateKey4 = new byte[32];
                RandomNumberGenerator rng4 = RandomNumberGenerator.Create();
                rng4.GetBytes(privateKey4);
                KeyPair key4 = new KeyPair(privateKey4);
                publicKeys4[i] = key4.PublicKey;
            }
            byte[] script4 = Contract.CreateMultiSigRedeemScript(3, publicKeys4);
            script4[script4.Length - 1] = 0x00;
            Assert.AreEqual(false, Neo.SmartContract.Helper.IsMultiSigContract(script4, out int m4, out int n4));

        }

        [TestMethod]
        public void TestIsSignatureContract()
        {
            byte[] privateKey = new byte[32];
            RandomNumberGenerator rng = RandomNumberGenerator.Create();
            rng.GetBytes(privateKey);
            KeyPair key = new KeyPair(privateKey);
            byte[] script = Contract.CreateSignatureRedeemScript(key.PublicKey);
            Assert.AreEqual(true, Neo.SmartContract.Helper.IsSignatureContract(script));
            script[0] = 0x22;
            Assert.AreEqual(false, Neo.SmartContract.Helper.IsSignatureContract(script));
        }

        [TestMethod]
        public void TestIsStandardContract()
        {
            byte[] privateKey1 = new byte[32];
            RandomNumberGenerator rng1 = RandomNumberGenerator.Create();
            rng1.GetBytes(privateKey1);
            KeyPair key1 = new KeyPair(privateKey1);
            byte[] script1 = Contract.CreateSignatureRedeemScript(key1.PublicKey);
            Assert.AreEqual(true, Neo.SmartContract.Helper.IsStandardContract(script1));

            Neo.Cryptography.ECC.ECPoint[] publicKeys2 = new Neo.Cryptography.ECC.ECPoint[3];
            for (int i = 0; i < 3; i++)
            {
                byte[] privateKey2 = new byte[32];
                RandomNumberGenerator rng2 = RandomNumberGenerator.Create();
                rng2.GetBytes(privateKey2);
                KeyPair key2 = new KeyPair(privateKey2);
                publicKeys2[i] = key2.PublicKey;
            }
            byte[] script2 = Contract.CreateMultiSigRedeemScript(3, publicKeys2);
            Assert.AreEqual(true, Neo.SmartContract.Helper.IsStandardContract(script2));
        }

        [TestMethod]
        public void TestSerialize()
        {
            StackItem stackItem1 = new ByteArray(new byte[5]);
            byte[] result1 = Neo.SmartContract.Helper.Serialize(stackItem1);
            byte[] expectedArray1 = new byte[] {
                        0x00, 0x05, 0x00, 0x00, 0x00, 0x00, 0x00
                    };
            Assert.AreEqual(Encoding.Default.GetString(expectedArray1), Encoding.Default.GetString(result1));

            StackItem stackItem2 = new VM.Types.Boolean(true);
            byte[] result2 = Neo.SmartContract.Helper.Serialize(stackItem2);
            byte[] expectedArray2 = new byte[] {
                        0x01, 0x01
                    };
            Assert.AreEqual(Encoding.Default.GetString(expectedArray2), Encoding.Default.GetString(result2));

            StackItem stackItem3 = new VM.Types.Integer(1);
            byte[] result3 = Neo.SmartContract.Helper.Serialize(stackItem3);
            byte[] expectedArray3 = new byte[] {
                        0x02, 0x01, 0x01
                    };
            Assert.AreEqual(Encoding.Default.GetString(expectedArray3), Encoding.Default.GetString(result3));

            StackItem stackItem4 = new InteropInterface<object>(new object());
            Action action4 = () => Neo.SmartContract.Helper.Serialize(stackItem4);
            action4.Should().Throw<NotSupportedException>();

            StackItem stackItem5 = new VM.Types.Integer(1);
            byte[] result5 = Neo.SmartContract.Helper.Serialize(stackItem5);
            byte[] expectedArray5 = new byte[] {
                        0x02, 0x01, 0x01
                    };
            Assert.AreEqual(Encoding.Default.GetString(expectedArray5), Encoding.Default.GetString(result5));


            StackItem stackItem61 = new VM.Types.Integer(1);
            List<StackItem> list6 = new List<StackItem>
                    {
                        stackItem61
                    };
            StackItem stackItem62 = new VM.Types.Array(list6);
            byte[] result6 = Neo.SmartContract.Helper.Serialize(stackItem62);
            byte[] expectedArray6 = new byte[] {
                        0x80,0x01,0x02,0x01,0x01
                    };
            Assert.AreEqual(Encoding.Default.GetString(expectedArray6), Encoding.Default.GetString(result6));

            StackItem stackItem71 = new VM.Types.Integer(1);
            List<StackItem> list7 = new List<StackItem>
                    {
                        stackItem71
                    };
            StackItem stackItem72 = new VM.Types.Struct(list7);
            byte[] result7 = Neo.SmartContract.Helper.Serialize(stackItem72);
            byte[] expectedArray7 = new byte[] {
                        0x81,0x01,0x02,0x01,0x01
                    };
            Assert.AreEqual(Encoding.Default.GetString(expectedArray7), Encoding.Default.GetString(result7));

            StackItem stackItem81 = new VM.Types.Integer(1);
            Dictionary<StackItem, StackItem> list8 = new Dictionary<StackItem, StackItem>
                    {
                        { new VM.Types.Integer(2), stackItem81 }
                    };
            StackItem stackItem82 = new VM.Types.Map(list8);
            byte[] result8 = Neo.SmartContract.Helper.Serialize(stackItem82);
            byte[] expectedArray8 = new byte[] {
                        0x82,0x01,0x02,0x01,0x02,0x02,0x01,0x01
                    };
            Assert.AreEqual(Encoding.Default.GetString(expectedArray8), Encoding.Default.GetString(result8));

            StackItem stackItem9 = new VM.Types.Integer(1);
            Map stackItem91 = new VM.Types.Map();
            stackItem91.Add(stackItem9, stackItem91);
            Action action9 = () => Neo.SmartContract.Helper.Serialize(stackItem91);
            action9.Should().Throw<NotSupportedException>();

            VM.Types.Array stackItem10 = new VM.Types.Array();
            stackItem10.Add(stackItem10);
            Action action10 = () => Neo.SmartContract.Helper.Serialize(stackItem10);
            action10.Should().Throw<NotSupportedException>();
        }

        [TestMethod]
        public void TestDeserializeStackItem()
        {
            StackItem stackItem1 = new ByteArray(new byte[5]);
            byte[] byteArray1 = Neo.SmartContract.Helper.Serialize(stackItem1);
            StackItem result1 = Neo.SmartContract.Helper.DeserializeStackItem(byteArray1, 1, (uint)byteArray1.Length);
            Assert.AreEqual(stackItem1, result1);

            StackItem stackItem2 = new VM.Types.Boolean(true);
            byte[] byteArray2 = Neo.SmartContract.Helper.Serialize(stackItem2);
            StackItem result2 = Neo.SmartContract.Helper.DeserializeStackItem(byteArray2, 1, (uint)byteArray2.Length);
            Assert.AreEqual(stackItem2, result2);

            StackItem stackItem3 = new VM.Types.Integer(1);
            byte[] byteArray3 = Neo.SmartContract.Helper.Serialize(stackItem3);
            StackItem result3 = Neo.SmartContract.Helper.DeserializeStackItem(byteArray3, 1, (uint)byteArray3.Length);
            Assert.AreEqual(stackItem3, result3);

            StackItem stackItem4 = new VM.Types.Integer(1);
            byte[] byteArray4 = Neo.SmartContract.Helper.Serialize(stackItem4);
            byteArray4[0] = 0x40;
            Action action4 = () => Neo.SmartContract.Helper.DeserializeStackItem(byteArray4, 1, (uint)byteArray4.Length);
            action4.Should().Throw<FormatException>();

            StackItem stackItem51 = new VM.Types.Integer(1);
            List<StackItem> list5 = new List<StackItem>();
            list5.Add(stackItem51);
            StackItem stackItem52 = new VM.Types.Array(list5);
            byte[] byteArray5 = Neo.SmartContract.Helper.Serialize(stackItem52);
            StackItem result5 = Neo.SmartContract.Helper.DeserializeStackItem(byteArray5, 1, (uint)byteArray5.Length);
            Assert.AreEqual(((VM.Types.Array)stackItem52).Count, ((VM.Types.Array)result5).Count);
            Assert.AreEqual(((VM.Types.Array)stackItem52).GetEnumerator().Current, ((VM.Types.Array)result5).GetEnumerator().Current);

            StackItem stackItem61 = new VM.Types.Integer(1);
            List<StackItem> list6 = new List<StackItem>();
            list6.Add(stackItem61);
            StackItem stackItem62 = new VM.Types.Struct(list6);
            byte[] byteArray6 = Neo.SmartContract.Helper.Serialize(stackItem62);
            StackItem result6 = Neo.SmartContract.Helper.DeserializeStackItem(byteArray6, 1, (uint)byteArray6.Length);
            Assert.AreEqual(((VM.Types.Struct)stackItem62).Count, ((VM.Types.Struct)result6).Count);
            Assert.AreEqual(((VM.Types.Struct)stackItem62).GetEnumerator().Current, ((VM.Types.Struct)result6).GetEnumerator().Current);

            StackItem stackItem71 = new VM.Types.Integer(1);
            Dictionary<StackItem, StackItem> list7 = new Dictionary<StackItem, StackItem>();
            list7.Add(new VM.Types.Integer(2), stackItem71);
            StackItem stackItem72 = new VM.Types.Map(list7);
            byte[] byteArray7 = Neo.SmartContract.Helper.Serialize(stackItem72);
            StackItem result7 = Neo.SmartContract.Helper.DeserializeStackItem(byteArray7, 1, (uint)byteArray7.Length);
            Assert.AreEqual(((VM.Types.Map)stackItem72).Count, ((VM.Types.Map)result7).Count);
            Assert.AreEqual(((VM.Types.Map)stackItem72).Keys.GetEnumerator().Current, ((VM.Types.Map)result7).Keys.GetEnumerator().Current);
            Assert.AreEqual(((VM.Types.Map)stackItem72).Values.GetEnumerator().Current, ((VM.Types.Map)result7).Values.GetEnumerator().Current);
        }

        [TestMethod]
        public void TestToInteropMethodHash()
        {
            byte[] temp1 = Encoding.ASCII.GetBytes("AAAA");
            byte[] temp2 = Neo.Cryptography.Helper.Sha256(temp1);
            uint result = BitConverter.ToUInt32(temp2, 0);
            Assert.AreEqual(result, Neo.SmartContract.Helper.ToInteropMethodHash("AAAA"));
        }

        [TestMethod]
        public void TestToScriptHash()
        {
            byte[] temp1 = Encoding.ASCII.GetBytes("AAAA");
            byte[] temp2 = Neo.Cryptography.Helper.Sha256(temp1);
            uint result = BitConverter.ToUInt32(temp2, 0);
            Assert.AreEqual(result, Neo.SmartContract.Helper.ToInteropMethodHash("AAAA"));
        }

        [TestMethod]
        public void TestVerifyWitnesses()
        {
            var mockSnapshot1 = new Mock<Snapshot>();
            UInt256 index1 = UInt256.Parse("0xa400ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff01");
            TestDataCache<UInt256, TrimmedBlock> testDataCache1 = new TestDataCache<UInt256, TrimmedBlock>();
            testDataCache1.Add(index1, new TrimmedBlock());
            testDataCache1.Delete(index1);
            mockSnapshot1.SetupGet(p => p.Blocks).Returns(testDataCache1);
            Assert.AreEqual(false, Neo.SmartContract.Helper.VerifyWitnesses(new Header() { PrevHash = index1 }, mockSnapshot1.Object, 100));

            var mockSnapshot2 = new Mock<Snapshot>();
            UInt256 index2 = UInt256.Parse("0xa400ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff01");
            TrimmedBlock block2 = new TrimmedBlock();
            block2.NextConsensus = UInt160.Zero;
            TestDataCache<UInt256, TrimmedBlock> testDataCache21 = new TestDataCache<UInt256, TrimmedBlock>();
            testDataCache21.Add(index2, block2);
            Header header2 = new Header() { PrevHash = index2, Witness = new Witness { VerificationScript = new byte[0] } };
            mockSnapshot2.SetupGet(p => p.Blocks).Returns(testDataCache21);

            TestDataCache<UInt160, ContractState> testDataCache22 = new TestDataCache<UInt160, ContractState>();
            testDataCache22.Add(UInt160.Zero, new ContractState());
            testDataCache22.Delete(UInt160.Zero);
            mockSnapshot2.SetupGet(p => p.Contracts).Returns(testDataCache22);
            Assert.AreEqual(false, Neo.SmartContract.Helper.VerifyWitnesses(header2, mockSnapshot2.Object, 100));

            var mockSnapshot3 = new Mock<Snapshot>();
            UInt256 index3 = UInt256.Parse("0xa400ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff00ff01");
            TrimmedBlock block3 = new TrimmedBlock();
            block3.NextConsensus = UInt160.Zero;
            TestDataCache<UInt256, TrimmedBlock> testDataCache31 = new TestDataCache<UInt256, TrimmedBlock>();
            testDataCache31.Add(index3, block3);
            Header header3 = new Header() { PrevHash = index3, Witness = new Witness { VerificationScript = new byte[0] } };
            mockSnapshot3.SetupGet(p => p.Blocks).Returns(testDataCache31);
            TestDataCache<UInt160, ContractState> testDataCache32 = new TestDataCache<UInt160, ContractState>();
            testDataCache32.Add(UInt160.Zero, new ContractState());
            mockSnapshot3.SetupGet(p => p.Contracts).Returns(testDataCache32);
            Assert.AreEqual(false, Neo.SmartContract.Helper.VerifyWitnesses(header3, mockSnapshot3.Object, 100));
        }
    }
}
