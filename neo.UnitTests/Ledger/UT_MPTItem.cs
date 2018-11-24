using System.IO;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Cryptography;
using Neo.Ledger;

namespace Neo.UnitTests.Ledger
{
    [TestClass]
    public class UT_MPTItem
    {
        private MPTItem mptItem;

        [TestInitialize]
        public void TestSetup()
        {
            mptItem = new MPTItem();
        }

        [TestMethod]
        public void Serialize_Leaf()
        {
            mptItem.NodeType = MPTItem.MPTNodeType.Leaf;
            mptItem.Path = "2";
            mptItem.Value = "abc";
            mptItem.KeyHash = new UInt256(System.Text.Encoding.UTF8.GetBytes("oi").Sha256());
            mptItem.Hashes = new UInt256[0];
            
            using (var ms = new MemoryStream())
            {
                using (var bw = new BinaryWriter(ms))
                {
                    mptItem.Serialize(bw);
                    using (var br = new BinaryReader(bw.BaseStream))
                    {
                        br.BaseStream.Position = 0;
                        var mptResposta = new MPTItem();
                        mptResposta.Deserialize(br);

                        mptResposta.NodeType.Should().Be(mptItem.NodeType);
                        mptResposta.Path.Should().Be(mptItem.Path);
                        mptResposta.Value.Should().Be(mptItem.Value);
                        mptResposta.KeyHash.Should().Be(mptItem.KeyHash);
                    }
                }
            }
        }
    }
}