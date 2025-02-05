// Copyright (C) 2015-2025 The Neo Project.
//
// UT_NEP6Contract.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the repository
// or https://opensource.org/license/mit for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Extensions;
using Neo.Json;
using Neo.SmartContract;
using Neo.Wallets.NEP6;
using System;

namespace Neo.UnitTests.Wallets.NEP6
{
    [TestClass]
    public class UT_NEP6Contract
    {
        [TestMethod]
        public void TestFromNullJson()
        {
            NEP6Contract nep6Contract = NEP6Contract.FromJson(null);
            Assert.IsNull(nep6Contract);
        }

        [TestMethod]
        public void TestFromJson()
        {
            string json = "{\"script\":\"IQPviR30wLfu+5N9IeoPuIzejg2Cp/8RhytecEeWna+062h0dHaq\"," +
                "\"parameters\":[{\"name\":\"signature\",\"type\":\"Signature\"}],\"deployed\":false}";
            JObject @object = (JObject)JToken.Parse(json);

            NEP6Contract nep6Contract = NEP6Contract.FromJson(@object);
            CollectionAssert.AreEqual("2103ef891df4c0b7eefb937d21ea0fb88cde8e0d82a7ff11872b5e7047969dafb4eb68747476aa".HexToBytes(), nep6Contract.Script);
            Assert.AreEqual(1, nep6Contract.ParameterList.Length);
            Assert.AreEqual(ContractParameterType.Signature, nep6Contract.ParameterList[0]);
            Assert.AreEqual(1, nep6Contract.ParameterNames.Length);
            Assert.AreEqual("signature", nep6Contract.ParameterNames[0]);
            Assert.IsFalse(nep6Contract.Deployed);
        }

        [TestMethod]
        public void TestToJson()
        {
            NEP6Contract nep6Contract = new()
            {
                Script = new byte[] { 0x00, 0x01 },
                ParameterList = new ContractParameterType[] { ContractParameterType.Boolean, ContractParameterType.Integer },
                ParameterNames = new string[] { "param1", "param2" },
                Deployed = false
            };

            JObject @object = nep6Contract.ToJson();
            JString jString = (JString)@object["script"];
            Assert.AreEqual(Convert.ToBase64String(nep6Contract.Script, Base64FormattingOptions.None), jString.Value);

            JBoolean jBoolean = (JBoolean)@object["deployed"];
            Assert.IsFalse(jBoolean.Value);

            JArray parameters = (JArray)@object["parameters"];
            Assert.AreEqual(2, parameters.Count);

            jString = (JString)parameters[0]["name"];
            Assert.AreEqual("param1", jString.Value);
            jString = (JString)parameters[0]["type"];
            Assert.AreEqual(ContractParameterType.Boolean.ToString(), jString.Value);

            jString = (JString)parameters[1]["name"];
            Assert.AreEqual("param2", jString.Value);
            jString = (JString)parameters[1]["type"];
            Assert.AreEqual(ContractParameterType.Integer.ToString(), jString.Value);
        }
    }
}
