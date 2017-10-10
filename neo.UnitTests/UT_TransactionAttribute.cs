using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Core;
using Neo.IO.Json;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Neo.UnitTests
{
    [TestClass]
    public class UT_TransactionAttribute
    {
        TransactionAttribute uut;

        [TestInitialize]
        public void TestSetup()
        {
            uut = new TransactionAttribute();
        }

        [TestMethod]
        public void Usage_Get()
        {
            uut.Usage.Should().Be(TransactionAttributeUsage.ContractHash);
        }

        [TestMethod]
        public void Usage_Set()
        {
            uut.Usage = TransactionAttributeUsage.ECDH02;
            uut.Usage.Should().Be(TransactionAttributeUsage.ECDH02);
        }

        [TestMethod]
        public void Data_Get()
        {
            uut.Data.Should().BeNull();
        }

        [TestMethod]
        public void Data_Set()
        {
            byte[] val = new byte[] { 0x42, 0x32 };
            uut.Data = val;
            uut.Data.Length.Should().Be(2);
            uut.Data[0].Should().Be(val[0]);
            uut.Data[1].Should().Be(val[1]);
        }

        [TestMethod]
        public void Size_Get_ContractHash()
        {
            uut.Usage = TransactionAttributeUsage.ContractHash;
            uut.Size.Should().Be(33); // 1 + 32
        }

        [TestMethod]
        public void Size_Get_ECDH02()
        {
            uut.Usage = TransactionAttributeUsage.ECDH02;
            uut.Size.Should().Be(33); // 1 + 32
        }

        [TestMethod]
        public void Size_Get_ECDH03()
        {
            uut.Usage = TransactionAttributeUsage.ECDH03;
            uut.Size.Should().Be(33); // 1 + 32
        }

        [TestMethod]
        public void Size_Get_Vote()
        {
            uut.Usage = TransactionAttributeUsage.Vote;
            uut.Size.Should().Be(33); // 1 + 32
        }

        [TestMethod]
        public void Size_Get_Hash()
        {
            for (TransactionAttributeUsage i = TransactionAttributeUsage.Hash1; i <= TransactionAttributeUsage.Hash15; i++)
            {
                uut.Usage = i;
                uut.Size.Should().Be(33); // 1 + 32
            }
        }

        [TestMethod]
        public void Size_Get_Script()
        {
            uut.Usage = TransactionAttributeUsage.Script;
            uut.Size.Should().Be(21); // 1 + 20
        }

        [TestMethod]
        public void Size_Get_DescriptionUrl()
        {
            uut.Usage = TransactionAttributeUsage.DescriptionUrl;
            uut.Data = TestUtils.GetByteArray(10, 0x42);
            uut.Size.Should().Be(12); // 1 + 1 + 10
        }

        [TestMethod]
        public void Size_Get_OtherAttribute()
        {
            uut.Usage = TransactionAttributeUsage.Remark;
            uut.Data = TestUtils.GetByteArray(10, 0x42);
            uut.Size.Should().Be(12); // 1 + 11
        }

        [TestMethod]
        public void ToJson()
        {
            uut.Usage = TransactionAttributeUsage.ECDH02;
            uut.Data = TestUtils.GetByteArray(10, 0x42);

            JObject jObj = uut.ToJson();
            jObj.Should().NotBeNull();
            jObj["usage"].AsString().Should().Be("ECDH02");
            jObj["data"].AsString().Should().Be("42202020202020202020");           
        }
    }
}
