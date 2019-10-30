using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.SmartContract;
using Neo.Wallets;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Neo.UnitTests.SmartContract
{
    [TestClass]
    public class UT_Contract
    {
        [TestMethod]
        public void TestGetAddress()
        {
            byte[] privateKey = new byte[32];
            RandomNumberGenerator rng = RandomNumberGenerator.Create();
            rng.GetBytes(privateKey);
            KeyPair key = new KeyPair(privateKey);
            Contract contract = Contract.CreateSignatureContract(key.PublicKey);
            byte[] script = contract.Script;
            byte[] expectedArray = new byte[39];
            expectedArray[0] = 0x21;
            Array.Copy(key.PublicKey.EncodePoint(true), 0, expectedArray, 1, 33);
            expectedArray[34] = 0x68;
            Array.Copy(BitConverter.GetBytes(InteropService.Neo_Crypto_CheckSig), 0, expectedArray, 35, 4);
            Assert.AreEqual(expectedArray.ToScriptHash().ToAddress(), contract.Address);
        }

        [TestMethod]
        public void TestGetScriptHash()
        {
            byte[] privateKey = new byte[32];
            RandomNumberGenerator rng = RandomNumberGenerator.Create();
            rng.GetBytes(privateKey);
            KeyPair key = new KeyPair(privateKey);
            Contract contract = Contract.CreateSignatureContract(key.PublicKey);
            byte[] script = contract.Script;
            byte[] expectedArray = new byte[39];
            expectedArray[0] = 0x21;
            Array.Copy(key.PublicKey.EncodePoint(true), 0, expectedArray, 1, 33);
            expectedArray[34] = 0x68;
            Array.Copy(BitConverter.GetBytes(InteropService.Neo_Crypto_CheckSig), 0, expectedArray, 35, 4);
            Assert.AreEqual(expectedArray.ToScriptHash(), contract.ScriptHash);
        }

        [TestMethod]
        public void TestCreate()
        {
            byte[] script = new byte[32];
            ContractParameterType[] parameterList = new ContractParameterType[] { ContractParameterType.Signature };
            Contract contract = Contract.Create(parameterList, script);
            Assert.AreEqual(contract.Script, script);
            Assert.AreEqual(1, contract.ParameterList.Length);
            Assert.AreEqual(ContractParameterType.Signature, contract.ParameterList[0]);
        }

        [TestMethod]
        public void TestCreateMultiSigContract()
        {
            byte[] privateKey1 = new byte[32];
            RandomNumberGenerator rng1 = RandomNumberGenerator.Create();
            rng1.GetBytes(privateKey1);
            KeyPair key1 = new KeyPair(privateKey1);
            byte[] privateKey2 = new byte[32];
            RandomNumberGenerator rng2 = RandomNumberGenerator.Create();
            rng2.GetBytes(privateKey2);
            KeyPair key2 = new KeyPair(privateKey2);
            Neo.Cryptography.ECC.ECPoint[] publicKeys = new Neo.Cryptography.ECC.ECPoint[2];
            publicKeys[0] = key1.PublicKey;
            publicKeys[1] = key2.PublicKey;
            publicKeys = publicKeys.OrderBy(p => p).ToArray();
            Contract contract = Contract.CreateMultiSigContract(2, publicKeys);
            byte[] expectedArray = new byte[75];
            expectedArray[0] = 0x52;
            expectedArray[1] = 0x21;
            Array.Copy(publicKeys[0].EncodePoint(true), 0, expectedArray, 2, 33);
            expectedArray[35] = 0x21;
            Array.Copy(publicKeys[1].EncodePoint(true), 0, expectedArray, 36, 33);
            expectedArray[69] = 0x52;
            expectedArray[70] = 0x68;
            Array.Copy(BitConverter.GetBytes(InteropService.Neo_Crypto_CheckMultiSig), 0, expectedArray, 71, 4);
            Assert.AreEqual(Encoding.Default.GetString(expectedArray), Encoding.Default.GetString(contract.Script));
            Assert.AreEqual(2, contract.ParameterList.Length);
            Assert.AreEqual(ContractParameterType.Signature, contract.ParameterList[0]);
            Assert.AreEqual(ContractParameterType.Signature, contract.ParameterList[1]);
        }

        [TestMethod]
        public void TestCreateMultiSigRedeemScript()
        {
            byte[] privateKey1 = new byte[32];
            RandomNumberGenerator rng1 = RandomNumberGenerator.Create();
            rng1.GetBytes(privateKey1);
            KeyPair key1 = new KeyPair(privateKey1);
            byte[] privateKey2 = new byte[32];
            RandomNumberGenerator rng2 = RandomNumberGenerator.Create();
            rng2.GetBytes(privateKey2);
            KeyPair key2 = new KeyPair(privateKey2);
            Neo.Cryptography.ECC.ECPoint[] publicKeys = new Neo.Cryptography.ECC.ECPoint[2];
            publicKeys[0] = key1.PublicKey;
            publicKeys[1] = key2.PublicKey;
            publicKeys = publicKeys.OrderBy(p => p).ToArray();
            Action action = () => Contract.CreateMultiSigRedeemScript(0, publicKeys);
            action.Should().Throw<ArgumentException>();
            byte[] script = Contract.CreateMultiSigRedeemScript(2, publicKeys);
            byte[] expectedArray = new byte[75];
            expectedArray[0] = 0x52;
            expectedArray[1] = 0x21;
            Array.Copy(publicKeys[0].EncodePoint(true), 0, expectedArray, 2, 33);
            expectedArray[35] = 0x21;
            Array.Copy(publicKeys[1].EncodePoint(true), 0, expectedArray, 36, 33);
            expectedArray[69] = 0x52;
            expectedArray[70] = 0x68;
            Array.Copy(BitConverter.GetBytes(InteropService.Neo_Crypto_CheckMultiSig), 0, expectedArray, 71, 4);
            Assert.AreEqual(Encoding.Default.GetString(expectedArray), Encoding.Default.GetString(script));
        }

        [TestMethod]
        public void TestCreateSignatureContract()
        {
            byte[] privateKey = new byte[32];
            RandomNumberGenerator rng = RandomNumberGenerator.Create();
            rng.GetBytes(privateKey);
            KeyPair key = new KeyPair(privateKey);
            Contract contract = Contract.CreateSignatureContract(key.PublicKey);
            byte[] script = contract.Script;
            byte[] expectedArray = new byte[39];
            expectedArray[0] = 0x21;
            Array.Copy(key.PublicKey.EncodePoint(true), 0, expectedArray, 1, 33);
            expectedArray[34] = 0x68;
            Array.Copy(BitConverter.GetBytes(InteropService.Neo_Crypto_CheckSig), 0, expectedArray, 35, 4);
            Assert.AreEqual(Encoding.Default.GetString(expectedArray), Encoding.Default.GetString(script));
            Assert.AreEqual(1, contract.ParameterList.Length);
            Assert.AreEqual(ContractParameterType.Signature, contract.ParameterList[0]);
        }

        [TestMethod]
        public void TestCreateSignatureRedeemScript()
        {
            byte[] privateKey = new byte[32];
            RandomNumberGenerator rng = RandomNumberGenerator.Create();
            rng.GetBytes(privateKey);
            KeyPair key = new KeyPair(privateKey);
            byte[] script = Contract.CreateSignatureRedeemScript(key.PublicKey);
            byte[] expectedArray = new byte[39];
            expectedArray[0] = 0x21;
            Array.Copy(key.PublicKey.EncodePoint(true), 0, expectedArray, 1, 33);
            expectedArray[34] = 0x68;
            Array.Copy(BitConverter.GetBytes(InteropService.Neo_Crypto_CheckSig), 0, expectedArray, 35, 4);
            Assert.AreEqual(Encoding.Default.GetString(expectedArray), Encoding.Default.GetString(script));
        }
    }
}
