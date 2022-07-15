using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO;
using Neo.SmartContract;
using Neo.Wallets;
using Neo.Wallets.SQLite;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Neo.UnitTests
{
    [TestClass]
    public class UT_VerificationContract
    {
        [TestMethod]
        public void TestGenerator()
        {
            byte[] privateKey = new byte[32];
            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(privateKey);
            }
            KeyPair key = new KeyPair(privateKey);
            VerificationContract contract = new VerificationContract
            {
                Script = Neo.SmartContract.Contract.CreateSignatureRedeemScript(key.PublicKey),
                ParameterList = new[] { ContractParameterType.Signature }
            };
            Assert.IsNotNull(contract);
        }

        [TestMethod]
        public void TestDeserialize()
        {
            byte[] privateKey = new byte[32];
            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(privateKey);
            }
            KeyPair key = new KeyPair(privateKey);
            VerificationContract contract1 = new VerificationContract
            {
                Script = Neo.SmartContract.Contract.CreateSignatureRedeemScript(key.PublicKey),
                ParameterList = new[] { ContractParameterType.Signature }
            };
            MemoryStream stream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(stream);
            contract1.Serialize(writer);
            MemoryReader reader = new(stream.ToArray());
            VerificationContract contract2 = new VerificationContract();
            contract2.Deserialize(ref reader);
            Assert.AreEqual(Encoding.Default.GetString(contract2.Script), Encoding.Default.GetString(contract1.Script));
            Assert.AreEqual(1, contract2.ParameterList.Length);
            Assert.AreEqual(ContractParameterType.Signature, contract2.ParameterList[0]);
        }

        [TestMethod]
        public void TestEquals()
        {
            byte[] privateKey = new byte[32];
            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(privateKey);
            }
            KeyPair key = new KeyPair(privateKey);
            VerificationContract contract1 = new VerificationContract
            {
                Script = Neo.SmartContract.Contract.CreateSignatureRedeemScript(key.PublicKey),
                ParameterList = new[] { ContractParameterType.Signature }
            };
            object tempObject = contract1;
            VerificationContract contract2 = new VerificationContract
            {
                Script = Neo.SmartContract.Contract.CreateSignatureRedeemScript(key.PublicKey),
                ParameterList = new[] { ContractParameterType.Signature }
            };
            Assert.AreEqual(true, contract1.Equals(tempObject));
            Assert.AreEqual(true, contract1.Equals(contract1));
            Assert.AreEqual(false, contract1.Equals(null));
            Assert.AreEqual(true, contract1.Equals(contract2));
        }

        [TestMethod]
        public void TestGetHashCode()
        {
            byte[] privateKey = new byte[32];
            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(privateKey);
            }
            KeyPair key = new KeyPair(privateKey);
            VerificationContract contract1 = new VerificationContract
            {
                Script = Neo.SmartContract.Contract.CreateSignatureRedeemScript(key.PublicKey),
                ParameterList = new[] { ContractParameterType.Signature }
            };
            byte[] script = Neo.SmartContract.Contract.CreateSignatureRedeemScript(key.PublicKey);
            Assert.AreEqual(script.ToScriptHash().GetHashCode(), contract1.GetHashCode());
        }

        [TestMethod]
        public void TestSerialize()
        {
            byte[] privateKey = new byte[32];
            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(privateKey);
            }
            KeyPair key = new KeyPair(privateKey);
            VerificationContract contract1 = new VerificationContract
            {
                Script = Neo.SmartContract.Contract.CreateSignatureRedeemScript(key.PublicKey),
                ParameterList = new[] { ContractParameterType.Signature }
            };
            byte[] byteArray = contract1.ToArray();
            byte[] script = Neo.SmartContract.Contract.CreateSignatureRedeemScript(key.PublicKey);
            byte[] result = new byte[43];
            result[0] = 0x01;
            result[1] = (byte)ContractParameterType.Signature;
            result[2] = 0x28;
            Array.Copy(script, 0, result, 3, 40);
            CollectionAssert.AreEqual(result, byteArray);
        }

        [TestMethod]
        public void TestGetSize()
        {
            byte[] privateKey = new byte[32];
            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(privateKey);
            }
            KeyPair key = new KeyPair(privateKey);
            VerificationContract contract1 = new VerificationContract
            {
                Script = Neo.SmartContract.Contract.CreateSignatureRedeemScript(key.PublicKey),
                ParameterList = new[] { ContractParameterType.Signature }
            };
            Assert.AreEqual(43, contract1.Size);
        }
    }
}
