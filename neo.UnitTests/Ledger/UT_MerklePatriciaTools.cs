using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Ledger.MPT;

namespace Neo.UnitTests.Ledger
{
    [TestClass]
    public class UT_MerklePatriciaTools
    {
        [TestMethod]
        public void DistinctRoot()
        {
            Assert.AreEqual("faf01", new byte[] {0xfa, 0xf0, 0x1}.ByteToHexString(false, false));
            Assert.AreEqual("fa f0 1", new byte[] {0xfa, 0xf0, 0x1}.ByteToHexString(true, false));
            Assert.AreEqual("faf001", new byte[] {0xfa, 0xf0, 0x1}.ByteToHexString(false));
            Assert.AreEqual("fa f0 01", new byte[] {0xfa, 0xf0, 0x1}.ByteToHexString());
        }

        [TestMethod]
        public void Inc()
        {
            Assert.AreEqual(true, 1.GetBit(0));
            Assert.AreEqual(false, 2.GetBit(0));
        }

        [TestMethod]
        public void CompactEncode()
        {
            Assert.IsTrue(new byte[] {1, 2, 3, 4, 5}.CompactEncode().SequenceEqual(new byte[] {0x11, 0x23, 0x45}));
            Assert.IsTrue(new byte[] {0, 1, 2, 3, 4, 5}.CompactEncode()
                .SequenceEqual(new byte[] {0, 0x01, 0x23, 0x45}));
            Assert.IsTrue(new byte[] {0, 0xf, 1, 0xc, 0xb, 8, 10}.CompactEncode()
                .SequenceEqual(new byte[] {0x10, 0xf1, 0xcb, 0x8a}));
            Assert.IsTrue(new byte[] {0xf, 1, 0xc, 0xb, 8, 10}.CompactEncode()
                .SequenceEqual(new byte[] {0x00, 0xf1, 0xcb, 0x8a}));

            Assert.IsTrue(new byte[] {1, 2, 3, 4, 5}.CompactEncode(true).SequenceEqual(new byte[] {0x31, 0x23, 0x45}));
            Assert.IsTrue(new byte[] {0, 1, 2, 3, 4, 5}.CompactEncode(true)
                .SequenceEqual(new byte[] {0x20, 0x01, 0x23, 0x45}));
            Assert.IsTrue(new byte[] {0, 0xf, 1, 0xc, 0xb, 8, 10}.CompactEncode(true)
                .SequenceEqual(new byte[] {0x30, 0xf1, 0xcb, 0x8a}));
            Assert.IsTrue(new byte[] {0xf, 1, 0xc, 0xb, 8, 10}.CompactEncode(true)
                .SequenceEqual(new byte[] {0x20, 0xf1, 0xcb, 0x8a}));
        }

        [TestMethod]
        public void CompactDecode()
        {
            Assert.IsTrue(new byte[] {0x11, 0x23, 0x45}.CompactDecode().SequenceEqual(new byte[] {1, 2, 3, 4, 5}));
            Assert.IsTrue(new byte[] {0, 0x01, 0x23, 0x45}.CompactDecode()
                .SequenceEqual(new byte[] {0, 1, 2, 3, 4, 5}));
            Assert.IsTrue(new byte[] {0x10, 0xf1, 0xcb, 0x8a}.CompactDecode()
                .SequenceEqual(new byte[] {0, 0xf, 1, 0xc, 0xb, 8, 10}));
            Assert.IsTrue(new byte[] {0x00, 0xf1, 0xcb, 0x8a}.CompactDecode()
                .SequenceEqual(new byte[] {0xf, 1, 0xc, 0xb, 8, 10}));

            Assert.IsTrue(new byte[] {0x31, 0x23, 0x45}.CompactDecode().SequenceEqual(new byte[] {1, 2, 3, 4, 5}));
            Assert.IsTrue(new byte[] {0x20, 0x01, 0x23, 0x45}.CompactDecode()
                .SequenceEqual(new byte[] {0, 1, 2, 3, 4, 5}));
            Assert.IsTrue(new byte[] {0x30, 0xf1, 0xcb, 0x8a}.CompactDecode()
                .SequenceEqual(new byte[] {0, 0xf, 1, 0xc, 0xb, 8, 10}));
            Assert.IsTrue(new byte[] {0x20, 0xf1, 0xcb, 0x8a}.CompactDecode()
                .SequenceEqual(new byte[] {0xf, 1, 0xc, 0xb, 8, 10}));
        }
    }
}