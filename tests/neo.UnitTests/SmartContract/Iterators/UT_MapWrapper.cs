using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.SmartContract.Iterators;
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
            MapWrapper mapWrapper = new MapWrapper(new List<KeyValuePair<PrimitiveType, StackItem>>());
            Assert.IsNotNull(mapWrapper);
            Action action = () => mapWrapper.Dispose();
            action.Should().NotThrow<Exception>();
        }

        [TestMethod]
        public void TestKeyAndValue()
        {
            List<KeyValuePair<PrimitiveType, StackItem>> list = new List<KeyValuePair<PrimitiveType, StackItem>>();
            Integer stackItem1 = new Integer(0);
            StackItem stackItem2 = new Integer(1);
            list.Add(new KeyValuePair<PrimitiveType, StackItem>(stackItem1, stackItem2));
            MapWrapper mapWrapper = new MapWrapper(list);
            mapWrapper.Next();
            Assert.AreEqual(stackItem1, mapWrapper.Key());
            Assert.AreEqual(stackItem2, mapWrapper.Value());
        }

        [TestMethod]
        public void TestNext()
        {
            MapWrapper mapWrapper = new MapWrapper(new List<KeyValuePair<PrimitiveType, StackItem>>());
            Assert.AreEqual(false, mapWrapper.Next());
        }
    }
}
