using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.SmartContract.Native;

namespace Neo.SmartContract.NNS.Tests
{
    [TestClass()]
    public class NNSContractTests
    {
        [TestMethod()]
        public void IsDomainTest()
        {
            Assert.IsFalse(NativeContract.NNS.IsDomain(""));
            Assert.IsFalse(NativeContract.NNS.IsDomain(null));
            Assert.IsFalse(NativeContract.NNS.IsDomain("www,neo.org"));
            Assert.IsFalse(NativeContract.NNS.IsDomain("www.hello.world.neo.org"));
            Assert.IsTrue(NativeContract.NNS.IsDomain("www.hello.neo.org"));
            Assert.IsTrue(NativeContract.NNS.IsDomain("www.neo.org"));
            Assert.IsTrue(NativeContract.NNS.IsDomain("neo.org"));
            Assert.IsTrue(NativeContract.NNS.IsDomain("bb.aa123"));
        }
    }
}
