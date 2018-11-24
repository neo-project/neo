using System.IO;
using System.Text;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Cryptography;
using Neo.Ledger;

namespace Neo.UnitTests.Ledger
{
    [TestClass]
    public class UT_MerklePatriciaNode
    {
        [TestMethod]
        public void Serialize_Leaf()
        {
            MerklePatriciaNode mptItem = MerklePatriciaNode.BranchNode();
            mptItem.Path = Encoding.UTF8.GetBytes("2");
            mptItem.Value = Encoding.UTF8.GetBytes("abc");
//            mptItem.Key = new UInt256(System.Text.Encoding.UTF8.GetBytes("oi").Sha256());
            mptItem.Key = System.Text.Encoding.UTF8.GetBytes("oi").Sha256();

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

//                        mptResposta.NodeType.Should().Be(mptItem.NodeType);
//                        mptResposta.Path.Should().Be(mptItem.Path);
//                        mptResposta.Value.Should().Be(mptItem.Value);
//                        mptResposta.KeyHash.Should().Be(mptItem.KeyHash);
                    }
                }
            }
        }
    }
}