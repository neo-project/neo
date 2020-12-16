using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.SmartContract.Enumerators;
using Neo.SmartContract.Iterators;
using Neo.VM.Types;
using System;
using System.Collections.Generic;

namespace Neo.UnitTests.SmartContract.Enumerators
{
    [TestClass]
    public class UT_ConcatenatedEnumerator
    {
        [TestMethod]
        public void TestConcatenatedIteratorAndDispose()
        {
            List<StackItem> list1 = new List<StackItem>();
            StackItem stackItem1 = new Integer(0);
            list1.Add(stackItem1);
            List<StackItem> list2 = new List<StackItem>();
            StackItem stackItem2 = new Integer(0);
            list2.Add(stackItem2);
            ArrayWrapper arrayWrapper1 = new ArrayWrapper(list1);
            ArrayWrapper arrayWrapper2 = new ArrayWrapper(list2);
            IteratorKeysWrapper it1 = new IteratorKeysWrapper(arrayWrapper1);
            IteratorKeysWrapper it2 = new IteratorKeysWrapper(arrayWrapper2);
            ConcatenatedEnumerator uut = new ConcatenatedEnumerator(it1, it2);
            Assert.IsNotNull(uut);
            Action action = () => uut.Dispose();
            action.Should().NotThrow<Exception>();
        }

        [TestMethod]
        public void TestNextAndValue()
        {
            List<StackItem> list1 = new List<StackItem>();
            StackItem stackItem1 = new Integer(1);
            list1.Add(stackItem1);
            List<StackItem> list2 = new List<StackItem>();
            StackItem stackItem2 = new Integer(0);
            list2.Add(stackItem2);
            ArrayWrapper arrayWrapper1 = new ArrayWrapper(list1);
            ArrayWrapper arrayWrapper2 = new ArrayWrapper(list2);
            IteratorKeysWrapper it1 = new IteratorKeysWrapper(arrayWrapper1);
            IteratorKeysWrapper it2 = new IteratorKeysWrapper(arrayWrapper2);
            ConcatenatedEnumerator uut = new ConcatenatedEnumerator(it1, it2);
            Assert.AreEqual(true, uut.Next());
            Assert.AreEqual(new Integer(0), uut.Value());
            Assert.AreEqual(true, uut.Next());
            Assert.AreEqual(new Integer(0), uut.Value());
            Assert.AreEqual(false, uut.Next());
        }
    }
}
