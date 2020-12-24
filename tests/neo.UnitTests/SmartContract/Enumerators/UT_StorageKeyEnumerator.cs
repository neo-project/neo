using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.Ledger;
using Neo.SmartContract.Enumerators;
using System;
using System.Collections.Generic;

namespace Neo.UnitTests.SmartContract.Enumerators
{
    [TestClass]
    public class UT_StorageKeyEnumerator
    {
        private class TestEnumeratorDispose : IEnumerator<StorageKey>
        {
            public bool IsDisposed { get; private set; }
            public StorageKey Current => throw new NotImplementedException();
            object System.Collections.IEnumerator.Current => throw new NotImplementedException();
            public void Dispose()
            {
                IsDisposed = true;
            }
            public bool MoveNext() => throw new NotImplementedException();
            public void Reset() => throw new NotImplementedException();
        }

        [TestMethod]
        public void TestGeneratorAndDispose()
        {
            var enumerator = new TestEnumeratorDispose();
            var iterator = new StorageKeyEnumerator(enumerator, 0);
            Action action = () => iterator.Dispose();
            enumerator.IsDisposed.Should().BeFalse();
            action.Should().NotThrow<Exception>();
            enumerator.IsDisposed.Should().BeTrue();
        }

        [TestMethod]
        public void TestNextAndValue()
        {
            var list = new List<StorageKey>
            {
                new StorageKey() { Id = 1, Key = new byte[] { 1, 2, 3 } },
                new StorageKey() { Id = 1, Key = new byte[] { 4, 5, 6 } }
            };

            // With prefix

            var iterator = new StorageKeyEnumerator(list.GetEnumerator(), 0);
            Action actionTrue = () => iterator.Next().Should().BeTrue();
            actionTrue.Should().NotThrow<Exception>();
            CollectionAssert.AreEqual(new byte[] { 1, 2, 3 }, iterator.Value().GetSpan().ToArray());
            actionTrue.Should().NotThrow<Exception>();
            CollectionAssert.AreEqual(new byte[] { 4, 5, 6 }, iterator.Value().GetSpan().ToArray());
            Action actionFalse = () => iterator.Next().Should().BeFalse();
            actionFalse.Should().NotThrow<Exception>();

            // Without prefix

            iterator = new StorageKeyEnumerator(list.GetEnumerator(), 2);
            actionTrue = () => iterator.Next().Should().BeTrue();
            actionTrue.Should().NotThrow<Exception>();
            CollectionAssert.AreEqual(new byte[] { 3 }, iterator.Value().GetSpan().ToArray());
            actionTrue.Should().NotThrow<Exception>();
            CollectionAssert.AreEqual(new byte[] { 6 }, iterator.Value().GetSpan().ToArray());
            actionFalse = () => iterator.Next().Should().BeFalse();
            actionFalse.Should().NotThrow<Exception>();
        }
    }
}
