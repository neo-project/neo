using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO.Caching;

namespace Neo.UnitTests.IO.Caching
{
    [TestClass]
    public class UT_CloneMetaCache
    {

        MyMetaCache<MyValue> myMetaCache;
        CloneMetaCache<MyValue> cloneMetaCache;

        [TestInitialize]
        public void Init()
        {
            myMetaCache = new MyMetaCache<MyValue>(() => new MyValue());
            cloneMetaCache = new CloneMetaCache<MyValue>(myMetaCache);
        }

        [TestMethod]
        public void TestConstructor()
        {
            cloneMetaCache.Should().NotBeNull();
        }

        [TestMethod]
        public void TestAddInternal()
        {
            // Skip, as CloneMetaCache.AddInternal cannot be reached
        }

        [TestMethod]
        public void TestTryGetInternal()
        {
            MyValue value = myMetaCache.GetAndChange();
            value.Value = "value1";

            cloneMetaCache.Get().Should().Be(value);
        }

        [TestMethod]
        public void TestUpdateInternal()
        {
            MyValue value = myMetaCache.GetAndChange();
            value.Value = "value1";

            MyValue value2 = cloneMetaCache.GetAndChange();
            value2.Value = "value2";

            cloneMetaCache.Commit();
            value.Value.Should().Be("value2");
        }
    }
}