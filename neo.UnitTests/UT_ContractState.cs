using System;
using Neo.Core;
using Neo.Cryptography.ECC;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using Microsoft.AspNetCore.Http.Features;
using Neo.IO.Json;

namespace Neo.UnitTests
{
    [TestClass]
    public class UT_ContractState
    {
        ContractState uut;

        [TestInitialize]
        public void TestSetup()
        {
            uut = new ContractState();
        }

        [TestMethod]
        public void FunctionCode_Get()
        {
            uut.Code.Should().BeNull();
        }

        [TestMethod]
        public void FunctionCode_Set()
        {
            FunctionCode val = new FunctionCode();
            uut.Code = val;
            uut.Code.Should().Be(val);
        }

        [TestMethod]
        public void HasStorage_Get()
        {
            uut.HasStorage.Should().BeFalse();
        }

        [TestMethod]
        public void HasStorage_Set()
        {
            uut.HasStorage = true;
            uut.HasStorage.Should().BeTrue();
        }

        [TestMethod]
        public void Name_Get()
        {
            uut.Name.Should().BeNull();
        }

        [TestMethod]
        public void Name_Set()
        {
            uut.Name = "Mr Anderson";
            uut.Name.Should().Be("Mr Anderson");
        }

        [TestMethod]
        public void CodeVersion_Get()
        {
            uut.CodeVersion.Should().BeNull();
        }

        [TestMethod]
        public void CodeVersion_Set()
        {
            uut.CodeVersion = "v0.1.42";
            uut.CodeVersion.Should().Be("v0.1.42");
        }

        [TestMethod]
        public void Author_Get()
        {
            uut.Author.Should().BeNull();
        }

        [TestMethod]
        public void Author_Set()
        {
            uut.Author = "Wachowski";
            uut.Author.Should().Be("Wachowski");
        }

        [TestMethod]
        public void Email_Get()
        {
            uut.Email.Should().BeNull();
        }

        [TestMethod]
        public void Email_Set()
        {
            uut.Email = "neo@cityofzion.org";
            uut.Email.Should().Be("neo@cityofzion.org");
        }

        [TestMethod]
        public void Description_Get()
        {
            uut.Description.Should().BeNull();
        }

        [TestMethod]
        public void Description_Set()
        {
            uut.Description = "Test";
            uut.Description.Should().Be("Test");
        }

        [TestMethod]
        public void ScriptHash_Get()
        {
            FunctionCode fc = new FunctionCode();
            fc.Script = TestUtils.GetByteArray(32, 0x42);
            uut.Code = fc;
            Byte[] hash = new Byte[] { 169, 124, 37, 249, 223, 124, 86, 186, 252, 41, 86, 241, 125, 22, 82, 214, 131, 49, 241, 61 };            
            uut.ScriptHash.Should().Be(new UInt160(hash));
        }

        private void setupContractStateWithValues(ContractState cs, out FunctionCode code, out bool hasStorage, out string name, out string codeVersion, out string author, out string email, out string description)
        {
            code = new FunctionCode();
            code.Script = TestUtils.GetByteArray(32, 0x42);
            code.ParameterList = new[] { new ContractParameterType() };            
            cs.Code = code;

            hasStorage = true;
            cs.HasStorage = hasStorage;

            name = "nameStr";
            cs.Name = name;

            codeVersion = "codeVersionStr";
            cs.CodeVersion = codeVersion;

            author = "authorStr";
            cs.Author = author;

            email = "emailStr";
            cs.Email = email;

            description = "descriptionStr";
            cs.Description = description;
        }

        [TestMethod]
        public void Size_Get()
        {
            FunctionCode code;
            bool hasStorage;
            string name;
            string codeVersion;
            string author;
            string email;
            string description;

            setupContractStateWithValues(uut, out code, out hasStorage, out name, out codeVersion, out author, out email, out description);

            uut.Size.Should().Be(95); // 1 + 36 + 1 + 8 + 15 + 10 + 9 + 15
        }

        [TestMethod]
        public void Clone()
        {
            FunctionCode code;
            bool hasStorage;
            string name;
            string codeVersion;
            string author;
            string email;
            string description;
            setupContractStateWithValues(uut, out code, out hasStorage, out name, out codeVersion, out author, out email, out description);

            ContractState newCs = ((ICloneable<ContractState>)uut).Clone();
            newCs.Code.Should().Be(code);
            newCs.HasStorage.Should().Be(hasStorage);
            newCs.CodeVersion.Should().Be(codeVersion);
            newCs.Author.Should().Be(author);
            newCs.Email.Should().Be(email);
            newCs.Description.Should().Be(description);
        }

