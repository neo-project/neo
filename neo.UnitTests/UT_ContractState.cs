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
    }
}
