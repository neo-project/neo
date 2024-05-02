// Copyright (C) 2015-2024 The Neo Project.
//
// UT_NEP6Contract.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
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
            var nep6Contract = NEP6Contract.FromJson(null);
            nep6Contract.Should().BeNull();
        }

        [TestMethod]
        public void TestFromJson()
        {
            var json = "{\"script\":\"IQPviR30wLfu+5N9IeoPuIzejg2Cp/8RhytecEeWna+062h0dHaq\"," +
                "\"parameters\":[{\"name\":\"signature\",\"type\":\"Signature\"}],\"deployed\":false}";
            var @object = (JObject)JToken.Parse(json);

            var nep6Contract = NEP6Contract.FromJson(@object);
            nep6Contract.Script.Should().BeEquivalentTo("2103ef891df4c0b7eefb937d21ea0fb88cde8e0d82a7ff11872b5e7047969dafb4eb68747476aa".HexToBytes());
            nep6Contract.ParameterList.Length.Should().Be(1);
            nep6Contract.ParameterList[0].Should().Be(ContractParameterType.Signature);
            nep6Contract.ParameterNames.Length.Should().Be(1);
            nep6Contract.ParameterNames[0].Should().Be("signature");
            nep6Contract.Deployed.Should().BeFalse();
        }

        [TestMethod]
        public void TestToJson()
        {
            NEP6Contract nep6Contract = new()
            {
                Script = [0x00, 0x01],
                ParameterList = [ContractParameterType.Boolean, ContractParameterType.Integer],
                ParameterNames = ["param1", "param2"],
                Deployed = false
            };

            var @object = nep6Contract.ToJson();
            var jString = (JString)@object["script"];
            jString.Value.Should().Be(Convert.ToBase64String(nep6Contract.Script, Base64FormattingOptions.None));

            var jBoolean = (JBoolean)@object["deployed"];
            jBoolean.Value.Should().BeFalse();

            var parameters = (JArray)@object["parameters"];
            parameters.Count.Should().Be(2);

            jString = (JString)parameters[0]["name"];
            jString.Value.Should().Be("param1");
            jString = (JString)parameters[0]["type"];
            jString.Value.Should().Be(ContractParameterType.Boolean.ToString());

            jString = (JString)parameters[1]["name"];
            jString.Value.Should().Be("param2");
            jString = (JString)parameters[1]["type"];
            jString.Value.Should().Be(ContractParameterType.Integer.ToString());
        }
    }
}
