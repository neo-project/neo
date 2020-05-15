using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.VM.Types;

namespace Neo.SmartContract.NNS.Tests
{
    [TestClass()]
    public class UT_DomainState
    {
        [TestMethod()]
        public void Check_FromAndToStackItem()
        {
            DomainState domainState = new DomainState();
            domainState.Name = "AA";
            domainState.Operator = UInt160.Zero;
            domainState.TimeToLive = 1;
            StackItem stackitem = domainState.ToStackItem(new VM.ReferenceCounter());
            DomainState result = new DomainState();
            result.FromStackItem(stackitem);
            Assert.AreEqual(domainState.Name, result.Name);
            Assert.AreEqual(domainState.Operator, result.Operator);
            Assert.AreEqual(domainState.TimeToLive, result.TimeToLive);
        }
    }
}
