using System.IO;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Cryptography;
using Neo.Ledger;

namespace Neo.UnitTests.Ledger
{
    [TestClass]
    public class UT_MerklePatricia
    {
        [TestMethod]
        public void DistinctRoot()
        {
            var mp = new MerklePatricia();
            Assert.IsFalse(mp == null);
            mp[new byte[] {0x0, 0x0, 0x1}] = new byte[] {0x0, 0x0, 0x1};
            Assert.Equals(new byte[] {0x0, 0x0, 0x1}, mp[new byte[] {0x0, 0x0, 0x1}]);

            mp[new byte[] {0x11, 0x0, 0x2}] = new byte[] {0x11, 0x0, 0x2};
            Assert.Equals(new byte[] {0x11, 0x0, 0x2}, mp[new byte[] {0x11, 0x0, 0x2}]);
        }

        [TestMethod]
        public void SameRoot()
        {
            var mp = new MerklePatricia();
            Assert.IsFalse(mp == null);
            mp[new byte[] {0x0, 0x0, 0x1}] = new byte[] {0x0, 0x0, 0x1};
            Assert.Equals(new byte[] {0x0, 0x0, 0x1}, mp[new byte[] {0x0, 0x0, 0x1}]);

            mp[new byte[] {0x0, 0x0, 0x1}] = new byte[] {0x0, 0x0, 0x2};
            Assert.Equals(new byte[] {0x0, 0x0, 0x2}, mp[new byte[] {0x0, 0x0, 0x1}]);
        }
        
        [TestMethod]
        public void ColideKeys()
        {
            var mp = new MerklePatricia
            {
                ["oi"] = "batata",
                ["oi"] = "batatatinha"
            };
            Assert.IsTrue(mp.ContainsKey("oi"));
            Assert.Equals("batatatinha", mp["oi"]);

            mp["orelha"] = "batatatinha";
            Assert.Equals("batatatinha", mp["orelha"]);

            mp["orfão"] = "criança";
            Assert.Equals("criança", mp["orfão"]);

            mp["orfanato"] = "crianças";
            Assert.Equals("crianças", mp["orfanato"]);

            Assert.IsTrue(mp.Remove("orfanato"));
            Assert.Equals("criança", mp["orfão"]);
            Assert.IsFalse(mp.ContainsKey("orfanato"));

            mp["orfã"] = "menina";
            Assert.Equals("menina", mp["orfã"]);
        }
    }
}