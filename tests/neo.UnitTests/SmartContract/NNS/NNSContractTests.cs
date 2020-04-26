using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.SmartContract.NNS;
using System;
using System.Collections.Generic;
using System.Text;

namespace Neo.SmartContract.NNS.Tests
{
    [TestClass()]
    public class NNSContractTests
    {
        [TestMethod()]
        public void IsDomainTest()
        {
            Assert.IsFalse(NNSContract.IsDomain(""));
            Assert.IsFalse(NNSContract.IsDomain(null));
            Assert.IsFalse(NNSContract.IsDomain("www,neo.org"));
            Assert.IsFalse(NNSContract.IsDomain("www.hello.world.neo.org"));
            Assert.IsTrue(NNSContract.IsDomain("www.hello.neo.org"));
            Assert.IsTrue(NNSContract.IsDomain("www.neo.org"));
            Assert.IsTrue(NNSContract.IsDomain("neo.org"));
            Assert.IsTrue(NNSContract.IsDomain("bb.aa123"));
        }
    }
}
