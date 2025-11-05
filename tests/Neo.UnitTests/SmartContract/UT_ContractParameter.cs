// Copyright (C) 2015-2025 The Neo Project.
//
// UT_ContractParameter.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Cryptography.ECC;
using Neo.Extensions;
using Neo.Extensions.Factories;
using Neo.Json;
using Neo.SmartContract;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using System.Text.Json.Nodes;

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
            Assert.IsFalse((bool?)contractParameter2.Value);

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
            Assert.IsEmpty((List<ContractParameter>)contractParameter9.Value);

            ContractParameter contractParameter10 = new(ContractParameterType.Map);
            Assert.IsNotNull(contractParameter10);
            Assert.IsEmpty((List<KeyValuePair<ContractParameter, ContractParameter>>)contractParameter10.Value);

            Assert.ThrowsExactly<ArgumentException>(() => _ = new ContractParameter(ContractParameterType.Void));
        }

        [TestMethod]
        public void TestFromAndToJson()
        {
            ContractParameter contractParameter1 = new(ContractParameterType.Signature);
            JsonObject jobject1 = contractParameter1.ToJson();
            Assert.AreEqual(jobject1.ToString(false), ContractParameter.FromJson(jobject1).ToJson().ToString(false));

            ContractParameter contractParameter2 = new(ContractParameterType.Boolean);
            JsonObject jobject2 = contractParameter2.ToJson();
            Assert.AreEqual(jobject2.ToString(false), ContractParameter.FromJson(jobject2).ToJson().ToString(false));

            ContractParameter contractParameter3 = new(ContractParameterType.Integer);
            JsonObject jobject3 = contractParameter3.ToJson();
            Assert.AreEqual(jobject3.ToString(false), ContractParameter.FromJson(jobject3).ToJson().ToString(false));

            ContractParameter contractParameter4 = new(ContractParameterType.Hash160);
            JsonObject jobject4 = contractParameter4.ToJson();
            Assert.AreEqual(jobject4.ToString(false), ContractParameter.FromJson(jobject4).ToJson().ToString(false));

            ContractParameter contractParameter5 = new(ContractParameterType.Hash256);
            JsonObject jobject5 = contractParameter5.ToJson();
            Assert.AreEqual(jobject5.ToString(false), ContractParameter.FromJson(jobject5).ToJson().ToString(false));

            ContractParameter contractParameter6 = new(ContractParameterType.ByteArray);
            JsonObject jobject6 = contractParameter6.ToJson();
            Assert.AreEqual(jobject6.ToString(false), ContractParameter.FromJson(jobject6).ToJson().ToString(false));

            ContractParameter contractParameter7 = new(ContractParameterType.PublicKey);
            JsonObject jobject7 = contractParameter7.ToJson();
            Assert.AreEqual(jobject7.ToString(false), ContractParameter.FromJson(jobject7).ToJson().ToString(false));

            ContractParameter contractParameter8 = new(ContractParameterType.String);
            JsonObject jobject8 = contractParameter8.ToJson();
            Assert.AreEqual(jobject8.ToString(false), ContractParameter.FromJson(jobject8).ToJson().ToString(false));

            ContractParameter contractParameter9 = new(ContractParameterType.Array);
            JsonObject jobject9 = contractParameter9.ToJson();
            Assert.AreEqual(jobject9.ToString(false), ContractParameter.FromJson(jobject9).ToJson().ToString(false));

            ContractParameter contractParameter10 = new(ContractParameterType.Map);
            JsonObject jobject10 = contractParameter10.ToJson();
            Assert.AreEqual(jobject10.ToString(false), ContractParameter.FromJson(jobject10).ToJson().ToString(false));

            ContractParameter contractParameter11 = new(ContractParameterType.String);
            JsonObject jobject11 = contractParameter11.ToJson();
            jobject11["type"] = "Void";
            Assert.ThrowsExactly<ArgumentException>(() => _ = ContractParameter.FromJson(jobject11));
        }

        [TestMethod]
        public void TestContractParameterCyclicReference()
        {
            var map = new ContractParameter
            {
                Type = ContractParameterType.Map,
                Value = new List<KeyValuePair<ContractParameter, ContractParameter>>
                {
                    new(
                        new ContractParameter { Type = ContractParameterType.Integer, Value = 1 },
                        new ContractParameter { Type = ContractParameterType.Integer, Value = 2 }
                    )
                }
            };

            var value = new List<ContractParameter> { map, map };
            var item = new ContractParameter { Type = ContractParameterType.Array, Value = value };

            // just check there is no exception
            var json = item.ToJson();
            Assert.AreEqual(json.ToString(false), ContractParameter.FromJson(json).ToJson().ToString(false));

            // check cyclic reference
            value.Add(item);
            Assert.ThrowsExactly<InvalidOperationException>(() => _ = item.ToJson());
        }

        [TestMethod]
        public void TestSetValue()
        {
            ContractParameter contractParameter1 = new(ContractParameterType.Signature);
            byte[] expectedArray1 = new byte[64];
            contractParameter1.SetValue(new byte[64].ToHexString());
            Assert.AreEqual(Encoding.Default.GetString(expectedArray1), Encoding.Default.GetString((byte[])contractParameter1.Value));
            Assert.ThrowsExactly<FormatException>(() => contractParameter1.SetValue(new byte[50].ToHexString()));

            ContractParameter contractParameter2 = new(ContractParameterType.Boolean);
            contractParameter2.SetValue("true");
            Assert.IsTrue((bool?)contractParameter2.Value);

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
            byte[] privateKey7 = new byte[32];
            for (int j = 0; j < privateKey7.Length; j++)
                privateKey7[j] = RandomNumberFactory.NextByte();
            ECPoint publicKey7 = ECCurve.Secp256r1.G * privateKey7;
            contractParameter7.SetValue(publicKey7.ToString());
            Assert.IsTrue(publicKey7.Equals(contractParameter7.Value));

            ContractParameter contractParameter8 = new(ContractParameterType.String);
            contractParameter8.SetValue("AAA");
            Assert.AreEqual("AAA", contractParameter8.Value);

            ContractParameter contractParameter9 = new(ContractParameterType.Array);
            Assert.ThrowsExactly<ArgumentException>(() => contractParameter9.SetValue("AAA"));
        }

        [TestMethod]
        public void TestToString()
        {
            ContractParameter contractParameter1 = new();
            Assert.AreEqual("(null)", contractParameter1.ToString());

            ContractParameter contractParameter2 = new(ContractParameterType.ByteArray)
            {
                Value = new byte[1]
            };
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
