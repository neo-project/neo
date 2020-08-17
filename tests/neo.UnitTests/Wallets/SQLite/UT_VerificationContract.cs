using Microsoft.VisualStudio.TestTools.UnitTesting;
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
            BinaryReader reader = new BinaryReader(stream);
            contract1.Serialize(writer);
            stream.Seek(0, SeekOrigin.Begin);
            VerificationContract contract2 = new VerificationContract();
            contract2.Deserialize(reader);
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
            MemoryStream stream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(stream);
            contract1.Serialize(writer);
            stream.Seek(0, SeekOrigin.Begin);
            byte[] byteArray = new byte[stream.Length];
            stream.Read(byteArray, 0, (int)stream.Length);
            byte[] script = Neo.SmartContract.Contract.CreateSignatureRedeemScript(key.PublicKey);
            byte[] result = new byte[64];
            result[20] = 0x01;
            result[21] = (byte)ContractParameterType.Signature;
            result[22] = 0x29;
            Array.Copy(script, 0, result, 23, 41);
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
            Assert.AreEqual(64, contract1.Size);
        }
    }
}
