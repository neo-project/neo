using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Neo.IO.Json
{
    public class JArray : JObject, IList<JObject>
    {
        private List<JObject> items = new List<JObject>();

        public JArray(params JObject[] items) : this((IEnumerable<JObject>)items)
        {
        }

        public JArray(IEnumerable<JObject> items)
        {
            this.items.AddRange(items);
        }

        public JObject this[int index]
        {
            get
            {
                return items[index];
            }
            set
            {
                items[index] = value;
            }
        }

        public int Count
        {
            get
            {
                return items.Count;
            }
        }

        public bool IsReadOnly
        {
            get
            {
                return false;
            }
        }

        public void Add(JObject item)
        {
            items.Add(item);
        }

        public void Clear()
        {
            items.Clear();
        }

        public bool Contains(JObject item)
        {
            return items.Contains(item);
        }

        public void CopyTo(JObject[] array, int arrayIndex)
        {
            items.CopyTo(array, arrayIndex);
        }

        public IEnumerator<JObject> GetEnumerator()
        {
            return items.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public int IndexOf(JObject item)
        {
            return items.IndexOf(item);
        }

        public void Insert(int index, JObject item)
        {
            items.Insert(index, item);
        }

        internal new static JArray Parse(TextReader reader, int max_nest)
        {
            if (max_nest < 0) throw new FormatException();
            SkipSpace(reader);
            if (reader.Read() != '[') throw new FormatException();
            SkipSpace(reader);
            JArray array = new JArray();
            while (reader.Peek() != ']')
            {
                if (reader.Peek() == ',') reader.Read();
                JObject obj = JObject.Parse(reader, max_nest - 1);
                array.items.Add(obj);
                SkipSpace(reader);
            }
            reader.Read();
            return array;
        }

        public bool Remove(JObject item)
        {
            return items.Remove(item);
        }

        public void RemoveAt(int index)
        {
            items.RemoveAt(index);
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append('[');
            foreach (JObject item in items)
            {
                if (item == null)
                    sb.Append("null");
                else
                    sb.Append(item);
                sb.Append(',');
            }
            if (items.Count == 0)
            {
                sb.Append(']');
            }
            else
            {
                sb[sb.Length - 1] = ']';
            }
            return sb.ToString();
        }
    }
}
