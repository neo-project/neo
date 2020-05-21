using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.VM.Types;
using System;
using System.Text;

namespace Neo.SmartContract.Nns.Tests
{
    [TestClass()]
    public class UT_DomainState
    {
        [TestMethod()]
        public void Check_FromAndToStackItem()
        {
            DomainState domainState = new DomainState();
            domainState.TokenId = Encoding.ASCII.GetBytes("AA");
            domainState.Operator = UInt160.Zero;
            domainState.TimeToLive = 1;
            StackItem stackitem = domainState.ToStackItem(new VM.ReferenceCounter());
            DomainState result = new DomainState();
            result.FromStackItem(stackitem);
            domainState.TokenId.Should().BeEquivalentTo(result.TokenId);
            Assert.AreEqual(domainState.Operator, result.Operator);
            Assert.AreEqual(domainState.TimeToLive, result.TimeToLive);
        }
    }
}
