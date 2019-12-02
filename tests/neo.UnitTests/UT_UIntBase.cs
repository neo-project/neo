using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Neo.UnitTests.IO
{
    [TestClass]
    public class UT_UIntBase
    {
        [TestMethod]
        public void TestParse()
        {
            UInt160 uInt1601 = (UInt160)UIntBase.Parse("0x0000000000000000000000000000000000000000");
            UInt256 uInt2561 = (UInt256)UIntBase.Parse("0x0000000000000000000000000000000000000000000000000000000000000000");
            UInt160 uInt1602 = (UInt160)UIntBase.Parse("0000000000000000000000000000000000000000");
            UInt256 uInt2562 = (UInt256)UIntBase.Parse("0000000000000000000000000000000000000000000000000000000000000000");
            Assert.AreEqual(UInt160.Zero, uInt1601);
            Assert.AreEqual(UInt256.Zero, uInt2561);
            Assert.AreEqual(UInt160.Zero, uInt1602);
            Assert.AreEqual(UInt256.Zero, uInt2562);
            Action action = () => UIntBase.Parse("0000000");
            action.Should().Throw<FormatException>();
        }

        [TestMethod]
        public void TestTryParse()
        {
            UInt160 uInt160 = new UInt160();
            Assert.AreEqual(true, UIntBase.TryParse("0x0000000000000000000000000000000000000000", out uInt160));
            Assert.AreEqual(UInt160.Zero, uInt160);
            Assert.AreEqual(false, UIntBase.TryParse("0x00000000000000000000000000000000000000", out uInt160));
            UInt256 uInt256 = new UInt256();
            Assert.AreEqual(true, UIntBase.TryParse("0x0000000000000000000000000000000000000000000000000000000000000000", out uInt256));
            Assert.AreEqual(UInt256.Zero, uInt256);
            Assert.AreEqual(false, UIntBase.TryParse("0x00000000000000000000000000000000000000000000000000000000000000", out uInt256));
            UIntBase uIntBase = new UInt160();
            Assert.AreEqual(true, UIntBase.TryParse("0x0000000000000000000000000000000000000000", out uIntBase));
            Assert.AreEqual(UInt160.Zero, uIntBase);
            Assert.AreEqual(true, UIntBase.TryParse("0000000000000000000000000000000000000000", out uIntBase));
            Assert.AreEqual(UInt160.Zero, uIntBase);
            uIntBase = new UInt256();
            Assert.AreEqual(true, UIntBase.TryParse("0x0000000000000000000000000000000000000000000000000000000000000000", out uIntBase));
            Assert.AreEqual(UInt256.Zero, uIntBase);
            Assert.AreEqual(true, UIntBase.TryParse("0000000000000000000000000000000000000000000000000000000000000000", out uIntBase));
            Assert.AreEqual(UInt256.Zero, uIntBase);
            Assert.AreEqual(false, UIntBase.TryParse("00000000000000000000000000000000000000000000000000000000000000", out uIntBase));
        }
    }
}
