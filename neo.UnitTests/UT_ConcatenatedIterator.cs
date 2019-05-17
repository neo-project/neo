using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.SmartContract.Iterators;
using Neo.VM.Types;
using System.Numerics;

namespace Neo.UnitTests
{

    [TestClass]
    public class UT_ConcatenatedIterator
    {
        [TestMethod]
        public void ConcatenatedIteratedOverflowTest()
        {
            Integer[] array1 = { MakeIntegerStackItem(1) };
            ArrayWrapper it1 = new ArrayWrapper(array1);
            ArrayWrapper it2 = new ArrayWrapper(array1);
            ConcatenatedIterator uut = new ConcatenatedIterator(it1, it2);

            uut.Next().Should().Be(true);
            uut.Key().Should().Be(MakeIntegerStackItem(0));
            uut.Value().Should().Be(array1[0]);

            uut.Next().Should().Be(true);
            uut.Key().Should().Be(MakeIntegerStackItem(0));
            uut.Value().Should().Be(array1[0]);

            uut.Next().Should().Be(false);
        }

        [TestMethod]
        public void ConcatenatedIteratedTest()
        {
            Integer[] array1 = { MakeIntegerStackItem(1), MakeIntegerStackItem(7), MakeIntegerStackItem(23) };
            Integer[] array2 = { MakeIntegerStackItem(8), MakeIntegerStackItem(47) };
            ArrayWrapper it1 = new ArrayWrapper(array1);
            ArrayWrapper it2 = new ArrayWrapper(array2);
            ConcatenatedIterator uut = new ConcatenatedIterator(it1, it2);

            uut.Next().Should().Be(true);
            uut.Key().Should().Be(MakeIntegerStackItem(0));
            uut.Value().Should().Be(array1[0]);

            uut.Next().Should().Be(true);
            uut.Key().Should().Be(MakeIntegerStackItem(1));
            uut.Value().Should().Be(array1[1]);

            uut.Next().Should().Be(true);
            uut.Key().Should().Be(MakeIntegerStackItem(2));
            uut.Value().Should().Be(array1[2]);

            uut.Next().Should().Be(true);
            uut.Key().Should().Be(MakeIntegerStackItem(0));
            uut.Value().Should().Be(array2[0]);

            uut.Next().Should().Be(true);
            uut.Key().Should().Be(MakeIntegerStackItem(1));
            uut.Value().Should().Be(array2[1]);

            uut.Next().Should().Be(false);
        }

        private Integer MakeIntegerStackItem(int val)
        {
            return new Integer(new BigInteger(val));
        }
    }
}