// Copyright (C) 2015-2026 The Neo Project.
//
// JToken.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using System.Text.Json;
using static Neo.Json.Utility;

namespace Neo.Json
{
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
            var d = GetNumber();
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
        /// <param name="exactIntegers">
        /// When <see langword="true"/>, integer JSON numbers (including values outside the IEEE-754
        /// safe range and integer-valued scientific notation) are stored exactly via
        /// <see cref="BigInteger"/>. When <see langword="false"/> (default), numbers use
        /// <see cref="double"/> only — the historical behavior required for consensus before
        /// <c>HF_Huyao</c> enables exact integers in <c>StdLib.jsonDeserialize</c>.
        /// </param>
        /// <returns>The parsed JSON token.</returns>
        public static JToken? Parse(ReadOnlySpan<byte> value, int max_nest = 64, bool exactIntegers = false)
        {
            var reader = new Utf8JsonReader(value, new JsonReaderOptions
            {
                AllowTrailingCommas = false,
                CommentHandling = JsonCommentHandling.Skip,
                MaxDepth = max_nest
            });
            try
            {
                var json = Read(ref reader, exactIntegers: exactIntegers);
                if (reader.Read()) throw new FormatException("Read json token failed");
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
        /// <param name="exactIntegers">See <see cref="Parse(ReadOnlySpan{byte}, int, bool)"/>.</param>
        /// <returns>The parsed JSON token.</returns>
        public static JToken? Parse(string value, int max_nest = 64, bool exactIntegers = false)
        {
            return Parse(StrictUTF8.GetBytes(value), max_nest, exactIntegers);
        }

        private static JToken? Read(ref Utf8JsonReader reader, bool skipReading = false, bool exactIntegers = false)
        {
            if (!skipReading && !reader.Read()) throw new FormatException("Read json token failed");
            return reader.TokenType switch
            {
                JsonTokenType.False => false,
                JsonTokenType.Null => Null,
                JsonTokenType.Number => ReadNumber(ref reader, exactIntegers),
                JsonTokenType.StartArray => ReadArray(ref reader, exactIntegers),
                JsonTokenType.StartObject => ReadObject(ref reader, exactIntegers),
                JsonTokenType.String => ReadString(ref reader),
                JsonTokenType.True => true,
                _ => throw new FormatException($"Unexpected token {reader.TokenType}"),
            };
        }

        /// <summary>
        /// Decimal-digit budget matching NeoVM 32-byte integers (~77 digits for 2^255).
        /// Used to reject oversized integer-valued JSON numbers before <see cref="BigInteger.Pow"/>
        /// or unbounded <see langword="stackalloc"/>.
        /// </summary>
        private const int MaxIntegerDecimalDigits = 78;

        private static JNumber ReadNumber(ref Utf8JsonReader reader, bool exactIntegers)
        {
            // Legacy / pre-HF_Huyao: always double (consensus-compatible with historical nodes).
            if (!exactIntegers)
                return new JNumber(reader.GetDouble());

            // Prefer exact integer tokens so large values (e.g. token amounts) keep full precision.
            // TryGetInt64 only succeeds for non-fractional, non-scientific spellings; other integer
            // forms (trailing .0, 1e3, …) share the same 32-byte path via TryParseExactInteger.
            if (reader.TryGetInt64(out var int64))
            {
                if (int64 >= JNumber.MIN_SAFE_INTEGER && int64 <= JNumber.MAX_SAFE_INTEGER)
                    return new JNumber((double)int64);
                return JNumber.FromBigInteger(int64);
            }

            var raw = GetRawNumberText(ref reader);
            if (TryParseExactInteger(raw, out var integer, out var overflow))
            {
                if (!JNumber.FitsMaxIntegerSize(integer))
                    throw new FormatException($"JSON integer exceeds {JNumber.MaxIntegerSize}-byte limit.");
                return JNumber.FromBigInteger(integer);
            }

            if (overflow)
                throw new FormatException($"JSON integer exceeds {JNumber.MaxIntegerSize}-byte limit.");

            return new JNumber(reader.GetDouble());
        }

        /// <summary>
        /// Parses a JSON number as an exact integer when the mathematical value has no fractional
        /// part, independent of spelling (decimal digits, trailing <c>.0</c>, or scientific form
        /// such as <c>9.05E+28</c>). Returns <see langword="false"/> with <paramref name="overflow"/>
        /// set when the value is an integer that cannot fit in <see cref="MaxIntegerDecimalDigits"/>.
        /// </summary>
        private static bool TryParseExactInteger(ReadOnlySpan<char> raw, out BigInteger result, out bool overflow)
        {
            result = default;
            overflow = false;

            var eIndex = raw.IndexOfAny('e', 'E');
            var mantissa = eIndex >= 0 ? raw[..eIndex] : raw;
            var expSpan = eIndex >= 0 ? raw[(eIndex + 1)..] : ReadOnlySpan<char>.Empty;
            var expNegative = expSpan.Length > 0 && expSpan[0] == '-';

            var negative = mantissa.Length > 0 && mantissa[0] == '-';
            if (negative)
                mantissa = mantissa[1..];

            var dot = mantissa.IndexOf('.');
            var intPart = dot >= 0 ? mantissa[..dot] : mantissa;
            var fracPart = dot >= 0 ? mantissa[(dot + 1)..] : ReadOnlySpan<char>.Empty;

            var firstInt = IndexOfNonZero(intPart);
            var firstFrac = IndexOfNonZero(fracPart);
            if (firstInt < 0 && firstFrac < 0)
            {
                result = BigInteger.Zero;
                return true;
            }

            var lastInt = LastIndexOfNonZero(intPart);
            var lastFrac = LastIndexOfNonZero(fracPart);
            var lastConcat = lastFrac >= 0 ? intPart.Length + lastFrac : lastInt;
            var firstConcat = firstInt >= 0 ? firstInt : intPart.Length + firstFrac;
            var significantDigits = lastConcat - firstConcat + 1;
            var trailingZeros = intPart.Length + fracPart.Length - lastConcat - 1;
            var scaleAdjust = (long)trailingZeros - fracPart.Length;

            var exponent = 0L;
            if (eIndex >= 0)
            {
                if (!long.TryParse(expSpan, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out exponent))
                {
                    if (expNegative)
                        return false;
                    overflow = true;
                    return false;
                }
            }

            // adjExp = scaleAdjust + exponent. Compare without adding values that may overflow long.
            if (exponent < -scaleAdjust)
                return false;

            if (significantDigits > MaxIntegerDecimalDigits)
            {
                overflow = true;
                return false;
            }

            var maxAdj = MaxIntegerDecimalDigits - significantDigits;
            if (exponent > maxAdj - scaleAdjust)
            {
                overflow = true;
                return false;
            }

            var adjExp = (int)(scaleAdjust + exponent);
            Span<char> digits = stackalloc char[MaxIntegerDecimalDigits];
            var n = CopySignificantDigits(intPart, fracPart, firstInt, lastInt, firstFrac, lastFrac, digits);
            if (!BigInteger.TryParse(digits[..n], NumberStyles.None, CultureInfo.InvariantCulture, out var significand))
                return false;

            if (adjExp > 0)
                significand *= BigInteger.Pow(10, adjExp);

            result = negative ? -significand : significand;
            return true;
        }

        private static int IndexOfNonZero(ReadOnlySpan<char> digits)
        {
            for (var i = 0; i < digits.Length; i++)
            {
                if (digits[i] != '0')
                    return i;
            }
            return -1;
        }

        private static int LastIndexOfNonZero(ReadOnlySpan<char> digits)
        {
            for (var i = digits.Length - 1; i >= 0; i--)
            {
                if (digits[i] != '0')
                    return i;
            }
            return -1;
        }

        private static int CopySignificantDigits(
            ReadOnlySpan<char> intPart,
            ReadOnlySpan<char> fracPart,
            int firstInt,
            int lastInt,
            int firstFrac,
            int lastFrac,
            Span<char> destination)
        {
            var n = 0;
            if (firstInt >= 0)
            {
                var intEnd = lastFrac >= 0 ? intPart.Length : lastInt + 1;
                intPart[firstInt..intEnd].CopyTo(destination);
                n = intEnd - firstInt;
            }
            if (lastFrac >= 0)
            {
                var fracStart = firstInt >= 0 ? 0 : firstFrac;
                var fracSlice = fracPart[fracStart..(lastFrac + 1)];
                fracSlice.CopyTo(destination[n..]);
                n += fracSlice.Length;
            }
            return n;
        }

        private static string GetRawNumberText(ref Utf8JsonReader reader)
        {
            if (!reader.HasValueSequence)
                return StrictUTF8.GetString(reader.ValueSpan);

            // Multi-segment number tokens are rare; reassemble without System.Buffers helpers.
            var length = checked((int)reader.ValueSequence.Length);
            var buffer = new byte[length];
            var offset = 0;
            foreach (var segment in reader.ValueSequence)
            {
                segment.Span.CopyTo(buffer.AsSpan(offset));
                offset += segment.Length;
            }
            return StrictUTF8.GetString(buffer);
        }

        private static JArray ReadArray(ref Utf8JsonReader reader, bool exactIntegers)
        {
            var array = new JArray();
            while (reader.Read())
            {
                switch (reader.TokenType)
                {
                    case JsonTokenType.EndArray:
                        return array;
                    default:
                        array.Add(Read(ref reader, skipReading: true, exactIntegers: exactIntegers));
                        break;
                }
            }
            throw new FormatException("Unterminated array");
        }

        private static JObject ReadObject(ref Utf8JsonReader reader, bool exactIntegers)
        {
            JObject obj = new();
            while (reader.Read())
            {
                switch (reader.TokenType)
                {
                    case JsonTokenType.EndObject:
                        return obj;
                    case JsonTokenType.PropertyName:
                        var name = ReadString(ref reader);
                        if (obj.Properties.ContainsKey(name))
                            throw new FormatException($"Duplicate property name: {name}");

                        var value = Read(ref reader, exactIntegers: exactIntegers);
                        obj.Properties.Add(name, value);
                        break;
                    default:
                        throw new FormatException($"Unexpected token {reader.TokenType}");
                }
            }
            throw new FormatException("Unterminated object");
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
            JToken?[] objects = [this];
            if (expr.Length == 0) return objects;

            Queue<JPathToken> tokens = new(JPathToken.Parse(expr));
            var first = tokens.Dequeue();
            if (first.Type != JPathTokenType.Root)
                throw new FormatException($"Unexpected token {first.Type}");

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

        public static implicit operator JToken(long value)
        {
            return (JNumber)value;
        }

        public static implicit operator JToken(BigInteger value)
        {
            return (JNumber)value;
        }

        [return: NotNullIfNotNull(nameof(value))]
        public static implicit operator JToken?(string? value)
        {
            return (JString?)value;
        }
    }
}
