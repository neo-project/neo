using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.SmartContract.Iterators;
using Neo.VM.Types;
using System;

namespace Neo.UnitTests.SmartContract.Iterators
{
    [TestClass]
    public class UT_PrimitiveWrapper
    {
        [TestMethod]
        public void TestGeneratorAndDispose()
        {
            ByteArrayWrapper arrayWrapper = new ByteArrayWrapper(new ByteString(new byte[0]));
            Assert.IsNotNull(arrayWrapper);
            Action action = () => arrayWrapper.Dispose();
            action.Should().NotThrow<Exception>();
        }

        [TestMethod]
        public void TestKeyAndValue()
        {
            ByteArrayWrapper arrayWrapper = new ByteArrayWrapper(new byte[] { 0x01, 0x02 });
            Action action2 = () => arrayWrapper.Value();
            action2.Should().Throw<InvalidOperationException>();
            arrayWrapper.Next();
            Assert.AreEqual(0x01, arrayWrapper.Value());
            arrayWrapper.Next();
            Assert.AreEqual(0x02, arrayWrapper.Value());
        }

        [TestMethod]
        public void TestNext()
        {
            ByteArrayWrapper arrayWrapper = new ByteArrayWrapper(new byte[] { 0x01, 0x02 });
            Assert.AreEqual(true, arrayWrapper.Next());
            Assert.AreEqual(0x01, arrayWrapper.Value());
            Assert.AreEqual(true, arrayWrapper.Next());
            Assert.AreEqual(0x02, arrayWrapper.Value());
            Assert.AreEqual(false, arrayWrapper.Next());
        }
    }
}
