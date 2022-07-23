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
            NEP6Contract nep6Contract = NEP6Contract.FromJson(null);
            nep6Contract.Should().BeNull();
        }

        [TestMethod]
        public void TestFromJson()
        {
            string json = "{\"script\":\"IQPviR30wLfu+5N9IeoPuIzejg2Cp/8RhytecEeWna+062h0dHaq\"," +
                "\"parameters\":[{\"name\":\"signature\",\"type\":\"Signature\"}],\"deployed\":false}";
            JObject @object = (JObject)JToken.Parse(json);

            NEP6Contract nep6Contract = NEP6Contract.FromJson(@object);
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
                Script = new byte[] { 0x00, 0x01 },
                ParameterList = new ContractParameterType[] { ContractParameterType.Boolean, ContractParameterType.Integer },
                ParameterNames = new string[] { "param1", "param2" },
                Deployed = false
            };

            JObject @object = nep6Contract.ToJson();
            JString jString = (JString)@object["script"];
            jString.Value.Should().Be(Convert.ToBase64String(nep6Contract.Script, Base64FormattingOptions.None));

            JBoolean jBoolean = (JBoolean)@object["deployed"];
            jBoolean.Value.Should().BeFalse();

            JArray parameters = (JArray)@object["parameters"];
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
