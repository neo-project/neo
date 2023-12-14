using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.VM;

namespace Neo.Test
{
    [TestClass]
    public class UtUnsafe
    {
        [TestMethod]
        public void NotZero()
        {
            Assert.IsFalse(Unsafe.NotZero(System.Array.Empty<byte>()));
            Assert.IsFalse(Unsafe.NotZero(new byte[4]));
            Assert.IsFalse(Unsafe.NotZero(new byte[8]));
            Assert.IsFalse(Unsafe.NotZero(new byte[11]));

            Assert.IsTrue(Unsafe.NotZero(new byte[4] { 0x00, 0x00, 0x00, 0x01 }));
            Assert.IsTrue(Unsafe.NotZero(new byte[8] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 }));
            Assert.IsTrue(Unsafe.NotZero(new byte[11] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01 }));
        }
    }
}
