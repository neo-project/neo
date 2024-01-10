// Copyright (C) 2015-2024 The Neo Project.
//
// UT_ContractParameter.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Cryptography.ECC;
using Neo.Json;
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
            ContractParameter contractParameter = new();
            Assert.IsNotNull(contractParameter);
        }

        [TestMethod]
        public void TestGenerator2()
        {
            ContractParameter contractParameter1 = new(ContractParameterType.Signature);
            byte[] expectedArray1 = new byte[64];
            Assert.IsNotNull(contractParameter1);
            Assert.AreEqual(Encoding.Default.GetString(expectedArray1), Encoding.Default.GetString((byte[])contractParameter1.Value));

            ContractParameter contractParameter2 = new(ContractParameterType.Boolean);
            Assert.IsNotNull(contractParameter2);
            Assert.AreEqual(false, contractParameter2.Value);

            ContractParameter contractParameter3 = new(ContractParameterType.Integer);
            Assert.IsNotNull(contractParameter3);
            Assert.AreEqual(0, contractParameter3.Value);

            ContractParameter contractParameter4 = new(ContractParameterType.Hash160);
            Assert.IsNotNull(contractParameter4);
            Assert.AreEqual(new UInt160(), contractParameter4.Value);

            ContractParameter contractParameter5 = new(ContractParameterType.Hash256);
            Assert.IsNotNull(contractParameter5);
            Assert.AreEqual(new UInt256(), contractParameter5.Value);

            ContractParameter contractParameter6 = new(ContractParameterType.ByteArray);
            byte[] expectedArray6 = Array.Empty<byte>();
            Assert.IsNotNull(contractParameter6);
            Assert.AreEqual(Encoding.Default.GetString(expectedArray6), Encoding.Default.GetString((byte[])contractParameter6.Value));

            ContractParameter contractParameter7 = new(ContractParameterType.PublicKey);
            Assert.IsNotNull(contractParameter7);
            Assert.AreEqual(ECCurve.Secp256r1.G, contractParameter7.Value);

            ContractParameter contractParameter8 = new(ContractParameterType.String);
            Assert.IsNotNull(contractParameter8);
            Assert.AreEqual("", contractParameter8.Value);

            ContractParameter contractParameter9 = new(ContractParameterType.Array);
            Assert.IsNotNull(contractParameter9);
            Assert.AreEqual(0, ((List<ContractParameter>)contractParameter9.Value).Count);

            ContractParameter contractParameter10 = new(ContractParameterType.Map);
            Assert.IsNotNull(contractParameter10);
            Assert.AreEqual(0, ((List<KeyValuePair<ContractParameter, ContractParameter>>)contractParameter10.Value).Count);

            Action action = () => new ContractParameter(ContractParameterType.Void);
            action.Should().Throw<ArgumentException>();
        }

        [TestMethod]
        public void TestFromAndToJson()
        {
            ContractParameter contractParameter1 = new(ContractParameterType.Signature);
            JObject jobject1 = contractParameter1.ToJson();
            Assert.AreEqual(jobject1.ToString(), ContractParameter.FromJson(jobject1).ToJson().ToString());

            ContractParameter contractParameter2 = new(ContractParameterType.Boolean);
            JObject jobject2 = contractParameter2.ToJson();
            Assert.AreEqual(jobject2.ToString(), ContractParameter.FromJson(jobject2).ToJson().ToString());

            ContractParameter contractParameter3 = new(ContractParameterType.Integer);
            JObject jobject3 = contractParameter3.ToJson();
            Assert.AreEqual(jobject3.ToString(), ContractParameter.FromJson(jobject3).ToJson().ToString());

            ContractParameter contractParameter4 = new(ContractParameterType.Hash160);
            JObject jobject4 = contractParameter4.ToJson();
            Assert.AreEqual(jobject4.ToString(), ContractParameter.FromJson(jobject4).ToJson().ToString());

            ContractParameter contractParameter5 = new(ContractParameterType.Hash256);
            JObject jobject5 = contractParameter5.ToJson();
            Assert.AreEqual(jobject5.ToString(), ContractParameter.FromJson(jobject5).ToJson().ToString());

            ContractParameter contractParameter6 = new(ContractParameterType.ByteArray);
            JObject jobject6 = contractParameter6.ToJson();
            Assert.AreEqual(jobject6.ToString(), ContractParameter.FromJson(jobject6).ToJson().ToString());

            ContractParameter contractParameter7 = new(ContractParameterType.PublicKey);
            JObject jobject7 = contractParameter7.ToJson();
            Assert.AreEqual(jobject7.ToString(), ContractParameter.FromJson(jobject7).ToJson().ToString());

            ContractParameter contractParameter8 = new(ContractParameterType.String);
            JObject jobject8 = contractParameter8.ToJson();
            Assert.AreEqual(jobject8.ToString(), ContractParameter.FromJson(jobject8).ToJson().ToString());

            ContractParameter contractParameter9 = new(ContractParameterType.Array);
            JObject jobject9 = contractParameter9.ToJson();
            Assert.AreEqual(jobject9.ToString(), ContractParameter.FromJson(jobject9).ToJson().ToString());

            ContractParameter contractParameter10 = new(ContractParameterType.Map);
            JObject jobject10 = contractParameter10.ToJson();
            Assert.AreEqual(jobject10.ToString(), ContractParameter.FromJson(jobject10).ToJson().ToString());

            ContractParameter contractParameter11 = new(ContractParameterType.String);
            JObject jobject11 = contractParameter11.ToJson();
            jobject11["type"] = "Void";
            Action action = () => ContractParameter.FromJson(jobject11);
            action.Should().Throw<ArgumentException>();
        }

        [TestMethod]
        public void TestSetValue()
        {
            ContractParameter contractParameter1 = new(ContractParameterType.Signature);
            byte[] expectedArray1 = new byte[64];
            contractParameter1.SetValue(new byte[64].ToHexString());
            Assert.AreEqual(Encoding.Default.GetString(expectedArray1), Encoding.Default.GetString((byte[])contractParameter1.Value));
            Action action1 = () => contractParameter1.SetValue(new byte[50].ToHexString());
            action1.Should().Throw<FormatException>();

            ContractParameter contractParameter2 = new(ContractParameterType.Boolean);
            contractParameter2.SetValue("true");
            Assert.AreEqual(true, contractParameter2.Value);

            ContractParameter contractParameter3 = new(ContractParameterType.Integer);
            contractParameter3.SetValue("11");
            Assert.AreEqual(new BigInteger(11), contractParameter3.Value);

            ContractParameter contractParameter4 = new(ContractParameterType.Hash160);
            contractParameter4.SetValue("0x0000000000000000000000000000000000000001");
            Assert.AreEqual(UInt160.Parse("0x0000000000000000000000000000000000000001"), contractParameter4.Value);

            ContractParameter contractParameter5 = new(ContractParameterType.Hash256);
            contractParameter5.SetValue("0x0000000000000000000000000000000000000000000000000000000000000000");
            Assert.AreEqual(UInt256.Parse("0x0000000000000000000000000000000000000000000000000000000000000000"), contractParameter5.Value);

            ContractParameter contractParameter6 = new(ContractParameterType.ByteArray);
            contractParameter6.SetValue("2222");
            byte[] expectedArray6 = new byte[2];
            expectedArray6[0] = 0x22;
            expectedArray6[1] = 0x22;
            Assert.AreEqual(Encoding.Default.GetString(expectedArray6), Encoding.Default.GetString((byte[])contractParameter6.Value));

            ContractParameter contractParameter7 = new(ContractParameterType.PublicKey);
            Random random7 = new();
            byte[] privateKey7 = new byte[32];
            for (int j = 0; j < privateKey7.Length; j++)
                privateKey7[j] = (byte)random7.Next(256);
            ECPoint publicKey7 = ECCurve.Secp256r1.G * privateKey7;
            contractParameter7.SetValue(publicKey7.ToString());
            Assert.AreEqual(true, publicKey7.Equals(contractParameter7.Value));

            ContractParameter contractParameter8 = new(ContractParameterType.String);
            contractParameter8.SetValue("AAA");
            Assert.AreEqual("AAA", contractParameter8.Value);

            ContractParameter contractParameter9 = new(ContractParameterType.Array);
            Action action9 = () => contractParameter9.SetValue("AAA");
            action9.Should().Throw<ArgumentException>();
        }

        [TestMethod]
        public void TestToString()
        {
            ContractParameter contractParameter1 = new();
            Assert.AreEqual("(null)", contractParameter1.ToString());

            ContractParameter contractParameter2 = new(ContractParameterType.ByteArray);
            contractParameter2.Value = new byte[1];
            Assert.AreEqual("00", contractParameter2.ToString());

            ContractParameter contractParameter3 = new(ContractParameterType.Array);
            Assert.AreEqual("[]", contractParameter3.ToString());
            ContractParameter internalContractParameter3 = new(ContractParameterType.Boolean);
            ((IList<ContractParameter>)contractParameter3.Value).Add(internalContractParameter3);
            Assert.AreEqual("[False]", contractParameter3.ToString());

            ContractParameter contractParameter4 = new(ContractParameterType.Map);
            Assert.AreEqual("[]", contractParameter4.ToString());
            ContractParameter internalContractParameter4 = new(ContractParameterType.Boolean);
            ((IList<KeyValuePair<ContractParameter, ContractParameter>>)contractParameter4.Value).Add(new KeyValuePair<ContractParameter, ContractParameter>(
                internalContractParameter4, internalContractParameter4
                ));
            Assert.AreEqual("[{False,False}]", contractParameter4.ToString());

            ContractParameter contractParameter5 = new(ContractParameterType.String);
            Assert.AreEqual("", contractParameter5.ToString());
        }
    }
}
