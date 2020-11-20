using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO;
using Neo.Network.P2P.Payloads;

namespace Neo.UnitTests.Network.P2P.Payloads
{
    [TestClass]
    public class UT_StateRoot
    {
        [TestMethod]
        public void TestSerialize()
        {
            var root = new StateRoot
            {
                Version = 0x00,
                Index = 0u,
                RootHash = UInt256.Zero,
                Witness = null,
            };
            var expect = "00" + "00000000" + "0000000000000000000000000000000000000000000000000000000000000000" + "00";
            Assert.AreEqual(expect, root.ToArray().ToHexString());

            root = new StateRoot
            {
                Version = 0x00,
                Index = 0u,
                RootHash = UInt256.Zero,
                Witness = new Witness
                {
                    InvocationScript = new byte[] { 0x01 },
                    VerificationScript = new byte[] { 0x02 }
                },
            };
            expect = "00" + "00000000" + "0000000000000000000000000000000000000000000000000000000000000000" + "01" + "0101" + "0102";
            Assert.AreEqual(expect, root.ToArray().ToHexString());
        }

        [TestMethod]
        public void TestSize()
        {
            var root = new StateRoot
            {
                Version = 0x00,
                Index = 0u,
                RootHash = UInt256.Zero,
                Witness = new Witness
                {
                    InvocationScript = new byte[] { 0x01 },
                    VerificationScript = new byte[] { 0x02 }
                },
            };
            Assert.AreEqual(42, root.Size);
        }

        [TestMethod]
        public void TestDeserial()
        {
            var hex = "00" + "00000000" + "0000000000000000000000000000000000000000000000000000000000000000" + "00";
            var root = hex.HexToBytes().AsSerializable<StateRoot>();
            Assert.AreEqual(0x00, root.Version);
            Assert.AreEqual(0u, root.Index);
            Assert.AreEqual(UInt256.Zero, root.RootHash);
            Assert.IsNull(root.Witness);
            hex = "00" + "00000000" + "0000000000000000000000000000000000000000000000000000000000000000" + "01" + "0101" + "0102";
            root = hex.HexToBytes().AsSerializable<StateRoot>();
            Assert.AreEqual(0x00, root.Version);
            Assert.AreEqual(0u, root.Index);
            Assert.AreEqual(UInt256.Zero, root.RootHash);
            Assert.IsNotNull(root.Witness);
            Assert.AreEqual("01", root.Witness.InvocationScript.ToHexString());
            Assert.AreEqual("02", root.Witness.VerificationScript.ToHexString());
        }
    }
}
