using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.IO.Caching;

namespace Neo.UnitTests.IO.Caching
{
    [TestClass]
    public class UT_CloneMetaCache
    {
        [TestMethod]
        public void TestConstructor()
        {
            var myMetaCache = new MyMetaCache<MyValue>(() => new MyValue());
            var cloneMetaCache = new CloneMetaCache<MyValue>(myMetaCache);
            cloneMetaCache.Should().NotBeNull();
        }

        [TestMethod]
        public void TestTryGetInternal()
        {
            var myMetaCache = new MyMetaCache<MyValue>(() => new MyValue());
            var cloneMetaCache = new CloneMetaCache<MyValue>(myMetaCache);
            MyValue value = myMetaCache.GetAndChange();
            value.Value = "value1";

            cloneMetaCache.Get().Should().Be(value);
        }

        [TestMethod]
        public void TestUpdateInternal()
        {
            var myMetaCache = new MyMetaCache<MyValue>(() => new MyValue());
            var cloneMetaCache = new CloneMetaCache<MyValue>(myMetaCache);

            MyValue value = myMetaCache.GetAndChange();
            value.Value = "value1";

            MyValue value2 = cloneMetaCache.GetAndChange();
            value2.Value = "value2";

            cloneMetaCache.Commit();
            value.Value.Should().Be("value2");
        }
    }
}
