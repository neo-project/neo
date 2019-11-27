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
    public class UT_ArrayWrapper
    {
        [TestMethod]
        public void TestGeneratorAndDispose()
        {
            ArrayWrapper arrayWrapper = new ArrayWrapper(new List<StackItem>());
            Assert.IsNotNull(arrayWrapper);
            Action action = () => arrayWrapper.Dispose();
            action.Should().NotThrow<Exception>();
        }

        [TestMethod]
        public void TestKeyAndValue()
        {
            List<StackItem> list = new List<StackItem>();
            StackItem stackItem = new Integer(0);
            list.Add(stackItem);
            ArrayWrapper arrayWrapper = new ArrayWrapper(list);
            Action action1 = () => arrayWrapper.Key();
            action1.Should().Throw<InvalidOperationException>();
            Action action2 = () => arrayWrapper.Value();
            action2.Should().Throw<InvalidOperationException>();
            arrayWrapper.Next();
            Assert.AreEqual(stackItem, arrayWrapper.Key());
            Assert.AreEqual(stackItem, arrayWrapper.Value());
        }

        [TestMethod]
        public void TestReverse()
        {
            List<StackItem> list = new List<StackItem>(new StackItem[] { 1, 2, 3 });

            IIterator arrayWrapper = new ArrayWrapper(list);

            for (int x = 0; x < 3; x++)
            {
                Assert.IsTrue(arrayWrapper.Next());
                Assert.AreEqual(x, arrayWrapper.Key());
                Assert.AreEqual(x + 1, arrayWrapper.Value());
            }

            Assert.IsFalse(arrayWrapper.Next());
            arrayWrapper = new ArrayWrapper(list).Reverse();

            for (int x = 2; x >= 0; x--)
            {
                Assert.IsTrue(arrayWrapper.Next());
                Assert.AreEqual(x, arrayWrapper.Key());
                Assert.AreEqual(x + 1, arrayWrapper.Value());
            }

            Assert.IsFalse(arrayWrapper.Next());
            arrayWrapper = new ArrayWrapper(list);

            Assert.IsTrue(arrayWrapper.Next());
            Assert.AreEqual(0, arrayWrapper.Key());
            Assert.AreEqual(1, arrayWrapper.Value());
            Assert.IsTrue(arrayWrapper.Next());
            Assert.AreEqual(1, arrayWrapper.Key());
            Assert.AreEqual(2, arrayWrapper.Value());

            arrayWrapper = arrayWrapper.Reverse();

            Assert.IsTrue(arrayWrapper.Next());
            Assert.AreEqual(0, arrayWrapper.Key());
            Assert.AreEqual(1, arrayWrapper.Value());

            Assert.IsFalse(arrayWrapper.Next());
        }

        [TestMethod]
        public void TestNext()
        {
            List<StackItem> list = new List<StackItem>();
            ArrayWrapper arrayWrapper = new ArrayWrapper(list);
            Assert.AreEqual(false, arrayWrapper.Next());
            StackItem stackItem = new Integer(0);
            list.Add(stackItem);
            Assert.AreEqual(true, arrayWrapper.Next());
        }
    }
}
