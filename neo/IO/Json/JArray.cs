using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Neo.IO.Json
{
    public class JArray : JObject, IList<JObject>
    {
        private readonly List<JObject> items = new List<JObject>();

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

        public override string AsString()
        {
            return string.Join(VALUE_SEPARATOR.ToString(), items.Select(p => p?.AsString()));
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
            SkipSpace(reader);
            if (reader.Read() != BEGIN_ARRAY) throw new FormatException();
            JArray array = new JArray();
            SkipSpace(reader);
            if (reader.Peek() != END_ARRAY)
            {
                while (true)
                {
                    JObject obj = JObject.Parse(reader, max_nest - 1);
                    array.items.Add(obj);
                    SkipSpace(reader);
                    char nextchar = (char)reader.Read();
                    if (nextchar == VALUE_SEPARATOR) continue;
                    if (nextchar == END_ARRAY) break;
                    throw new FormatException();
                }
            }
            else
            {
                reader.Read();
            }
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
            sb.Append(BEGIN_ARRAY);
            foreach (JObject item in items)
            {
                if (item == null)
                    sb.Append(LITERAL_NULL);
                else
                    sb.Append(item);
                sb.Append(VALUE_SEPARATOR);
            }
            if (items.Count == 0)
            {
                sb.Append(END_ARRAY);
            }
            else
            {
                sb[sb.Length - 1] = END_ARRAY;
            }
            return sb.ToString();
        }
    }
}
