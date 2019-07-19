using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO.Caching;

namespace Neo.UnitTests.IO.Caching
{
    [TestClass]
    public class UT_OrderedDictionary
    {
        private OrderedDictionary<string, uint> od;

        [TestInitialize]
        public void SetUp()
        {
            od = new OrderedDictionary<string, uint>();
            od.Add("a", 1);
            od.Add("b", 2);
            od.Add("c", 3);
        }

        [TestMethod]
        public void TestClear()
        {
            od.Clear();
            od.Count.Should().Be(0);
            od.TryGetValue("a", out uint i).Should().BeFalse();
        }

        [TestMethod]
        public void TestCount()
        {
            od.Count.Should().Be(3);
            od.Add("d", 4);
            od.Count.Should().Be(4);
        }

        [TestMethod]
        public void TestIsReadOnly()
        {
            od.IsReadOnly.Should().BeFalse();
        }

        [TestMethod]
        public void TestSetAndGetItem()
        {
            var val = od["a"];
            val.Should().Be(1);
            od["d"] = 10;
            od["d"].Should().Be(10);
            od["d"] = 15;
            od["d"].Should().Be(15);
        }

        [TestMethod]
        public void TestGetKeys()
        {
            var keys = od.Keys;
            keys.Contains("a").Should().BeTrue();
            keys.Count.Should().Be(3);
        }

        [TestMethod]
        public void TestGetValues()
        {
            var values = od.Values;
            values.Contains(1).Should().BeTrue();
            values.Count.Should().Be(3);
        }

        [TestMethod]
        public void TestRemove()
        {
            od.Remove("a");
            od.Count.Should().Be(2);
            od.ContainsKey("a").Should().BeFalse();
        }

        [TestMethod]
        public void TestTryGetValue()
        {
            od.TryGetValue("a", out uint i).Should().BeTrue();
            i.Should().Be(1);
            od.TryGetValue("d", out uint j).Should().BeFalse();
            j.Should().Be(0);
        }
    }
}