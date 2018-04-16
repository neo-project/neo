using System;

namespace Neo.IO.Caching
{
    public abstract class MetaDataCache<T> where T : class, ISerializable, new()
    {
        protected T Item;
        protected TrackState State;
        private Func<T> factory;

        protected abstract T TryGetInternal();

        protected MetaDataCache(Func<T> factory)
        {
            this.factory = factory;
        }

        public T Get()
        {
            if (Item == null)
            {
                Item = TryGetInternal();
            }
            if (Item == null)
            {
                Item = factory?.Invoke() ?? new T();
                State = TrackState.Added;
            }
            return Item;
        }

        public T GetAndChange()
        {
            T item = Get();
            if (State == TrackState.None)
                State = TrackState.Changed;
            return item;
        }
    }
}
