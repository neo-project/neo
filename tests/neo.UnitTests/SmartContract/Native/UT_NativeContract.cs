using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.SmartContract.Native;

namespace Neo.UnitTests.SmartContract.Native
{
    [TestClass]
    public class UT_NativeContract
    {
        [TestMethod]
        public void TestGetContract()
        {
            Assert.IsTrue(NativeContract.GAS == NativeContract.GetContract(NativeContract.GAS.Hash));
        }
    }
}
