using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.SmartContract.Iterators;
using Neo.VM;
using Neo.VM.Types;
using System;
using System.Collections.Generic;

namespace Neo.UnitTests.SmartContract.Iterators
{
    [TestClass]
    public class UT_MapWrapper
    {
        [TestMethod]
        public void TestGeneratorAndDispose()
        {
            MapWrapper mapWrapper = new MapWrapper(new List<KeyValuePair<StackItem, StackItem>>());
            Assert.IsNotNull(mapWrapper);
            Action action = () => mapWrapper.Dispose();
            action.Should().NotThrow<Exception>();
        }

        [TestMethod]
        public void TestKeyAndValue()
        {
            List<KeyValuePair<StackItem, StackItem>> list = new List<KeyValuePair<StackItem, StackItem>>();
            StackItem stackItem1 = new Integer(0);
            StackItem stackItem2 = new Integer(1);
            list.Add(new KeyValuePair<StackItem, StackItem>(stackItem1, stackItem2));
            MapWrapper mapWrapper = new MapWrapper(list);
            mapWrapper.Next();
            Assert.AreEqual(stackItem1, mapWrapper.Key());
            Assert.AreEqual(stackItem2, mapWrapper.Value());
        }

        [TestMethod]
        public void TestNext()
        {
            MapWrapper mapWrapper = new MapWrapper(new List<KeyValuePair<StackItem, StackItem>>());
            Assert.AreEqual(false, mapWrapper.Next());
        }
    }
}
