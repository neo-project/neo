using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Core;
using Neo.IO.Json;

namespace Neo.UnitTests
{
    [TestClass]
    public class UT_FunctionCode
    {
        FunctionCode uut;

        [TestInitialize]
        public void TestSetup()
        {
            uut = new FunctionCode();
        }

        [TestMethod]
        public void Script_Get()
        {
            uut.Script.Should().BeNull();
        }

        [TestMethod]
        public void Script_Set()
        {
            byte[] val = TestUtils.GetByteArray(32, 0x42);
            uut.Script = val;
            uut.Script.Length.Should().Be(val.Length);
            for (int i = 0; i < uut.Script.Length; i++)
            {
                uut.Script[i].Should().Be(val[i]);
            }
        }

        [TestMethod]
        public void ParameterList_Get()
        {
            uut.ParameterList.Should().BeNull();
        }

        [TestMethod]
        public void ParameterList_Set()
        {
            ContractParameterType item = new ContractParameterType();
            ContractParameterType[] val = new[] { item };
            uut.ParameterList = val;
            uut.ParameterList.Length.Should().Be(val.Length);
            uut.ParameterList[0].Should().Be(item);
        }

        [TestMethod]
        public void ReturnType_Get()
        {
            uut.ReturnType.Should().Be(ContractParameterType.Signature);
        }

        [TestMethod]
        public void ReturnType_Set()
        {
            ContractParameterType val = ContractParameterType.Hash256;
            uut.ReturnType = val;
            uut.ReturnType.Should().Be(val);
        }

        [TestMethod]
        public void ScriptHash_Get()
        {            
            uut.Script = TestUtils.GetByteArray(32, 0x42);
            Byte[] hash = new Byte[] { 169, 124, 37, 249, 223, 124, 86, 186, 252, 41, 86, 241, 125, 22, 82, 214, 131, 49, 241, 61 };
            uut.ScriptHash.Should().Be(new UInt160(hash));
        }

        [TestMethod]
        public void Size_Get_1_Parameter()
        {
            uut.Script = TestUtils.GetByteArray(32, 0x42);
            ContractParameterType item = new ContractParameterType();
            ContractParameterType[] val = new[] { item };
            uut.ParameterList = val;

            uut.Size.Should().Be(36); // 33 + 2 + 1
        }

        [TestMethod]
        public void Size_Get_2_Parameter()
        {
            uut.Script = TestUtils.GetByteArray(32, 0x42);
            ContractParameterType item = new ContractParameterType();
            ContractParameterType[] val = new[] { item, item };
            uut.ParameterList = val;

            uut.Size.Should().Be(37); // 33 + 3 + 1
        }

        [TestMethod]
        public void ToJson()
        {
            uut.Script = TestUtils.GetByteArray(32, 0x42);
            ContractParameterType item = ContractParameterType.Boolean;
            ContractParameterType[] val = new[] { item };
            uut.ParameterList = val;
            uut.ReturnType = ContractParameterType.Hash256;

            JObject jObj = uut.ToJson();
            jObj.Should().NotBeNull();
            jObj["hash"].AsString().Should().Be("3df13183d652167df15629fcba567cdff9257ca9");
            jObj["script"].AsString().Should().Be("4220202020202020202020202020202020202020202020202020202020202020");            
            JArray pObj = (JArray)jObj["parameters"];
            pObj.Count.Should().Be(1);
            pObj[0].AsString().Should().Be("Boolean");
            jObj["returntype"].AsString().Should().Be("Hash256");
        }
    }
}
