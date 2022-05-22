using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO;
using Neo.Persistence;
using Neo.SmartContract;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Neo.UnitTests.IO.Caching
{
    class MyKey : StorageKey, IEquatable<MyKey>, IComparable<MyKey>, IComparable<StorageKey>, IEquatable<StorageKey>, IComparable
    {
        public int Size => Key.Length;

        public MyKey(UInt256 hash)
        {
            Key = hash.ToArray();
        }

        public MyKey(StorageKey key)
        {
            Id = key.Id;
            Key = key.Key;
        }

        public MyKey(string val)
        {
            Key = Encoding.UTF8.GetBytes(val);
        }

        public void Deserialize(BinaryReader reader)
        {
            Key = Encoding.UTF8.GetBytes(reader.ReadString());
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(Key.Span);
        }
        public bool Equals(MyKey other)
        {
            if (other == null) return false;
            return Id == other.Id && Key.Span.SequenceEqual(other.Key.Span);
        }

        public override bool Equals(object obj)
        {
            if (obj is not MyKey other) return false;
            return Id == other.Id && Key.Span.SequenceEqual(other.Key.Span);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Id, Key.Length);
        }

        public int CompareTo(MyKey obj)
        {
            return CompareTo((StorageKey)obj);
        }

        public int CompareTo(StorageKey obj)
        {
            if (obj is null) throw new Exception();
            int ret = Id.CompareTo(obj.Id);
            if (ret != 0) return ret;
            return Encoding.UTF8.GetString(Key.Span).CompareTo(Encoding.UTF8.GetString(obj.Key.Span));
        }

        public int CompareTo(object obj)
        {
            if (obj is not StorageKey key) throw new Exception();
            return CompareTo(key);
        }
    }

    public class MyValue : StorageItem, ISerializable, IEquatable<MyValue>, IEquatable<StorageItem>
    {
        public MyValue(UInt256 hash)
        {
            Value = hash.ToArray();
        }

        public MyValue(string val)
        {
            Value = Encoding.Default.GetBytes(val);
        }

        public MyValue(ReadOnlyMemory<byte> val)
        {
            Value = val;
        }

        public void FromReplica(MyValue replica)
        {
            Value = replica.Value;
        }

        public bool Equals(StorageItem other)
        {
            if (other == null) return false;
            return Value.Span.SequenceEqual(other.Value.Span);
        }

        public bool Equals(MyValue other)
        {
            if (other == null) return false;
            return Value.Span.SequenceEqual(other.Value.Span);
        }

        public override bool Equals(object obj)
        {
            if (obj is not StorageItem other) return false;
            return Value.Span.SequenceEqual(other.Value.Span);
        }

        public override int GetHashCode()
        {
            return Value.Length;
        }
    }

    class MyDataCache : DataCache
    {
        public Dictionary<StorageKey, StorageItem> InnerDict = new();

        protected override void DeleteInternal(StorageKey key)
        {
            InnerDict.Remove(key);
        }

        protected override void AddInternal(StorageKey key, StorageItem value)
        {
            InnerDict.Add(key, new MyValue(value.Value));
        }

        protected override IEnumerable<(StorageKey, StorageItem)> SeekInternal(byte[] keyOrPrefix, SeekDirection direction = SeekDirection.Forward)
        {
            if (direction == SeekDirection.Forward)
                return InnerDict.OrderBy(kvp => kvp.Key)
                    .Where(kvp => ByteArrayComparer.Default.Compare(kvp.Key.ToArray(), keyOrPrefix) >= 0)
                    .Select(p => (p.Key, (StorageItem)new MyValue(p.Value.Value)));
            else
                return InnerDict.OrderByDescending(kvp => kvp.Key)
                    .Where(kvp => ByteArrayComparer.Reverse.Compare(kvp.Key.ToArray(), keyOrPrefix) >= 0)
                    .Select(p => (p.Key, (StorageItem)new MyValue(p.Value.Value)));
        }

        protected override StorageItem GetInternal(StorageKey key)
        {
            if (InnerDict.TryGetValue(key, out var value))
            {
                return new MyValue(value.Value);
            }
            throw new KeyNotFoundException();
        }

        protected override StorageItem TryGetInternal(StorageKey key)
        {
            if (InnerDict.TryGetValue(key, out var value))
            {
                return new MyValue(value.Value);
            }
            return null;
        }

        protected override bool ContainsInternal(StorageKey key)
        {
            return InnerDict.ContainsKey(key);
        }

        protected override void UpdateInternal(StorageKey key, StorageItem value)
        {
            InnerDict[key] = new MyValue(value.Value);
        }
    }

    [TestClass]
    public class UT_DataCache
    {
        MyDataCache myDataCache;

        [TestInitialize]
        public void Initialize()
        {
            myDataCache = new MyDataCache();
        }

        [TestMethod]
        public void TestAccessByKey()
        {
            myDataCache.Add(new MyKey("key1"), new MyValue("value1"));
            myDataCache.Add(new MyKey("key2"), new MyValue("value2"));

            myDataCache[new MyKey("key1")].Should().Be(new MyValue("value1"));

            // case 2 read from inner
            myDataCache.InnerDict.Add(new MyKey("key3"), new MyValue("value3"));
            myDataCache[new MyKey("key3")].Should().Be(new MyValue("value3"));
        }

        [TestMethod]
        public void TestAccessByNotFoundKey()
        {
            Action action = () =>
            {
                var item = myDataCache[new MyKey("key1")];
            };
            action.Should().Throw<KeyNotFoundException>();
        }

        [TestMethod]
        public void TestAccessByDeletedKey()
        {
            myDataCache.InnerDict.Add(new MyKey("key1"), new MyValue("value1"));
            myDataCache.Delete(new MyKey("key1"));

            Action action = () =>
            {
                var item = myDataCache[new MyKey("key1")];
            };
            action.Should().Throw<KeyNotFoundException>();
        }

        [TestMethod]
        public void TestAdd()
        {
            myDataCache.Add(new MyKey("key1"), new MyValue("value1"));
            myDataCache[new MyKey("key1")].Should().Be(new MyValue("value1"));

            Action action = () => myDataCache.Add(new MyKey("key1"), new MyValue("value1"));
            action.Should().Throw<ArgumentException>();

            myDataCache.InnerDict.Add(new MyKey("key2"), new MyValue("value2"));
            myDataCache.Delete(new MyKey("key2"));
            Assert.AreEqual(TrackState.Deleted, myDataCache.GetChangeSet().Where(u => u.Key.Equals(new MyKey("key2"))).Select(u => u.State).FirstOrDefault());
            myDataCache.Add(new MyKey("key2"), new MyValue("value2"));
            Assert.AreEqual(TrackState.Changed, myDataCache.GetChangeSet().Where(u => u.Key.Equals(new MyKey("key2"))).Select(u => u.State).FirstOrDefault());

            action = () => myDataCache.Add(new MyKey("key2"), new MyValue("value2"));
            action.Should().Throw<ArgumentException>();
        }

        [TestMethod]
        public void TestCommit()
        {
            myDataCache.Add(new MyKey("key1"), new MyValue("value1"));
            Assert.AreEqual(TrackState.Added, myDataCache.GetChangeSet().Where(u => u.Key.Equals(new MyKey("key1"))).Select(u => u.State).FirstOrDefault());

            myDataCache.InnerDict.Add(new MyKey("key2"), new MyValue("value2"));
            myDataCache.Delete(new MyKey("key2"));
            Assert.AreEqual(TrackState.Deleted, myDataCache.GetChangeSet().Where(u => u.Key.Equals(new MyKey("key2"))).Select(u => u.State).FirstOrDefault());

            myDataCache.InnerDict.Add(new MyKey("key3"), new MyValue("value3"));
            Assert.AreEqual(TrackState.None, myDataCache.GetChangeSet().Where(u => u.Key.Equals(new MyKey("key3"))).Select(u => u.State).FirstOrDefault());
            myDataCache.Delete(new MyKey("key3"));
            Assert.AreEqual(TrackState.Deleted, myDataCache.GetChangeSet().Where(u => u.Key.Equals(new MyKey("key3"))).Select(u => u.State).FirstOrDefault());
            myDataCache.Add(new MyKey("key3"), new MyValue("value4"));
            Assert.AreEqual(TrackState.Changed, myDataCache.GetChangeSet().Where(u => u.Key.Equals(new MyKey("key3"))).Select(u => u.State).FirstOrDefault());

            myDataCache.Commit();
            Assert.AreEqual(0, myDataCache.GetChangeSet().Count());

            myDataCache.InnerDict[new MyKey("key1")].Should().Be(new MyValue("value1"));
            myDataCache.InnerDict.ContainsKey(new MyKey("key2")).Should().BeFalse();
            myDataCache.InnerDict[new MyKey("key3")].Should().Be(new MyValue("value4"));
        }

        [TestMethod]
        public void TestCreateSnapshot()
        {
            myDataCache.CreateSnapshot().Should().NotBeNull();
        }

        [TestMethod]
        public void TestDelete()
        {
            myDataCache.Add(new MyKey("key1"), new MyValue("value1"));
            myDataCache.Delete(new MyKey("key1"));
            myDataCache.InnerDict.ContainsKey(new MyKey("key1")).Should().BeFalse();

            myDataCache.InnerDict.Add(new MyKey("key2"), new MyValue("value2"));
            myDataCache.Delete(new MyKey("key2"));
            myDataCache.Commit();
            myDataCache.InnerDict.ContainsKey(new MyKey("key2")).Should().BeFalse();
        }

        [TestMethod]
        public void TestFind()
        {
            myDataCache.Add(new MyKey("key1"), new MyValue("value1"));
            myDataCache.Add(new MyKey("key2"), new MyValue("value2"));

            myDataCache.InnerDict.Add(new MyKey("key3"), new MyValue("value3"));
            myDataCache.InnerDict.Add(new MyKey("key4"), new MyValue("value4"));

            var items = myDataCache.Find(new MyKey("key1").ToArray());
            new MyKey("key1").Should().Be(items.ElementAt(0).Key);
            new MyValue("value1").Should().Be(items.ElementAt(0).Value);
            items.Count().Should().Be(1);

            items = myDataCache.Find(new MyKey("key5").ToArray());
            items.Count().Should().Be(0);
        }

        [TestMethod]
        public void TestSeek()
        {
            myDataCache.Add(new MyKey("key1"), new MyValue("value1"));
            myDataCache.Add(new MyKey("key2"), new MyValue("value2"));

            myDataCache.InnerDict.Add(new MyKey("key3"), new MyValue("value3"));
            myDataCache.InnerDict.Add(new MyKey("key4"), new MyValue("value4"));

            var items = myDataCache.Seek(new MyKey("key3").ToArray(), SeekDirection.Backward).ToArray();
            new MyKey("key3").Should().Be(items[0].Key);
            new MyValue("value3").Should().Be(items[0].Value);
            new MyKey("key2").Should().Be(items[1].Key);
            new MyValue("value2").Should().Be(items[1].Value);
            items.Length.Should().Be(3);

            items = myDataCache.Seek(new MyKey("key5").ToArray(), SeekDirection.Forward).ToArray();
            items.Length.Should().Be(0);
        }

        [TestMethod]
        public void TestFindRange()
        {
            myDataCache.Add(new MyKey("key1"), new MyValue("value1"));
            myDataCache.Add(new MyKey("key2"), new MyValue("value2"));

            myDataCache.InnerDict.Add(new MyKey("key3"), new MyValue("value3"));
            myDataCache.InnerDict.Add(new MyKey("key4"), new MyValue("value4"));

            var items = myDataCache.FindRange(new MyKey("key3").ToArray(), new MyKey("key5").ToArray()).ToArray();
            new MyKey("key3").Should().Be(items[0].Key);
            new MyValue("value3").Should().Be(items[0].Value);
            new MyKey("key4").Should().Be(items[1].Key);
            new MyValue("value4").Should().Be(items[1].Value);
            items.Length.Should().Be(2);

            // case 2 Need to sort the cache of myDataCache

            myDataCache = new MyDataCache();
            myDataCache.Add(new MyKey("key1"), new MyValue("value1"));
            myDataCache.Add(new MyKey("key2"), new MyValue("value2"));

            myDataCache.InnerDict.Add(new MyKey("key4"), new MyValue("value4"));
            myDataCache.InnerDict.Add(new MyKey("key3"), new MyValue("value3"));

            items = myDataCache.FindRange(new MyKey("key3").ToArray(), new MyKey("key5").ToArray()).ToArray();
            new MyKey("key3").Should().Be(items[0].Key);
            new MyValue("value3").Should().Be(items[0].Value);
            new MyKey("key4").Should().Be(items[1].Key);
            new MyValue("value4").Should().Be(items[1].Value);
            items.Length.Should().Be(2);

            // case 3 FindRange by Backward

            myDataCache = new MyDataCache();
            myDataCache.Add(new MyKey("key1"), new MyValue("value1"));
            myDataCache.Add(new MyKey("key2"), new MyValue("value2"));

            myDataCache.InnerDict.Add(new MyKey("key4"), new MyValue("value4"));
            myDataCache.InnerDict.Add(new MyKey("key3"), new MyValue("value3"));
            myDataCache.InnerDict.Add(new MyKey("key5"), new MyValue("value5"));

            items = myDataCache.FindRange(new MyKey("key5").ToArray(), new MyKey("key3").ToArray(), SeekDirection.Backward).ToArray();
            new MyKey("key5").Should().Be(items[0].Key);
            new MyValue("value5").Should().Be(items[0].Value);
            new MyKey("key4").Should().Be(items[1].Key);
            new MyValue("value4").Should().Be(items[1].Value);
            items.Length.Should().Be(2);
        }

        [TestMethod]
        public void TestGetChangeSet()
        {
            myDataCache.Add(new MyKey("key1"), new MyValue("value1"));
            Assert.AreEqual(TrackState.Added, myDataCache.GetChangeSet().Where(u => u.Key.Equals(new MyKey("key1"))).Select(u => u.State).FirstOrDefault());
            myDataCache.Add(new MyKey("key2"), new MyValue("value2"));
            Assert.AreEqual(TrackState.Added, myDataCache.GetChangeSet().Where(u => u.Key.Equals(new MyKey("key2"))).Select(u => u.State).FirstOrDefault());

            myDataCache.InnerDict.Add(new MyKey("key3"), new MyValue("value3"));
            myDataCache.InnerDict.Add(new MyKey("key4"), new MyValue("value4"));
            myDataCache.Delete(new MyKey("key3"));
            Assert.AreEqual(TrackState.Deleted, myDataCache.GetChangeSet().Where(u => u.Key.Equals(new MyKey("key3"))).Select(u => u.State).FirstOrDefault());
            myDataCache.Delete(new MyKey("key4"));
            Assert.AreEqual(TrackState.Deleted, myDataCache.GetChangeSet().Where(u => u.Key.Equals(new MyKey("key4"))).Select(u => u.State).FirstOrDefault());

            var items = myDataCache.GetChangeSet();
            int i = 0;
            foreach (var item in items)
            {
                i++;
                new MyKey("key" + i).Should().Be(item.Key);
                new MyValue("value" + i).Should().Be(item.Item);
            }
            i.Should().Be(4);
        }

        [TestMethod]
        public void TestGetAndChange()
        {
            myDataCache.Add(new MyKey("key1"), new MyValue("value1"));
            Assert.AreEqual(TrackState.Added, myDataCache.GetChangeSet().Where(u => u.Key.Equals(new MyKey("key1"))).Select(u => u.State).FirstOrDefault());
            myDataCache.InnerDict.Add(new MyKey("key2"), new MyValue("value2"));
            myDataCache.InnerDict.Add(new MyKey("key3"), new MyValue("value3"));
            myDataCache.Delete(new MyKey("key3"));
            Assert.AreEqual(TrackState.Deleted, myDataCache.GetChangeSet().Where(u => u.Key.Equals(new MyKey("key3"))).Select(u => u.State).FirstOrDefault());

            myDataCache.GetAndChange(new MyKey("key1"), () => new MyValue("value_bk_1")).Should().Be(new MyValue("value1"));
            myDataCache.GetAndChange(new MyKey("key2"), () => new MyValue("value_bk_2")).Should().Be(new MyValue("value2"));
            myDataCache.GetAndChange(new MyKey("key3"), () => new MyValue("value_bk_3")).Should().Be(new MyValue("value_bk_3"));
            myDataCache.GetAndChange(new MyKey("key4"), () => new MyValue("value_bk_4")).Should().Be(new MyValue("value_bk_4"));
        }

        [TestMethod]
        public void TestGetOrAdd()
        {
            myDataCache.Add(new MyKey("key1"), new MyValue("value1"));
            Assert.AreEqual(TrackState.Added, myDataCache.GetChangeSet().Where(u => u.Key.Equals(new MyKey("key1"))).Select(u => u.State).FirstOrDefault());
            myDataCache.InnerDict.Add(new MyKey("key2"), new MyValue("value2"));
            myDataCache.InnerDict.Add(new MyKey("key3"), new MyValue("value3"));
            myDataCache.Delete(new MyKey("key3"));
            Assert.AreEqual(TrackState.Deleted, myDataCache.GetChangeSet().Where(u => u.Key.Equals(new MyKey("key3"))).Select(u => u.State).FirstOrDefault());

            myDataCache.GetOrAdd(new MyKey("key1"), () => new MyValue("value_bk_1")).Should().Be(new MyValue("value1"));
            myDataCache.GetOrAdd(new MyKey("key2"), () => new MyValue("value_bk_2")).Should().Be(new MyValue("value2"));
            myDataCache.GetOrAdd(new MyKey("key3"), () => new MyValue("value_bk_3")).Should().Be(new MyValue("value_bk_3"));
            myDataCache.GetOrAdd(new MyKey("key4"), () => new MyValue("value_bk_4")).Should().Be(new MyValue("value_bk_4"));
        }

        [TestMethod]
        public void TestTryGet()
        {
            myDataCache.Add(new MyKey("key1"), new MyValue("value1"));
            Assert.AreEqual(TrackState.Added, myDataCache.GetChangeSet().Where(u => u.Key.Equals(new MyKey("key1"))).Select(u => u.State).FirstOrDefault());
            myDataCache.InnerDict.Add(new MyKey("key2"), new MyValue("value2"));
            myDataCache.InnerDict.Add(new MyKey("key3"), new MyValue("value3"));
            myDataCache.Delete(new MyKey("key3"));
            Assert.AreEqual(TrackState.Deleted, myDataCache.GetChangeSet().Where(u => u.Key.Equals(new MyKey("key3"))).Select(u => u.State).FirstOrDefault());

            myDataCache.TryGet(new MyKey("key1")).Should().Be(new MyValue("value1"));
            myDataCache.TryGet(new MyKey("key2")).Should().Be(new MyValue("value2"));
            myDataCache.TryGet(new MyKey("key3")).Should().BeNull();
        }

        [TestMethod]
        public void TestFindInvalid()
        {
            var myDataCache = new MyDataCache();
            myDataCache.Add(new MyKey("key1"), new MyValue("value1"));

            myDataCache.InnerDict.Add(new MyKey("key2"), new MyValue("value2"));
            myDataCache.InnerDict.Add(new MyKey("key3"), new MyValue("value3"));
            myDataCache.InnerDict.Add(new MyKey("key4"), new MyValue("value3"));

            var items = myDataCache.Find().GetEnumerator();
            items.MoveNext().Should().Be(true);
            items.Current.Key.Should().Be(new MyKey("key1"));

            myDataCache.TryGet(new MyKey("key3")); // GETLINE

            items.MoveNext().Should().Be(true);
            items.Current.Key.Should().Be(new MyKey("key2"));
            items.MoveNext().Should().Be(true);
            items.Current.Key.Should().Be(new MyKey("key3"));
            items.MoveNext().Should().Be(true);
            items.Current.Key.Should().Be(new MyKey("key4"));
            items.MoveNext().Should().Be(false);
        }
    }
}
