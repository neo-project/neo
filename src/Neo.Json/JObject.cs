// Copyright (C) 2015-2023 The Neo Project.
//
// The Neo.Json is free software distributed under the MIT software license,
// see the accompanying file LICENSE in the main directory of the
// project or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System.Text.Json;

namespace Neo.Json
{
    /// <summary>
    /// Represents a JSON object.
    /// </summary>
    public class JObject : JContainer
    {
        private readonly OrderedDictionary<string, JToken?> properties = new();

        /// <summary>
        /// Gets or sets the properties of the JSON object.
        /// </summary>
        public IDictionary<string, JToken?> Properties => properties;

        /// <summary>
        /// Gets or sets the properties of the JSON object.
        /// </summary>
        /// <param name="name">The name of the property to get or set.</param>
        /// <returns>The property with the specified name.</returns>
        public override JToken? this[string name]
        {
            get
            {
                if (Properties.TryGetValue(name, out JToken? value))
                    return value;
                return null;
            }
            set
            {
                Properties[name] = value;
            }
        }

        public override IReadOnlyList<JToken?> Children => properties.Values;

        /// <summary>
        /// Determines whether the JSON object contains a property with the specified name.
        /// </summary>
        /// <param name="key">The property name to locate in the JSON object.</param>
        /// <returns><see langword="true"/> if the JSON object contains a property with the name; otherwise, <see langword="false"/>.</returns>
        public bool ContainsProperty(string key)
        {
            return Properties.ContainsKey(key);
        }

        public override void Clear() => properties.Clear();

        internal override void Write(Utf8JsonWriter writer)
        {
            writer.WriteStartObject();
            foreach (var (key, value) in Properties)
            {
                writer.WritePropertyName(key);
                if (value is null)
                    writer.WriteNullValue();
                else
                    value.Write(writer);
            }
            writer.WriteEndObject();
        }

        /// <summary>
        /// Creates a copy of the current JSON object.
        /// </summary>
        /// <returns>A copy of the current JSON object.</returns>
        public override JObject Clone()
        {
            var cloned = new JObject();

            foreach (var (key, value) in Properties)
            {
                cloned[key] = value != null ? value.Clone() : Null;
            }

            return cloned;
        }
    }
}