        [TestMethod]
        public void Deserialize()
        {
            FunctionCode code;
            bool hasStorage;
            string name;
            string codeVersion;
            string author;
            string email;
            string description;
            setupContractStateWithValues(new ContractState(), out code, out hasStorage, out name, out codeVersion, out author, out email, out description);

            byte[] data = new byte[] { 0, 32, 66, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 1, 0, 0, 1, 7, 110, 97, 109, 101, 83, 116, 114, 14, 99, 111, 100, 101, 86, 101, 114, 115, 105, 111, 110, 83, 116, 114, 9, 97, 117, 116, 104, 111, 114, 83, 116, 114, 8, 101, 109, 97, 105, 108, 83, 116, 114, 14, 100, 101, 115, 99, 114, 105, 112, 116, 105, 111, 110, 83, 116, 114 };
            int index = 0;
            using (MemoryStream ms = new MemoryStream(data, index, data.Length - index, false))
            {
                using (BinaryReader reader = new BinaryReader(ms))
                {
                    uut.Deserialize(reader);
                }
            }
            uut.Code.ParameterList.Length.Should().Be(code.ParameterList.Length);
            for (int i = 0; i < uut.Code.ParameterList.Length; i++)
            {
                uut.Code.ParameterList[i].Should().Be(code.ParameterList[i]);
            }
            uut.Code.Script.Length.Should().Be(code.Script.Length);
            for (int i = 0; i < uut.Code.Script.Length; i++)
            {
                uut.Code.Script[i].Should().Be(code.Script[i]);
            }
            uut.Code.ReturnType.Should().Be(code.ReturnType);
            uut.HasStorage.Should().Be(hasStorage);
            uut.CodeVersion.Should().Be(codeVersion);
            uut.Author.Should().Be(author);
            uut.Email.Should().Be(email);
            uut.Description.Should().Be(description);
        }

        [TestMethod]
        public void FromReplica()
        {
            ContractState cs = new ContractState();
            FunctionCode code;
            bool hasStorage;
            string name;
            string codeVersion;
            string author;
            string email;
            string description;
            setupContractStateWithValues(cs, out code, out hasStorage, out name, out codeVersion, out author, out email, out description);

            ((ICloneable<ContractState>)uut).FromReplica(cs);
            uut.Code.Should().Be(code);
            uut.HasStorage.Should().Be(hasStorage);
            uut.CodeVersion.Should().Be(codeVersion);
            uut.Author.Should().Be(author);
            uut.Email.Should().Be(email);
            uut.Description.Should().Be(description);
        }

        [TestMethod]
        public void Serialize()
        {
            FunctionCode code;
            bool hasStorage;
            string name;
            string codeVersion;
            string author;
            string email;
            string description;
            setupContractStateWithValues(uut, out code, out hasStorage, out name, out codeVersion, out author, out email, out description);

            byte[] data;
            using (MemoryStream stream = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(stream, Encoding.ASCII, true))
                {
                    uut.Serialize(writer);
                    data = stream.ToArray();
                }
            }

            byte[] requiredData = new byte[] { 0, 32, 66, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 32, 1, 0, 0, 1, 7, 110, 97, 109, 101, 83, 116, 114, 14, 99, 111, 100, 101, 86, 101, 114, 115, 105, 111, 110, 83, 116, 114, 9, 97, 117, 116, 104, 111, 114, 83, 116, 114, 8, 101, 109, 97, 105, 108, 83, 116, 114, 14, 100, 101, 115, 99, 114, 105, 112, 116, 105, 111, 110, 83, 116, 114 };

            data.Length.Should().Be(95);
            for (int i = 0; i < 95; i++)
            {
                data[i].Should().Be(requiredData[i]);
            }
        }

        [TestMethod]
        public void ToJson()
        {
            FunctionCode code;
            bool hasStorage;
            string name;
            string codeVersion;
            string author;
            string email;
            string description;
            setupContractStateWithValues(uut, out code, out hasStorage, out name, out codeVersion, out author,
                out email, out description);

            JObject jObj = uut.ToJson();
            jObj.Should().NotBeNull();
            jObj["version"].AsNumber().Should().Be(0);
            jObj["storage"].AsBoolean().Should().Be(hasStorage);
            jObj["name"].AsString().Should().Be(name);
            jObj["code_version"].AsString().Should().Be(codeVersion);
            jObj["author"].AsString().Should().Be(author);
            jObj["email"].AsString().Should().Be(email);
            jObj["description"].AsString().Should().Be(description);
            jObj["code"].Should().NotBeNull(); // will be tested in Function Code tests
        }
    }
}
