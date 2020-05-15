using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.SmartContract.Native.Tokens;
using Neo.VM.Types;

namespace Neo.SmartContract.NNS.Tests
{
    [TestClass()]
    public class UT_Nep11AccountState
    {
        [TestMethod()]
        public void Check_FromAndToStackItem()
        {
            Nep11AccountState nep11AccountState = new Nep11AccountState();
            nep11AccountState.Balance = 100;
            StackItem stackitem = nep11AccountState.ToStackItem(new VM.ReferenceCounter());
            Nep11AccountState result = new Nep11AccountState();
            result.FromStackItem(stackitem);
            Assert.AreEqual(nep11AccountState.Balance, result.Balance);
        }
    }
}
