// Copyright (C) 2015-2025 The Neo Project.
//
// JArray.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System.Collections;
using System.Text.Json;

namespace Neo.Json
{
    /// <summary>
    /// Represents a JSON array.
    /// </summary>
    public class JArray : JContainer, IList<JToken?>
    {
        private readonly List<JToken?> _items = [];

        /// <summary>
        /// Initializes a new instance of the <see cref="JArray"/> class.
        /// </summary>
        /// <param name="items">The initial items in the array.</param>
        public JArray(params JToken?[] items) : this((IEnumerable<JToken?>)items)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="JArray"/> class.
        /// </summary>
        /// <param name="items">The initial items in the array.</param>
        public JArray(IEnumerable<JToken?> items)
        {
            _items.AddRange(items);
        }

        public override JToken? this[int index]
        {
            get
            {
                return _items[index];
            }
            set
            {
                _items[index] = value;
            }
        }

        public override IReadOnlyList<JToken?> Children => _items;

        public bool IsReadOnly
        {
            get
            {
                return false;
            }
        }

        public void Add(JToken? item)
        {
            _items.Add(item);
        }

        public override string AsString()
        {
            return ToString();
        }

        public override void Clear()
        {
            _items.Clear();
        }

        public bool Contains(JToken? item)
        {
            return _items.Contains(item);
        }

        public IEnumerator<JToken?> GetEnumerator()
        {
            return _items.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public int IndexOf(JToken? item)
        {
            return _items.IndexOf(item);
        }

        public void Insert(int index, JToken? item)
        {
            _items.Insert(index, item);
        }

        public bool Remove(JToken? item)
        {
            return _items.Remove(item);
        }

        public void RemoveAt(int index)
        {
            _items.RemoveAt(index);
        }

        internal override void Write(Utf8JsonWriter writer)
        {
            writer.WriteStartArray();
            foreach (var item in _items)
            {
                if (item is null)
                    writer.WriteNullValue();
                else
                    item.Write(writer);
            }
            writer.WriteEndArray();
        }

        public override JToken Clone()
        {
            var cloned = new JArray();

            foreach (var item in _items)
            {
                cloned.Add(item?.Clone());
            }

            return cloned;
        }

        public static implicit operator JArray(JToken?[] value)
        {
            return [.. value];
        }
    }
}
