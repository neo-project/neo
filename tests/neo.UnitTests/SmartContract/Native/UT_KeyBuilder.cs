using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.SmartContract.Native;

namespace Neo.UnitTests.SmartContract.Native
{
    [TestClass]
    public class UT_KeyBuilder
    {
        private struct a
        {
            public int x;
        }

        [TestMethod]
        public void Test()
        {
            var key = new KeyBuilder(1, 2);

            Assert.AreEqual("0100000002", key.ToArray().ToHexString());

            key = new KeyBuilder(1, 2);
            key = key.Add(new byte[] { 3, 4 });
            Assert.AreEqual("01000000020304", key.ToArray().ToHexString());

            key = new KeyBuilder(1, 2);
            key = key.Add(new byte[] { 3, 4 });
            key = key.Add(UInt160.Zero);
            Assert.AreEqual("010000000203040000000000000000000000000000000000000000", key.ToArray().ToHexString());

            key = new KeyBuilder(1, 2);
            key = key.Add(new a() { x = 123 });
            Assert.AreEqual("01000000027b000000", key.ToArray().ToHexString());

            key = new KeyBuilder(1, 0);
            key = key.AddBigEndian(new a() { x = 1 });
            Assert.AreEqual("010000000000000001", key.ToArray().ToHexString());
        }
    }
}
