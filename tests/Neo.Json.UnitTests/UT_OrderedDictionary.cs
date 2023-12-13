using System.Collections;

namespace Neo.Json.UnitTests
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
            keys.Should().Contain("a");
            keys.Count.Should().Be(3);
        }

        [TestMethod]
        public void TestGetValues()
        {
            var values = od.Values;
            values.Should().Contain(1);
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

        [TestMethod]
        public void TestCollectionAddAndContains()
        {
            var pair = new KeyValuePair<string, uint>("d", 4);
            ICollection<KeyValuePair<string, uint>> collection = od;
            collection.Add(pair);
            collection.Contains(pair).Should().BeTrue();
        }

        [TestMethod]
        public void TestCollectionCopyTo()
        {
            var arr = new KeyValuePair<string, uint>[3];
            ICollection<KeyValuePair<string, uint>> collection = od;
            collection.CopyTo(arr, 0);
            arr[0].Key.Should().Be("a");
            arr[0].Value.Should().Be(1);
            arr[1].Key.Should().Be("b");
            arr[1].Value.Should().Be(2);
            arr[2].Key.Should().Be("c");
            arr[2].Value.Should().Be(3);
        }

        [TestMethod]
        public void TestCollectionRemove()
        {
            ICollection<KeyValuePair<string, uint>> collection = od;
            var pair = new KeyValuePair<string, uint>("a", 1);
            collection.Remove(pair);
            collection.Contains(pair).Should().BeFalse();
            collection.Count.Should().Be(2);
        }

        [TestMethod]
        public void TestGetEnumerator()
        {
            IEnumerable collection = od;
            collection.GetEnumerator().MoveNext().Should().BeTrue();
        }
    }
}
