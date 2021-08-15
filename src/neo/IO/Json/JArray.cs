// Copyright (C) 2014-2021 NEO GLOBAL DEVELOPMENT.
// 
// The neo is free software distributed under the MIT software license, 
// see the accompanying file LICENSE in the main directory or 
// the project http://www.opensource.org/licenses/mit-license.php 
// for more details.
// 
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Neo.IO.Json
{
    /// <summary>
    /// Represents a JSON array.
    /// </summary>
    public class JArray : JObject, IList<JObject>
    {
        private readonly List<JObject> items = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="JArray"/> class.
        /// </summary>
        /// <param name="items">The initial items in the array.</param>
        public JArray(params JObject[] items) : this((IEnumerable<JObject>)items)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="JArray"/> class.
        /// </summary>
        /// <param name="items">The initial items in the array.</param>
        public JArray(IEnumerable<JObject> items)
        {
            this.items.AddRange(items);
        }

        public override JObject this[int index]
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
            return string.Join(",", items.Select(p => p?.AsString()));
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

        public override JArray GetArray() => this;

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

        public bool Remove(JObject item)
        {
            return items.Remove(item);
        }

        public void RemoveAt(int index)
        {
            items.RemoveAt(index);
        }

        internal override void Write(Utf8JsonWriter writer)
        {
            writer.WriteStartArray();
            foreach (JObject item in items)
            {
                if (item is null)
                    writer.WriteNullValue();
                else
                    item.Write(writer);
            }
            writer.WriteEndArray();
        }

        public override JObject Clone()
        {
            var cloned = new JArray();

            foreach (JObject item in items)
            {
                cloned.Add(item.Clone());
            }

            return cloned;
        }

        public static implicit operator JArray(JObject[] value)
        {
            return new JArray(value);
        }
    }
}
