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
            AccountState nepAccountState = new AccountState();
            nepAccountState.Balance = 100;
            StackItem stackitem = nepAccountState.ToStackItem(new VM.ReferenceCounter());
            AccountState result = new AccountState();
            result.FromStackItem(stackitem);
            Assert.AreEqual(nepAccountState.Balance, result.Balance);
        }
    }
}
