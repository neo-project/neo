using System;

namespace Neo.IO.Caching
{
    public abstract class MetaDataCache<T> where T : class, ISerializable, new()
    {
        protected T Item;
        protected TrackState State;

        protected abstract T TryGetInternal();

        protected MetaDataCache(Func<T> factory)
        {
            this.Item = TryGetInternal();
            if (this.Item == null)
            {
                this.Item = factory?.Invoke() ?? new T();
                this.State = TrackState.Added;
            }
        }

        public T Get()
        {
            return Item;
        }

        public T GetAndChange()
        {
            if (State == TrackState.None)
                State = TrackState.Changed;
            return Item;
        }
    }
}
