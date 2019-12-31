using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO.Caching;

namespace Neo.UnitTests.IO.Caching
{
    [TestClass]
    public class UT_HashSetCache
    {
        [TestMethod]
        public void TestHashSetCache()
        {
            var bucket = new HashSetCache<int>(10);
            for (int i = 1; i <= 100; i++)
            {
                bucket.Add(i);
            }
            bucket.Count.Should().Be(100);

            int sum = 0;
            foreach (var ele in bucket)
            {
                sum += ele;
            }
            sum.Should().Be(5050);

            bucket.Add(101);
            bucket.Count.Should().Be(91);

            var items = new int[10];
            var value = 11;
            for (int i = 0; i < 10; i++)
            {
                items[i] = value;
                value += 2;
            }
            bucket.ExceptWith(items);
            bucket.Count.Should().Be(81);

            bucket.Contains(13).Should().BeFalse();
            bucket.Contains(50).Should().BeTrue();
        }
    }
}
