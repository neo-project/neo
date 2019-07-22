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

    class MyKey : ISerializable, IEquatable<MyKey>
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
            if (obj is null) return false;
            if (!(obj is MyKey)) return false;
            return Equals((MyKey)obj);
        }

        public override int GetHashCode()
        {
            return Key.GetHashCode();
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
            if (obj is null) return false;
            if (!(obj is MyValue)) return false;
            return Equals((MyValue)obj);
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

        public override void DeleteInternal(TKey key)
        {
            InnerDict.Remove(key);
        }

        protected override void AddInternal(TKey key, TValue value)
        {
            InnerDict.Add(key, value);
        }

        protected override IEnumerable<KeyValuePair<TKey, TValue>> FindInternal(byte[] key_prefix)
        {
            return InnerDict.Where(kvp => kvp.Key.ToArray().Take(key_prefix.Length).SequenceEqual(key_prefix));
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
            action.ShouldThrow<KeyNotFoundException>();
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
            action.ShouldThrow<KeyNotFoundException>();
        }

        [TestMethod]
        public void TestAdd()
        {
            myDataCache.Add(new MyKey("key1"), new MyValue("value1"));
            myDataCache[new MyKey("key1")].Should().Be(new MyValue("value1"));

            Action action = () => myDataCache.Add(new MyKey("key1"), new MyValue("value1"));
            action.ShouldThrow<ArgumentException>();

            myDataCache.InnerDict.Add(new MyKey("key2"), new MyValue("value2"));
            myDataCache.Delete(new MyKey("key2"));                      // trackable.State = TrackState.Deleted    
            myDataCache.Add(new MyKey("key2"), new MyValue("value2"));  // trackable.State = TrackState.Changed

            action = () => myDataCache.Add(new MyKey("key2"), new MyValue("value2"));
            action.ShouldThrow<ArgumentException>();
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
        public void TestDeleteWhere()
        {
            myDataCache.Add(new MyKey("key1"), new MyValue("value1"));
            myDataCache.Add(new MyKey("key2"), new MyValue("value2"));

            myDataCache.InnerDict.Add(new MyKey("key3"), new MyValue("value3"));
            myDataCache.InnerDict.Add(new MyKey("key4"), new MyValue("value4"));

            myDataCache.DeleteWhere((k, v) => k.Key.StartsWith("key"));
            myDataCache.Commit();
            myDataCache.TryGet(new MyKey("key1")).Should().BeNull();
            myDataCache.TryGet(new MyKey("key2")).Should().BeNull();
            myDataCache.InnerDict.ContainsKey(new MyKey("key1")).Should().BeFalse();
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
    }
}