using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace AntShares.Threading
{
    internal class ConcurrentSet<T> : ISet<T>
    {
        private ConcurrentDictionary<T, object> base_dictionary = new ConcurrentDictionary<T, object>();

        public int Count
        {
            get
            {
                return base_dictionary.Count;
            }
        }

        public bool IsReadOnly
        {
            get
            {
                return false;
            }
        }

        public bool Add(T item)
        {
            return base_dictionary.TryAdd(item, null);
        }

        void ICollection<T>.Add(T item)
        {
            this.Add(item);
        }

        public void Clear()
        {
            base_dictionary.Clear();
        }

        public bool Contains(T item)
        {
            return base_dictionary.ContainsKey(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            foreach (T item in this)
                array[arrayIndex++] = item;
        }

        public void ExceptWith(IEnumerable<T> other)
        {
            foreach (T item in other)
                this.Remove(item);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return base_dictionary.Keys.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        public void IntersectWith(IEnumerable<T> other)
        {
            foreach (T item in this.ToArray())
                if (!other.Contains(item))
                    this.Remove(item);
        }

        public bool IsProperSubsetOf(IEnumerable<T> other)
        {
            return this.Count < other.Count() && this.IsSubsetOf(other);
        }

        public bool IsProperSupersetOf(IEnumerable<T> other)
        {
            return this.Count > other.Count() && this.IsSupersetOf(other);
        }

        public bool IsSubsetOf(IEnumerable<T> other)
        {
            foreach (T item in this)
                if (!other.Contains(item))
                    return false;
            return true;
        }

        public bool IsSupersetOf(IEnumerable<T> other)
        {
            foreach (T item in other)
                if (!this.Contains(item))
                    return false;
            return true;
        }

        public bool Overlaps(IEnumerable<T> other)
        {
            foreach (T item in this)
                if (other.Contains(item))
                    return true;
            return false;
        }

        public bool Remove(T item)
        {
            object ignore;
            return base_dictionary.TryRemove(item, out ignore);
        }

        public bool SetEquals(IEnumerable<T> other)
        {
            return this.Count == other.Count() && this.All(p => other.Contains(p));
        }

        public void SymmetricExceptWith(IEnumerable<T> other)
        {
            List<T> intersect = new List<T>();
            foreach (T item in this.ToArray())
                if (other.Contains(item))
                {
                    this.Remove(item);
                    intersect.Add(item);
                }
            foreach (T item in other)
                if (!intersect.Contains(item))
                    this.Add(item);
        }

        public void UnionWith(IEnumerable<T> other)
        {
            foreach (T item in other)
                this.Add(item);
        }
    }
}
