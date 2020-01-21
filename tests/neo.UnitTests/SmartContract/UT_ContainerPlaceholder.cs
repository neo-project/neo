using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.SmartContract;
using Neo.VM.Types;
using System;

namespace Neo.UnitTests.SmartContract
{
    [TestClass]
    public class UT_ContainerPlaceholder
    {
        [TestMethod]
        public void TestGenerator()
        {
            ContainerPlaceholder containerPlaceholder = new ContainerPlaceholder(StackItemType.Any, 0);
            Assert.IsNotNull(containerPlaceholder);
        }

        [TestMethod]
        public void TestEquals()
        {
            ContainerPlaceholder containerPlaceholder = new ContainerPlaceholder(StackItemType.Any, 0);
            Action action = () => containerPlaceholder.Equals(new Integer(0));
            action.Should().Throw<NotSupportedException>();
        }

        [TestMethod]
        public void TestGetBoolean()
        {
            ContainerPlaceholder containerPlaceholder = new ContainerPlaceholder(StackItemType.Any, 0);
            Action action = () => containerPlaceholder.ToBoolean();
            action.Should().Throw<NotSupportedException>();
        }
    }
}
