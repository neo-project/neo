using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.SmartContract.Enumerators;
using Neo.SmartContract.Iterators;
using Neo.VM;
using System;
using System.Collections.Generic;

namespace Neo.UnitTests.SmartContract.Enumerators
{

    [TestClass]
    public class UT_IteratorValuesWrapper
    {
        [TestMethod]
        public void TestGeneratorAndDispose()
        {
            IteratorValuesWrapper iteratorValuesWrapper = new IteratorValuesWrapper(new ArrayWrapper(new List<StackItem>()));
            Assert.IsNotNull(iteratorValuesWrapper);
            Action action = () => iteratorValuesWrapper.Dispose();
            action.Should().NotThrow<Exception>();
        }

        [TestMethod]
        public void TestNextAndValue()
        {
            StackItem stackItem = new VM.Types.Boolean(true);
            List<StackItem> list = new List<StackItem>();
            list.Add(stackItem);
            ArrayWrapper wrapper = new ArrayWrapper(list);
            IteratorValuesWrapper iteratorValuesWrapper = new IteratorValuesWrapper(wrapper);
            Action action = () => iteratorValuesWrapper.Next();
            action.Should().NotThrow<Exception>();
            Assert.AreEqual(stackItem, iteratorValuesWrapper.Value());
        }
    }
}
