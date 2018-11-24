using System.Collections.Generic;
using System.IO;
using System.Linq;
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
            Assert.IsTrue(new byte[] {0x0, 0x0, 0x1}.SequenceEqual(mp[new byte[] {0x0, 0x0, 0x1}]));

            mp[new byte[] {0x11, 0x0, 0x2}] = new byte[] {0x11, 0x0, 0x2};
            Assert.IsTrue(new byte[] {0x11, 0x0, 0x2}.SequenceEqual(mp[new byte[] {0x11, 0x0, 0x2}]));
        }

        [TestMethod]
        public void SameRoot()
        {
            var mp = new MerklePatricia();
            Assert.IsFalse(mp == null);
            mp[new byte[] {0x0, 0x0, 0x1}] = new byte[] {0x0, 0x0, 0x1};
            Assert.IsTrue(new byte[] {0x0, 0x0, 0x1}.SequenceEqual(mp[new byte[] {0x0, 0x0, 0x1}]));

            mp[new byte[] {0x0, 0x0, 0x1}] = new byte[] {0x0, 0x0, 0x2};
            Assert.IsTrue(new byte[] {0x0, 0x0, 0x2}.SequenceEqual(mp[new byte[] {0x0, 0x0, 0x1}]));
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
            Assert.AreEqual("batatatinha", mp["oi"]);

            mp["orelha"] = "batatatinha";
            Assert.AreEqual("batatatinha", mp["orelha"]);

            mp["orfão"] = "criança";
            Assert.AreEqual("criança", mp["orfão"]);

            mp["orfanato"] = "crianças";
            Assert.AreEqual("crianças", mp["orfanato"]);

            Assert.IsTrue(mp.Remove("orfanato"));
            Assert.AreEqual("criança", mp["orfão"]);
            Assert.IsFalse(mp.ContainsKey("orfanato"));

            mp["orfã"] = "menina";
            Assert.AreEqual("menina", mp["orfã"]);
        }

        [TestMethod]
        public void StepByStep()
        {
            var mp = new MerklePatricia();
            Assert.AreEqual(0, mp.Count());
            var a0001 = new byte[] {0x0, 0x01};
            mp[a0001] = a0001;
            Assert.AreEqual(1, mp.Count());
            Assert.AreEqual(a0001, mp[a0001]);

            var a0101 = new byte[] {0x01, 0x01};
            mp[a0101] = a0101;
            Assert.AreEqual(2, mp.Count());
            Assert.AreEqual(a0001, mp[a0001]);
            Assert.AreEqual(a0101, mp[a0101]);

            Assert.IsTrue(mp.Remove(a0101));
            Assert.IsFalse(mp.ContainsKey(a0101));
            Assert.AreEqual(a0001, mp[a0001]);

            // TODO FIXME Change the dictionary of byte[] to UIntBase cause byte[] has a problem on the hashcode
            Assert.AreEqual(new MerklePatricia {[a0001] = a0001}, mp);
        }
    }
}