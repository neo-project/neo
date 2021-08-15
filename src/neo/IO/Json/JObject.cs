// Copyright (C) 2015-2021 NEO GLOBAL DEVELOPMENT.
// 
// The Neo project is free software distributed under the MIT software license, 
// see the accompanying file LICENSE in the main directory of the
// project or http://www.opensource.org/licenses/mit-license.php 
// for more details.
// 
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.IO.Caching;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Neo.IO.Json
{
    /// <summary>
    /// Represents a JSON object.
    /// </summary>
    public class JObject
    {
        /// <summary>
        /// Represents a <see langword="null"/> object.
        /// </summary>
        public static readonly JObject Null = null;

        private readonly OrderedDictionary<string, JObject> properties = new();

        /// <summary>
        /// Gets or sets the properties of the JSON object.
        /// </summary>
        public IDictionary<string, JObject> Properties => properties;

        /// <summary>
        /// Gets or sets the properties of the JSON object.
        /// </summary>
        /// <param name="name">The name of the property to get or set.</param>
        /// <returns>The property with the specified name.</returns>
        public JObject this[string name]
        {
            get
            {
                if (Properties.TryGetValue(name, out JObject value))
                    return value;
                return null;
            }
            set
            {
                Properties[name] = value;
            }
        }

        /// <summary>
        /// Gets the property at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index of the property to get.</param>
        /// <returns>The property at the specified index.</returns>
        public virtual JObject this[int index]
        {
            get => properties[index];
            set => throw new NotSupportedException();
        }

        /// <summary>
        /// Converts the current JSON object to a boolean value.
        /// </summary>
        /// <returns>The converted value.</returns>
        public virtual bool AsBoolean()
        {
            return true;
        }

        /// <summary>
        /// Converts the current JSON object to a floating point number.
        /// </summary>
        /// <returns>The converted value.</returns>
        public virtual double AsNumber()
        {
            return double.NaN;
        }

        /// <summary>
        /// Converts the current JSON object to a <see cref="string"/>.
        /// </summary>
        /// <returns>The converted value.</returns>
        public virtual string AsString()
        {
            return ToString();
        }

        /// <summary>
        /// Determines whether the JSON object contains a property with the specified name.
        /// </summary>
        /// <param name="key">The property name to locate in the JSON object.</param>
        /// <returns><see langword="true"/> if the JSON object contains a property with the name; otherwise, <see langword="false"/>.</returns>
        public bool ContainsProperty(string key)
        {
            return Properties.ContainsKey(key);
        }

        /// <summary>
        /// Converts the current JSON object to a <see cref="JArray"/> object.
        /// </summary>
        /// <returns>The converted value.</returns>
        /// <exception cref="InvalidCastException">The JSON object is not a <see cref="JArray"/>.</exception>
        public virtual JArray GetArray() => throw new InvalidCastException();

        /// <summary>
        /// Converts the current JSON object to a boolean value.
        /// </summary>
        /// <returns>The converted value.</returns>
        /// <exception cref="InvalidCastException">The JSON object is not a <see cref="JBoolean"/>.</exception>
        public virtual bool GetBoolean() => throw new InvalidCastException();

        /// <summary>
        /// Converts the current JSON object to a 32-bit signed integer.
        /// </summary>
        /// <returns>The converted value.</returns>
        /// <exception cref="InvalidCastException">The JSON object is not a <see cref="JNumber"/>.</exception>
        /// <exception cref="InvalidCastException">The JSON object cannot be converted to an integer.</exception>
        /// <exception cref="OverflowException">The JSON object cannot be converted to a 32-bit signed integer.</exception>
        public int GetInt32()
        {
            double d = GetNumber();
            if (d % 1 != 0) throw new InvalidCastException();
            return checked((int)d);
        }

        /// <summary>
        /// Converts the current JSON object to a floating point number.
        /// </summary>
        /// <returns>The converted value.</returns>
        /// <exception cref="InvalidCastException">The JSON object is not a <see cref="JNumber"/>.</exception>
        public virtual double GetNumber() => throw new InvalidCastException();

        /// <summary>
        /// Converts the current JSON object to a <see cref="string"/>.
        /// </summary>
        /// <returns>The converted value.</returns>
        /// <exception cref="InvalidCastException">The JSON object is not a <see cref="JString"/>.</exception>
        public virtual string GetString() => throw new InvalidCastException();

        /// <summary>
        /// Parses a JSON object from a byte array.
        /// </summary>
        /// <param name="value">The byte array that contains the JSON object.</param>
        /// <param name="max_nest">The maximum nesting depth when parsing the JSON object.</param>
        /// <returns>The parsed JSON object.</returns>
        public static JObject Parse(ReadOnlySpan<byte> value, int max_nest = 100)
        {
            Utf8JsonReader reader = new(value, new JsonReaderOptions
            {
                AllowTrailingCommas = false,
                CommentHandling = JsonCommentHandling.Skip,
                MaxDepth = max_nest
            });
            try
            {
                JObject json = Read(ref reader);
                if (reader.Read()) throw new FormatException();
                return json;
            }
            catch (JsonException ex)
            {
                throw new FormatException(ex.Message, ex);
            }
        }

        /// <summary>
        /// Parses a JSON object from a <see cref="string"/>.
        /// </summary>
        /// <param name="value">The <see cref="string"/> that contains the JSON object.</param>
        /// <param name="max_nest">The maximum nesting depth when parsing the JSON object.</param>
        /// <returns>The parsed JSON object.</returns>
        public static JObject Parse(string value, int max_nest = 100)
        {
            return Parse(Utility.StrictUTF8.GetBytes(value), max_nest);
        }

        private static JObject Read(ref Utf8JsonReader reader, bool skipReading = false)
        {
            if (!skipReading && !reader.Read()) throw new FormatException();
            return reader.TokenType switch
            {
                JsonTokenType.False => false,
                JsonTokenType.Null => Null,
                JsonTokenType.Number => reader.GetDouble(),
                JsonTokenType.StartArray => ReadArray(ref reader),
                JsonTokenType.StartObject => ReadObject(ref reader),
                JsonTokenType.String => ReadString(ref reader),
                JsonTokenType.True => true,
                _ => throw new FormatException(),
            };
        }

        private static JArray ReadArray(ref Utf8JsonReader reader)
        {
            JArray array = new();
            while (reader.Read())
            {
                switch (reader.TokenType)
                {
                    case JsonTokenType.EndArray:
                        return array;
                    default:
                        array.Add(Read(ref reader, skipReading: true));
                        break;
                }
            }
            throw new FormatException();
        }

        private static JObject ReadObject(ref Utf8JsonReader reader)
        {
            JObject obj = new();
            while (reader.Read())
            {
                switch (reader.TokenType)
                {
                    case JsonTokenType.EndObject:
                        return obj;
                    case JsonTokenType.PropertyName:
                        string name = ReadString(ref reader);
                        if (obj.Properties.ContainsKey(name)) throw new FormatException();
                        JObject value = Read(ref reader);
                        obj.Properties.Add(name, value);
                        break;
                    default:
                        throw new FormatException();
                }
            }
            throw new FormatException();
        }

        private static string ReadString(ref Utf8JsonReader reader)
        {
            try
            {
                return reader.GetString();
            }
            catch (InvalidOperationException ex)
            {
                throw new FormatException(ex.Message, ex);
            }
        }

        /// <summary>
        /// Encode the current JSON object into a byte array.
        /// </summary>
        /// <param name="indented">Indicates whether indentation is required.</param>
        /// <returns>The encoded JSON object.</returns>
        public byte[] ToByteArray(bool indented)
        {
            using MemoryStream ms = new();
            using Utf8JsonWriter writer = new(ms, new JsonWriterOptions
            {
                Indented = indented,
                SkipValidation = true
            });
            Write(writer);
            writer.Flush();
            return ms.ToArray();
        }

        /// <summary>
        /// Encode the current JSON object into a <see cref="string"/>.
        /// </summary>
        /// <returns>The encoded JSON object.</returns>
        public override string ToString()
        {
            return ToString(false);
        }

        /// <summary>
        /// Encode the current JSON object into a <see cref="string"/>.
        /// </summary>
        /// <param name="indented">Indicates whether indentation is required.</param>
        /// <returns>The encoded JSON object.</returns>
        public string ToString(bool indented)
        {
            return Utility.StrictUTF8.GetString(ToByteArray(indented));
        }

        /// <summary>
        /// Converts the current JSON object to an <see cref="Enum"/>.
        /// </summary>
        /// <typeparam name="T">The type of the <see cref="Enum"/>.</typeparam>
        /// <param name="defaultValue">If the current JSON object cannot be converted to type <typeparamref name="T"/>, then the default value is returned.</param>
        /// <param name="ignoreCase">Indicates whether case should be ignored during conversion.</param>
        /// <returns>The converted value.</returns>
        public virtual T TryGetEnum<T>(T defaultValue = default, bool ignoreCase = false) where T : Enum
        {
            return defaultValue;
        }

        internal virtual void Write(Utf8JsonWriter writer)
        {
            writer.WriteStartObject();
            foreach (KeyValuePair<string, JObject> pair in Properties)
            {
                writer.WritePropertyName(pair.Key);
                if (pair.Value is null)
                    writer.WriteNullValue();
                else
                    pair.Value.Write(writer);
            }
            writer.WriteEndObject();
        }

        /// <summary>
        /// Creates a copy of the current JSON object.
        /// </summary>
        /// <returns>A copy of the current JSON object.</returns>
        public virtual JObject Clone()
        {
            var cloned = new JObject();

            foreach (KeyValuePair<string, JObject> pair in Properties)
            {
                cloned[pair.Key] = pair.Value != null ? pair.Value.Clone() : Null;
            }

            return cloned;
        }

        public JArray JsonPath(string expr)
        {
            JObject[] objects = { this };
            if (expr.Length == 0) return objects;
            Queue<JPathToken> tokens = new(JPathToken.Parse(expr));
            JPathToken first = tokens.Dequeue();
            if (first.Type != JPathTokenType.Root) throw new FormatException();
            JPathToken.ProcessJsonPath(ref objects, tokens);
            return objects;
        }

        public static implicit operator JObject(Enum value)
        {
            return (JString)value;
        }

        public static implicit operator JObject(JObject[] value)
        {
            return (JArray)value;
        }

        public static implicit operator JObject(bool value)
        {
            return (JBoolean)value;
        }

        public static implicit operator JObject(double value)
        {
            return (JNumber)value;
        }

        public static implicit operator JObject(string value)
        {
            return (JString)value;
        }
    }
}
