namespace Neo.IO.Caching
{
    internal class CloneMetaCache<T> : MetaDataCache<T>
        where T : class, ICloneable<T>, ISerializable, new()
    {
        private MetaDataCache<T> innerCache;

        public CloneMetaCache(MetaDataCache<T> innerCache)
            : base(null)
        {
            this.innerCache = innerCache;
        }

        protected override void AddInternal(T item)
        {
        }

        protected override T TryGetInternal()
        {
            return innerCache.Get().Clone();
        }

        protected override void UpdateInternal(T item)
        {
            innerCache.GetAndChange().FromReplica(item);
        }
    }
}
