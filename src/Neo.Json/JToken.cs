// Copyright (C) 2015-2022 The Neo Project.
// 
// The Neo.Json is free software distributed under the MIT software license, 
// see the accompanying file LICENSE in the main directory of the
// project or http://www.opensource.org/licenses/mit-license.php 
// for more details.
// 
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System.Numerics;
using System.Text.Json;
using static Neo.Json.Utility;

namespace Neo.Json;

/// <summary>
/// Represents an abstract JSON token.
/// </summary>
public abstract class JToken
{
    /// <summary>
    /// Represents a <see langword="null"/> token.
    /// </summary>
    public const JToken? Null = null;

    /// <summary>
    /// Gets or sets the child token at the specified index.
    /// </summary>
    /// <param name="index">The zero-based index of the child token to get or set.</param>
    /// <returns>The child token at the specified index.</returns>
    public virtual JToken? this[int index]
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    /// <summary>
    /// Gets or sets the properties of the JSON object.
    /// </summary>
    /// <param name="key">The key of the property to get or set.</param>
    /// <returns>The property with the specified name.</returns>
    public virtual JToken? this[string key]
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    /// <summary>
    /// Converts the current JSON token to a boolean value.
    /// </summary>
    /// <returns>The converted value.</returns>
    public virtual bool AsBoolean()
    {
        return true;
    }

    /// <summary>
    /// Converts the current JSON token to an <see cref="Enum"/>.
    /// </summary>
    /// <typeparam name="T">The type of the <see cref="Enum"/>.</typeparam>
    /// <param name="defaultValue">If the current JSON token cannot be converted to type <typeparamref name="T"/>, then the default value is returned.</param>
    /// <param name="ignoreCase">Indicates whether case should be ignored during conversion.</param>
    /// <returns>The converted value.</returns>
    public virtual T AsEnum<T>(T defaultValue = default, bool ignoreCase = false) where T : unmanaged, Enum
    {
        return defaultValue;
    }

    /// <summary>
    /// Converts the current JSON token to a floating point number.
    /// </summary>
    /// <returns>The converted value.</returns>
    public virtual double AsNumber()
    {
        return double.NaN;
    }

    /// <summary>
    /// Converts the current JSON token to a <see cref="string"/>.
    /// </summary>
    /// <returns>The converted value.</returns>
    public virtual string AsString()
    {
        return ToString();
    }

    /// <summary>
    /// Converts the current JSON token to a boolean value.
    /// </summary>
    /// <returns>The converted value.</returns>
    /// <exception cref="InvalidCastException">The JSON token is not a <see cref="JBoolean"/>.</exception>
    public virtual bool GetBoolean() => throw new InvalidCastException();

    public virtual T GetEnum<T>(bool ignoreCase = false) where T : unmanaged, Enum => throw new InvalidCastException();

    /// <summary>
    /// Converts the current JSON token to a 32-bit signed integer.
    /// </summary>
    /// <returns>The converted value.</returns>
    /// <exception cref="InvalidCastException">The JSON token is not a <see cref="JNumber"/>.</exception>
    /// <exception cref="InvalidCastException">The JSON token cannot be converted to an integer.</exception>
    /// <exception cref="OverflowException">The JSON token cannot be converted to a 32-bit signed integer.</exception>
    public int GetInt32()
    {
        double d = GetNumber();
        if (d % 1 != 0) throw new InvalidCastException();
        return checked((int)d);
    }

    /// <summary>
    /// Converts the current JSON token to a floating point number.
    /// </summary>
    /// <returns>The converted value.</returns>
    /// <exception cref="InvalidCastException">The JSON token is not a <see cref="JNumber"/>.</exception>
    public virtual double GetNumber() => throw new InvalidCastException();

    /// <summary>
    /// Converts the current JSON token to a <see cref="string"/>.
    /// </summary>
    /// <returns>The converted value.</returns>
    /// <exception cref="InvalidCastException">The JSON token is not a <see cref="JString"/>.</exception>
    public virtual string GetString() => throw new InvalidCastException();

    /// <summary>
    /// Parses a JSON token from a byte array.
    /// </summary>
    /// <param name="value">The byte array that contains the JSON token.</param>
    /// <param name="max_nest">The maximum nesting depth when parsing the JSON token.</param>
    /// <returns>The parsed JSON token.</returns>
    public static JToken? Parse(ReadOnlySpan<byte> value, int max_nest = 64)
    {
        Utf8JsonReader reader = new(value, new JsonReaderOptions
        {
            AllowTrailingCommas = false,
            CommentHandling = JsonCommentHandling.Skip,
            MaxDepth = max_nest
        });
        try
        {
            JToken? json = Read(ref reader);
            if (reader.Read()) throw new FormatException();
            return json;
        }
        catch (JsonException ex)
        {
            throw new FormatException(ex.Message, ex);
        }
    }

    /// <summary>
    /// Parses a JSON token from a <see cref="string"/>.
    /// </summary>
    /// <param name="value">The <see cref="string"/> that contains the JSON token.</param>
    /// <param name="max_nest">The maximum nesting depth when parsing the JSON token.</param>
    /// <returns>The parsed JSON token.</returns>
    public static JToken? Parse(string value, int max_nest = 64)
    {
        return Parse(StrictUTF8.GetBytes(value), max_nest);
    }

    private static JToken? Read(ref Utf8JsonReader reader, bool skipReading = false)
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
                    JToken? value = Read(ref reader);
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
            return reader.GetString()!;
        }
        catch (InvalidOperationException ex)
        {
            throw new FormatException(ex.Message, ex);
        }
    }

    /// <summary>
    /// Encode the current JSON token into a byte array.
    /// </summary>
    /// <param name="indented">Indicates whether indentation is required.</param>
    /// <returns>The encoded JSON token.</returns>
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
    /// Encode the current JSON token into a <see cref="string"/>.
    /// </summary>
    /// <returns>The encoded JSON token.</returns>
    public override string ToString()
    {
        return ToString(false);
    }

    /// <summary>
    /// Encode the current JSON token into a <see cref="string"/>.
    /// </summary>
    /// <param name="indented">Indicates whether indentation is required.</param>
    /// <returns>The encoded JSON token.</returns>
    public string ToString(bool indented)
    {
        return StrictUTF8.GetString(ToByteArray(indented));
    }

    internal abstract void Write(Utf8JsonWriter writer);

    public abstract JToken Clone();

    public JArray JsonPath(string expr)
    {
        JToken?[] objects = { this };
        if (expr.Length == 0) return objects;
        Queue<JPathToken> tokens = new(JPathToken.Parse(expr));
        JPathToken first = tokens.Dequeue();
        if (first.Type != JPathTokenType.Root) throw new FormatException();
        JPathToken.ProcessJsonPath(ref objects, tokens);
        return objects;
    }

    public static implicit operator JToken(Enum value)
    {
        return (JString)value;
    }

    public static implicit operator JToken(JToken?[] value)
    {
        return (JArray)value;
    }

    public static implicit operator JToken(bool value)
    {
        return (JBoolean)value;
    }

    public static implicit operator JToken(double value)
    {
        return (JNumber)value;
    }

    public static implicit operator JToken(BigInteger value)
    {
        return (JNumber)value;
    }

    public static implicit operator JToken?(string? value)
    {
        return (JString?)value;
    }
}
