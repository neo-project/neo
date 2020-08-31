using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.SmartContract;

namespace Neo.UnitTests.SmartContract
{
    [TestClass]
    public class UT_Helper
    {
        [TestMethod]
        public void TestIsMultiSigContract()
        {
            var case1 = new byte[]
            {
                0, 2, 12, 33, 221, 221, 221, 221, 221, 221, 221, 221, 221, 221, 221, 221, 221, 221, 221, 221, 221, 221,
                221, 221, 221, 221, 221, 221, 221, 221, 221, 221, 221, 221, 221, 221, 221, 12, 33, 255, 255, 255, 255,
                255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
                255, 255, 255, 255, 255, 255, 255, 255, 0,
            };
            Assert.IsFalse(case1.IsMultiSigContract());

            var case2 = new byte[]
            {
                18, 12, 33, 2, 111, 240, 59, 148, 146, 65, 206, 29, 173, 212, 53, 25, 230, 150, 14, 10, 133, 180, 26,
                105, 160, 92, 50, 129, 3, 170, 43, 206, 21, 148, 202, 22, 12, 33, 2, 111, 240, 59, 148, 146, 65, 206,
                29, 173, 212, 53, 25, 230, 150, 14, 10, 133, 180, 26, 105, 160, 92, 50, 129, 3, 170, 43, 206, 21, 148,
                202, 22, 18
            };
            case2.IsMultiSigContract();
        }
    }
}
