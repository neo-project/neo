using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.SmartContract.Native.Tokens;
using Neo.VM.Types;

namespace Neo.SmartContract.NNS.Tests
{
    [TestClass()]
    public class UT_NepAccountState
    {
        [TestMethod()]
        public void Check_FromAndToStackItem()
        {
            NepAccountState nepAccountState = new NepAccountState();
            nepAccountState.Balance = 100;
            StackItem stackitem = nepAccountState.ToStackItem(new VM.ReferenceCounter());
            NepAccountState result = new NepAccountState();
            result.FromStackItem(stackitem);
            Assert.AreEqual(nepAccountState.Balance, result.Balance);
        }
    }
}
