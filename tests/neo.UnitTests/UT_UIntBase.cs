using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO;
using System;
using System.IO;

namespace Neo.UnitTests.IO
{
    [TestClass]
    public class UT_UIntBase
    {
        [TestMethod]
        public void TestDeserialize()
        {
            using (MemoryStream stream = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(stream))
            using (BinaryReader reader = new BinaryReader(stream))
            {
                writer.Write(new byte[20]);
                stream.Seek(0, SeekOrigin.Begin);
                MyUIntBase uIntBase = new MyUIntBase();
                Action action = () => ((ISerializable)uIntBase).Deserialize(reader);
                action.Should().Throw<FormatException>();
            }
        }

        [TestMethod]
        public void TestEquals1()
        {
            MyUIntBase temp1 = new MyUIntBase();
            MyUIntBase temp2 = new MyUIntBase();
            UInt160 temp3 = new UInt160();
            Assert.AreEqual(false, temp1.Equals(null));
            Assert.AreEqual(true, temp1.Equals(temp1));
            Assert.AreEqual(true, temp1.Equals(temp2));
            Assert.AreEqual(false, temp1.Equals(temp3));
        }

        [TestMethod]
        public void TestEquals2()
        {
            MyUIntBase temp1 = new MyUIntBase();
            object temp2 = null;
            object temp3 = new object();
            Assert.AreEqual(false, temp1.Equals(temp2));
            Assert.AreEqual(false, temp1.Equals(temp3));
        }

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

        [TestMethod]
        public void TestOperatorEqual()
        {
            Assert.AreEqual(false, new MyUIntBase() == null);
            Assert.AreEqual(false, null == new MyUIntBase());
        }
    }

    internal class MyUIntBase : UIntBase
    {
        public const int Length = 32;
        public MyUIntBase() : this(null) { }
        public MyUIntBase(byte[] value) : base(Length, value) { }
    }
}
