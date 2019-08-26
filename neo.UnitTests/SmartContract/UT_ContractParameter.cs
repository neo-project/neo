using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Cryptography.ECC;
using Neo.IO.Json;
using Neo.SmartContract;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Neo.UnitTests.SmartContract
{
    [TestClass]
    public class UT_ContractParameter
    {
        [TestMethod]
        public void TestGenerator1()
        {
            ContractParameter contractParameter = new ContractParameter();
            Assert.IsNotNull(contractParameter);
        }

        [TestMethod]
        public void TestGenerator2()
        {
            for (int i = 0; i < 11; i++)
            {
                if (i == 0)
                {
                    ContractParameter contractParameter = new ContractParameter(ContractParameterType.Signature);
                    byte[] expectedArray = new byte[64];
                    Assert.IsNotNull(contractParameter);
                    Assert.AreEqual(Encoding.Default.GetString(expectedArray), Encoding.Default.GetString((byte[])contractParameter.Value));
                }
                else if (i == 1)
                {
                    ContractParameter contractParameter = new ContractParameter(ContractParameterType.Boolean);
                    Assert.IsNotNull(contractParameter);
                    Assert.AreEqual(false, contractParameter.Value);
                }
                else if (i == 2)
                {
                    ContractParameter contractParameter = new ContractParameter(ContractParameterType.Integer);
                    Assert.IsNotNull(contractParameter);
                    Assert.AreEqual(0, contractParameter.Value);
                }
                else if (i == 3)
                {
                    ContractParameter contractParameter = new ContractParameter(ContractParameterType.Hash160);
                    Assert.IsNotNull(contractParameter);
                    Assert.AreEqual(new UInt160(), contractParameter.Value);
                }
                else if (i == 4)
                {
                    ContractParameter contractParameter = new ContractParameter(ContractParameterType.Hash256);
                    Assert.IsNotNull(contractParameter);
                    Assert.AreEqual(new UInt256(), contractParameter.Value);
                }
                else if (i == 5)
                {
                    ContractParameter contractParameter = new ContractParameter(ContractParameterType.ByteArray);
                    byte[] expectedArray = new byte[0];
                    Assert.IsNotNull(contractParameter);
                    Assert.AreEqual(Encoding.Default.GetString(expectedArray), Encoding.Default.GetString((byte[])contractParameter.Value));
                }
                else if (i == 6)
                {
                    ContractParameter contractParameter = new ContractParameter(ContractParameterType.PublicKey);
                    Assert.IsNotNull(contractParameter);
                    Assert.AreEqual(ECCurve.Secp256r1.G, contractParameter.Value);
                }
                else if (i == 7)
                {
                    ContractParameter contractParameter = new ContractParameter(ContractParameterType.String);
                    Assert.IsNotNull(contractParameter);
                    Assert.AreEqual("", contractParameter.Value);
                }
                else if (i == 8)
                {
                    ContractParameter contractParameter = new ContractParameter(ContractParameterType.Array);
                    Assert.IsNotNull(contractParameter);
                    Assert.AreEqual(0, ((List<ContractParameter>)contractParameter.Value).Count);
                }
                else if (i == 9)
                {
                    ContractParameter contractParameter = new ContractParameter(ContractParameterType.Map);
                    Assert.IsNotNull(contractParameter);
                    Assert.AreEqual(0, ((List<KeyValuePair<ContractParameter, ContractParameter>>)contractParameter.Value).Count);
                }
                else
                {
                    Action action = () => new ContractParameter(ContractParameterType.Void);
                    action.ShouldThrow<ArgumentException>();
                }
            }
        }

        [TestMethod]
        public void TestFromAndToJson()
        {
            for (int i = 0; i < 11; i++)
            {
                if (i == 0)
                {
                    ContractParameter contractParameter = new ContractParameter(ContractParameterType.Signature);
                    JObject jobject = contractParameter.ToJson();
                    Assert.AreEqual(jobject.ToString(), ContractParameter.FromJson(jobject).ToJson().ToString());
                }
                else if (i == 1)
                {
                    ContractParameter contractParameter = new ContractParameter(ContractParameterType.Boolean);
                    JObject jobject = contractParameter.ToJson();
                    Assert.AreEqual(jobject.ToString(), ContractParameter.FromJson(jobject).ToJson().ToString());
                }
                else if (i == 2)
                {
                    ContractParameter contractParameter = new ContractParameter(ContractParameterType.Integer);
                    JObject jobject = contractParameter.ToJson();
                    Assert.AreEqual(jobject.ToString(), ContractParameter.FromJson(jobject).ToJson().ToString());
                }
                else if (i == 3)
                {
                    ContractParameter contractParameter = new ContractParameter(ContractParameterType.Hash160);
                    JObject jobject = contractParameter.ToJson();
                    Assert.AreEqual(jobject.ToString(), ContractParameter.FromJson(jobject).ToJson().ToString());
                }
                else if (i == 4)
                {
                    ContractParameter contractParameter = new ContractParameter(ContractParameterType.Hash256);
                    JObject jobject = contractParameter.ToJson();
                    Assert.AreEqual(jobject.ToString(), ContractParameter.FromJson(jobject).ToJson().ToString());
                }
                else if (i == 5)
                {
                    ContractParameter contractParameter = new ContractParameter(ContractParameterType.ByteArray);
                    JObject jobject = contractParameter.ToJson();
                    Assert.AreEqual(jobject.ToString(), ContractParameter.FromJson(jobject).ToJson().ToString());
                }
                else if (i == 6)
                {
                    ContractParameter contractParameter = new ContractParameter(ContractParameterType.PublicKey);
                    JObject jobject = contractParameter.ToJson();
                    Assert.AreEqual(jobject.ToString(), ContractParameter.FromJson(jobject).ToJson().ToString());
                }
                else if (i == 7)
                {
                    ContractParameter contractParameter = new ContractParameter(ContractParameterType.String);
                    JObject jobject = contractParameter.ToJson();
                    Assert.AreEqual(jobject.ToString(), ContractParameter.FromJson(jobject).ToJson().ToString());
                }
                else if (i == 8)
                {
                    ContractParameter contractParameter = new ContractParameter(ContractParameterType.Array);
                    JObject jobject = contractParameter.ToJson();
                    Assert.AreEqual(jobject.ToString(), ContractParameter.FromJson(jobject).ToJson().ToString());
                }
                else if (i == 9)
                {
                    ContractParameter contractParameter = new ContractParameter(ContractParameterType.Map);
                    JObject jobject = contractParameter.ToJson();
                    Assert.AreEqual(jobject.ToString(), ContractParameter.FromJson(jobject).ToJson().ToString());
                }
                else
                {
                    ContractParameter contractParameter = new ContractParameter(ContractParameterType.String);
                    JObject jobject = contractParameter.ToJson();
                    jobject["type"] = "Void";
                    Action action = () => ContractParameter.FromJson(jobject);
                    action.ShouldThrow<ArgumentException>();
                }
            }
        }

        [TestMethod]
        public void TestSetValue()
        {
            for (int i = 0; i < 11; i++)
            {
                if (i == 0)
                {
                    ContractParameter contractParameter = new ContractParameter(ContractParameterType.Signature);
                    byte[] expectedArray = new byte[64];
                    contractParameter.SetValue(new byte[64].ToHexString());
                    Assert.AreEqual(Encoding.Default.GetString(expectedArray), Encoding.Default.GetString((byte[])contractParameter.Value));
                    Action action = () => contractParameter.SetValue(new byte[50].ToHexString());
                    action.ShouldThrow<FormatException>();
                }
                else if (i == 1)
                {
                    ContractParameter contractParameter = new ContractParameter(ContractParameterType.Boolean);
                    contractParameter.SetValue("true");
                    Assert.AreEqual(true, contractParameter.Value);
                }
                else if (i == 2)
                {
                    ContractParameter contractParameter = new ContractParameter(ContractParameterType.Integer);
                    contractParameter.SetValue("11");
                    Assert.AreEqual(new BigInteger(11), contractParameter.Value);
                }
                else if (i == 3)
                {
                    ContractParameter contractParameter = new ContractParameter(ContractParameterType.Hash160);
                    contractParameter.SetValue("0x0000000000000000000000000000000000000001");
                    Assert.AreEqual(UInt160.Parse("0x0000000000000000000000000000000000000001"), contractParameter.Value);
                }
                else if (i == 4)
                {
                    ContractParameter contractParameter = new ContractParameter(ContractParameterType.Hash256);
                    contractParameter.SetValue("0x0000000000000000000000000000000000000000000000000000000000000000");
                    Assert.AreEqual(UInt256.Parse("0x0000000000000000000000000000000000000000000000000000000000000000"), contractParameter.Value);
                }
                else if (i == 5)
                {
                    ContractParameter contractParameter = new ContractParameter(ContractParameterType.ByteArray);
                    contractParameter.SetValue("2222");
                    byte[] expectedArray = new byte[2];
                    expectedArray[0] = 0x22;
                    expectedArray[1] = 0x22;
                    Assert.AreEqual(Encoding.Default.GetString(expectedArray), Encoding.Default.GetString((byte[])contractParameter.Value));
                }
                else if (i == 6)
                {
                    ContractParameter contractParameter = new ContractParameter(ContractParameterType.PublicKey);
                    Random random = new Random();
                    byte[] privateKey = new byte[32];
                    for (int j = 0; j < privateKey.Length; j++)
                        privateKey[j] = (byte)random.Next(256);
                    ECPoint publicKey = ECCurve.Secp256r1.G * privateKey;
                    contractParameter.SetValue(publicKey.ToString());
                    Assert.AreEqual(true, publicKey.Equals(contractParameter.Value));
                }
                else if (i == 7)
                {
                    ContractParameter contractParameter = new ContractParameter(ContractParameterType.String);
                    contractParameter.SetValue("AAA");
                    Assert.AreEqual("AAA", contractParameter.Value);
                }
                else
                {
                    ContractParameter contractParameter = new ContractParameter(ContractParameterType.Array);
                    Action action = () => contractParameter.SetValue("AAA");
                    action.ShouldThrow<ArgumentException>();
                }
            }
        }

        [TestMethod]
        public void TestToString()
        {
            for (int i = 0; i < 5; i++)
            {
                if (i == 0)
                {
                    ContractParameter contractParameter = new ContractParameter();
                    Assert.AreEqual("(null)", contractParameter.ToString());
                }
                else if (i == 1)
                {
                    ContractParameter contractParameter = new ContractParameter(ContractParameterType.ByteArray);
                    contractParameter.Value = new byte[1];
                    Assert.AreEqual("00", contractParameter.ToString());
                }
                else if (i == 2)
                {
                    ContractParameter contractParameter = new ContractParameter(ContractParameterType.Array);
                    Assert.AreEqual("[]", contractParameter.ToString());
                    ContractParameter internalContractParameter = new ContractParameter(ContractParameterType.Boolean);
                    ((IList<ContractParameter>)contractParameter.Value).Add(internalContractParameter);
                    Assert.AreEqual("[False]", contractParameter.ToString());
                }
                else if (i == 3)
                {
                    ContractParameter contractParameter = new ContractParameter(ContractParameterType.Map);
                    Assert.AreEqual("[]", contractParameter.ToString());
                    ContractParameter internalContractParameter = new ContractParameter(ContractParameterType.Boolean);
                    ((IList<KeyValuePair<ContractParameter, ContractParameter>>)contractParameter.Value).Add(new KeyValuePair<ContractParameter, ContractParameter>(
                        internalContractParameter, internalContractParameter
                        ));
                    Assert.AreEqual("[{False,False}]", contractParameter.ToString());
                }
                else
                {
                    ContractParameter contractParameter = new ContractParameter(ContractParameterType.String);
                    Assert.AreEqual("", contractParameter.ToString());
                }
            }
        }
    }
}