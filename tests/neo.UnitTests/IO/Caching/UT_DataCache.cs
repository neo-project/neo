using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO;
using Neo.IO.Caching;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Neo.UnitTests.IO.Caching
{
    class MyKey : ISerializable, IEquatable<MyKey>, IComparable<MyKey>
    {
        public string Key;

        public int Size => Key.Length;

        public MyKey() { }

        public MyKey(string val)
        {
            Key = val;
        }

        public void Deserialize(BinaryReader reader)
        {
            Key = reader.ReadString();
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(Key);
        }

        public bool Equals(MyKey other)
        {
            return Key.Equals(other.Key);
        }

        public override bool Equals(object obj)
        {
            if (!(obj is MyKey key)) return false;
            return Equals(key);
        }

        public override int GetHashCode()
        {
            return Key.GetHashCode();
        }

        public int CompareTo(MyKey obj)
        {
            return Key.CompareTo(obj.Key);
        }
    }

    public class MyValue : ISerializable, ICloneable<MyValue>
    {
        public string Value;

        public int Size => Value.Length;

        public MyValue() { }

        public MyValue(string val)
        {
            Value = val;
        }

        public void Deserialize(BinaryReader reader)
        {
            Value = reader.ReadString();
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(Value);
        }

        MyValue ICloneable<MyValue>.Clone()
        {
            return new MyValue(Value);
        }

        void ICloneable<MyValue>.FromReplica(MyValue replica)
        {
            Value = replica.Value;
        }

        public bool Equals(MyValue other)
        {
            return (Value == null && other.Value == null) || Value.Equals(other.Value);
        }

        public override bool Equals(object obj)
        {
            if (!(obj is MyValue key)) return false;
            return Equals(key);
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }
    }

    class MyDataCache<TKey, TValue> : DataCache<TKey, TValue>
           where TKey : IEquatable<TKey>, ISerializable, new()
           where TValue : class, ICloneable<TValue>, ISerializable, new()
    {
        public Dictionary<TKey, TValue> InnerDict = new Dictionary<TKey, TValue>();

        protected override void DeleteInternal(TKey key)
        {
            InnerDict.Remove(key);
        }

        protected override void AddInternal(TKey key, TValue value)
        {
            InnerDict.Add(key, value);
        }

        protected override IEnumerable<(TKey, TValue)> SeekInternal(byte[] keyOrPrefix, SeekDirection direction = SeekDirection.Forward)
        {
            if (direction == SeekDirection.Forward)
                return InnerDict.OrderBy(kvp => kvp.Key).Where(kvp => ByteArrayComparer.Default.Compare(kvp.Key.ToArray(), keyOrPrefix) >= 0).Select(p => (p.Key, p.Value));
            else
                return InnerDict.OrderByDescending(kvp => kvp.Key).Where(kvp => ByteArrayComparer.Reverse.Compare(kvp.Key.ToArray(), keyOrPrefix) >= 0).Select(p => (p.Key, p.Value));
        }

        protected override TValue GetInternal(TKey key)
        {
            if (InnerDict.TryGetValue(key, out TValue value))
            {
                return value.Clone();
            }
            throw new KeyNotFoundException();
        }

        protected override TValue TryGetInternal(TKey key)
        {
            if (InnerDict.TryGetValue(key, out TValue value))
            {
                return value.Clone();
            }
            return null;
        }

        protected override bool ContainsInternal(TKey key)
        {
            return InnerDict.ContainsKey(key);
        }

        protected override void UpdateInternal(TKey key, TValue value)
        {
            InnerDict[key] = value;
        }
    }

    [TestClass]
    public class UT_DataCache
    {
        MyDataCache<MyKey, MyValue> myDataCache;

        [TestInitialize]
        public void Initialize()
        {
            myDataCache = new MyDataCache<MyKey, MyValue>();
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
            myDataCache.Delete(new MyKey("key2"));                      // trackable.State = TrackState.Deleted    
            myDataCache.Add(new MyKey("key2"), new MyValue("value2"));  // trackable.State = TrackState.Changed

            action = () => myDataCache.Add(new MyKey("key2"), new MyValue("value2"));
            action.Should().Throw<ArgumentException>();
        }

        [TestMethod]
        public void TestCommit()
        {
            myDataCache.Add(new MyKey("key1"), new MyValue("value1"));   // trackable.State = TrackState.Added    

            myDataCache.InnerDict.Add(new MyKey("key2"), new MyValue("value2"));
            myDataCache.Delete(new MyKey("key2"));       // trackable.State = TrackState.Deleted    

            myDataCache.InnerDict.Add(new MyKey("key3"), new MyValue("value3"));
            myDataCache.Delete(new MyKey("key3"));                      // trackable.State = TrackState.Deleted    
            myDataCache.Add(new MyKey("key3"), new MyValue("value4"));  // trackable.State = TrackState.Changed

            myDataCache.Commit();

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
            items.ElementAt(0).Key.Should().Be(new MyKey("key1"));
            items.ElementAt(0).Value.Should().Be(new MyValue("value1"));
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
            items[0].Key.Should().Be(new MyKey("key3"));
            items[0].Value.Should().Be(new MyValue("value3"));
            items[1].Key.Should().Be(new MyKey("key2"));
            items[1].Value.Should().Be(new MyValue("value2"));
            items.Count().Should().Be(3);

            items = myDataCache.Seek(new MyKey("key5").ToArray(), SeekDirection.Forward).ToArray();
            items.Count().Should().Be(0);
        }

        [TestMethod]
        public void TestFindRange()
        {
            myDataCache.Add(new MyKey("key1"), new MyValue("value1"));
            myDataCache.Add(new MyKey("key2"), new MyValue("value2"));

            myDataCache.InnerDict.Add(new MyKey("key3"), new MyValue("value3"));
            myDataCache.InnerDict.Add(new MyKey("key4"), new MyValue("value4"));

            var items = myDataCache.FindRange(new MyKey("key3"), new MyKey("key5")).ToArray();
            items[0].Key.Should().Be(new MyKey("key3"));
            items[0].Value.Should().Be(new MyValue("value3"));
            items[1].Key.Should().Be(new MyKey("key4"));
            items[1].Value.Should().Be(new MyValue("value4"));
            items.Count().Should().Be(2);

            // case 2 Need to sort the cache of myDataCache

            myDataCache = new MyDataCache<MyKey, MyValue>();
            myDataCache.Add(new MyKey("key1"), new MyValue("value1"));
            myDataCache.Add(new MyKey("key2"), new MyValue("value2"));

            myDataCache.InnerDict.Add(new MyKey("key4"), new MyValue("value4")); // if we cache first the key4 it will fail
            myDataCache.InnerDict.Add(new MyKey("key3"), new MyValue("value3"));

            items = myDataCache.FindRange(new MyKey("key3"), new MyKey("key5")).ToArray();
            items[0].Key.Should().Be(new MyKey("key3"));
            items[0].Value.Should().Be(new MyValue("value3"));
            items[1].Key.Should().Be(new MyKey("key4"));
            items[1].Value.Should().Be(new MyValue("value4"));
            items.Count().Should().Be(2);

            // case 3 FindRange by Backward

            myDataCache = new MyDataCache<MyKey, MyValue>();
            myDataCache.Add(new MyKey("key1"), new MyValue("value1"));
            myDataCache.Add(new MyKey("key2"), new MyValue("value2"));

            myDataCache.InnerDict.Add(new MyKey("key4"), new MyValue("value4")); // if we cache first the key4 it will fail
            myDataCache.InnerDict.Add(new MyKey("key3"), new MyValue("value3"));
            myDataCache.InnerDict.Add(new MyKey("key5"), new MyValue("value5"));

            items = myDataCache.FindRange(new MyKey("key3"), new MyKey("key5"), SeekDirection.Backward).ToArray();
            items[0].Key.Should().Be(new MyKey("key5"));
            items[0].Value.Should().Be(new MyValue("value5"));
            items[1].Key.Should().Be(new MyKey("key4"));
            items[1].Value.Should().Be(new MyValue("value4"));
            items.Count().Should().Be(2);
        }

        [TestMethod]
        public void TestGetChangeSet()
        {
            myDataCache.Add(new MyKey("key1"), new MyValue("value1"));  // trackable.State = TrackState.Added 
            myDataCache.Add(new MyKey("key2"), new MyValue("value2"));  // trackable.State = TrackState.Added 

            myDataCache.InnerDict.Add(new MyKey("key3"), new MyValue("value3"));
            myDataCache.InnerDict.Add(new MyKey("key4"), new MyValue("value4"));
            myDataCache.Delete(new MyKey("key3"));      // trackable.State = TrackState.Deleted 
            myDataCache.Delete(new MyKey("key4"));      // trackable.State = TrackState.Deleted 

            var items = myDataCache.GetChangeSet();
            int i = 0;
            foreach (var item in items)
            {
                i++;
                item.Key.Should().Be(new MyKey("key" + i));
                item.Item.Should().Be(new MyValue("value" + i));
            }
            i.Should().Be(4);
        }

        [TestMethod]
        public void TestGetAndChange()
        {
            myDataCache.Add(new MyKey("key1"), new MyValue("value1"));                  //  trackable.State = TrackState.Added 
            myDataCache.InnerDict.Add(new MyKey("key2"), new MyValue("value2"));
            myDataCache.InnerDict.Add(new MyKey("key3"), new MyValue("value3"));
            myDataCache.Delete(new MyKey("key3"));                                      //  trackable.State = TrackState.Deleted 

            myDataCache.GetAndChange(new MyKey("key1"), () => new MyValue("value_bk_1")).Should().Be(new MyValue("value1"));
            myDataCache.GetAndChange(new MyKey("key2"), () => new MyValue("value_bk_2")).Should().Be(new MyValue("value2"));
            myDataCache.GetAndChange(new MyKey("key3"), () => new MyValue("value_bk_3")).Should().Be(new MyValue("value_bk_3"));
            myDataCache.GetAndChange(new MyKey("key4"), () => new MyValue("value_bk_4")).Should().Be(new MyValue("value_bk_4"));
        }

        [TestMethod]
        public void TestGetOrAdd()
        {
            myDataCache.Add(new MyKey("key1"), new MyValue("value1"));                  //  trackable.State = TrackState.Added 
            myDataCache.InnerDict.Add(new MyKey("key2"), new MyValue("value2"));
            myDataCache.InnerDict.Add(new MyKey("key3"), new MyValue("value3"));
            myDataCache.Delete(new MyKey("key3"));                                      //  trackable.State = TrackState.Deleted 

            myDataCache.GetOrAdd(new MyKey("key1"), () => new MyValue("value_bk_1")).Should().Be(new MyValue("value1"));
            myDataCache.GetOrAdd(new MyKey("key2"), () => new MyValue("value_bk_2")).Should().Be(new MyValue("value2"));
            myDataCache.GetOrAdd(new MyKey("key3"), () => new MyValue("value_bk_3")).Should().Be(new MyValue("value_bk_3"));
            myDataCache.GetOrAdd(new MyKey("key4"), () => new MyValue("value_bk_4")).Should().Be(new MyValue("value_bk_4"));
        }

        [TestMethod]
        public void TestTryGet()
        {
            myDataCache.Add(new MyKey("key1"), new MyValue("value1"));                  //  trackable.State = TrackState.Added 
            myDataCache.InnerDict.Add(new MyKey("key2"), new MyValue("value2"));
            myDataCache.InnerDict.Add(new MyKey("key3"), new MyValue("value3"));
            myDataCache.Delete(new MyKey("key3"));                                      //  trackable.State = TrackState.Deleted 

            myDataCache.TryGet(new MyKey("key1")).Should().Be(new MyValue("value1"));
            myDataCache.TryGet(new MyKey("key2")).Should().Be(new MyValue("value2"));
            myDataCache.TryGet(new MyKey("key3")).Should().BeNull();
        }

        [TestMethod]
        public void TestFindInvalid()
        {
            var myDataCache = new MyDataCache<MyKey, MyValue>();
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
