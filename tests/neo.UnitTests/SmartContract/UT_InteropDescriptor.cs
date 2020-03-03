using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.SmartContract;

namespace Neo.UnitTests.SmartContract
{
    [TestClass]
    public class UT_InteropDescriptor
    {
        [TestMethod]
        public void TestGetMethod()
        {
            string method = @"System.ExecutionEngine.GetScriptContainer";
            uint price = 0_00000250u;
            TriggerType allowedTriggers = TriggerType.All;
            InteropDescriptor descriptor = new InteropDescriptor(method, TestHandler, price, allowedTriggers, CallFlags.None);
            descriptor.Method.Should().Be(method);
            descriptor.Price.Should().Be(price);
        }

        private bool TestHandler(ApplicationEngine engine)
        {
            return true;
        }
    }
}
