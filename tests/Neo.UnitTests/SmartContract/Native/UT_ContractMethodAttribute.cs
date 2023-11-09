using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.SmartContract.Native;

namespace Neo.UnitTests.SmartContract.Native
{
    [TestClass]
    public class UT_ContractMethodAttribute
    {
        [TestMethod]
        public void TestConstructorOneArg()
        {
            var arg = new ContractMethodAttribute();

            Assert.IsNull(arg.ActiveIn);

            arg = new ContractMethodAttribute(Hardfork.HF_Aspidochelone);

            Assert.AreEqual(Hardfork.HF_Aspidochelone, arg.ActiveIn);
        }
    }
}
