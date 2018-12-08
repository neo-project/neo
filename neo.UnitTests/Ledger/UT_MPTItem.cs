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
        private MPTItem _mptItem;

        [TestInitialize]
        public void TestSetup()
        {
            _mptItem = new MPTItem();
        }

        [TestMethod]
        public void Serialize_Leaf()
        {
            _mptItem.NodeType = MPTItem.MPTNodeType.Leaf;
            _mptItem.Path = "2";
            _mptItem.Value = "abc";
            _mptItem.KeyHash = new UInt256(System.Text.Encoding.UTF8.GetBytes("oi").Sha256());
            _mptItem.Hashes = new UInt256[0];
            
            using (var ms = new MemoryStream())
            {
                using (var bw = new BinaryWriter(ms))
                {
                    _mptItem.Serialize(bw);
                    using (var br = new BinaryReader(bw.BaseStream))
                    {
                        br.BaseStream.Position = 0;
                        var mptResposta = new MPTItem();
                        mptResposta.Deserialize(br);

                        mptResposta.NodeType.Should().Be(_mptItem.NodeType);
                        mptResposta.Path.Should().Be(_mptItem.Path);
                        mptResposta.Value.Should().Be(_mptItem.Value);
                        mptResposta.KeyHash.Should().Be(_mptItem.KeyHash);
                    }
                }
            }
        }
    }
}