using Neo.IO;
using Neo.IO.Caching;

namespace Neo.UnitTests
{
    public class TestMetaDataCache<T> : MetaDataCache<T> where T : class, ISerializable, new()
    {
        public TestMetaDataCache()
            : base(null)
        {
        }

        protected override T TryGetInternal()
        {
            return null;
        }
    }
}
