// Copyright (C) 2015-2025 The Neo Project.
//
// StringMutations.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Neo.Json.Fuzzer.Generators
{
    /// <summary>
    /// Provides string-related mutation strategies for the mutation engine
    /// </summary>
    public class StringMutations
    {
        private readonly BaseMutationEngine _engine;
        private readonly Random _random;

        /// <summary>
        /// Initializes a new instance of the StringMutations class
        /// </summary>
        public StringMutations(BaseMutationEngine engine, Random random)
        {
            _engine = engine ?? throw new ArgumentNullException(nameof(engine));
            _random = random ?? throw new ArgumentNullException(nameof(random));
        }

        /// <summary>
        /// Modifies a string value in the JSON
        /// </summary>
        public string ModifyStringValue(string json)
        {
            try
            {
                var token = JToken.Parse(json);
                if (token != null)
                {
                    ModifyStringValuesRecursive(token);
                    return token.ToString();
                }
                return json;
            }
            catch
            {
                // If parsing fails, return the original JSON
                return json;
            }
        }

        /// <summary>
        /// Modifies string values in a JToken recursively
        /// </summary>
        private void ModifyStringValuesRecursive(JToken? token)
        {
            if (token == null) return;

            if (token is JString str)
            {
                // String values are handled by the parent object/array
            }
            else if (token is JObject obj)
            {
                // Process all properties
                foreach (var property in obj.Properties)
                {
                    if (property.Value is JString strValue)
                    {
                        // Modify the string value
                        obj[property.Key] = new JString(ModifyStringValue(strValue.Value));
                    }
                    else if (property.Value != null)
                    {
                        // Recursively process nested objects/arrays
                        ModifyStringValuesRecursive(property.Value);
                    }
                }
            }
            else if (token is JArray array)
            {
                // Process all array elements
                for (int i = 0; i < array.Count; i++)
                {
                    if (array[i] is JString strValue)
                    {
                        // Modify the string value
                        array[i] = new JString(ModifyStringValue(strValue.Value));
                    }
                    else if (array[i] != null)
                    {
                        // Recursively process nested objects/arrays
                        ModifyStringValuesRecursive(array[i]);
                    }
                }
            }
        }

        /// <summary>
        /// Generates a random string with controlled length
        /// </summary>
        public string GenerateRandomString(int maxLength = 100)
        {
            // Ensure the length is within reasonable limits
            int length = Math.Min(maxLength, BaseMutationEngine.MAX_STRING_LENGTH);

            // Select a string generation strategy
            int strategy = _random.Next(5);

            return strategy switch
            {
                0 => GenerateAlphanumericString(length),
                1 => GenerateUnicodeString(length),
                2 => GenerateSpecialCharacterString(length),
                3 => GenerateJsonEscapeSequenceString(length),
                _ => GenerateMixedString(length)
            };
        }

        /// <summary>
        /// Generates a random string with controlled minimum and maximum length
        /// </summary>
        public string GenerateRandomString(int minLength, int maxLength)
        {
            // Ensure the lengths are within reasonable limits
            minLength = Math.Min(minLength, BaseMutationEngine.MAX_STRING_LENGTH);
            maxLength = Math.Min(maxLength, BaseMutationEngine.MAX_STRING_LENGTH);

            // Ensure minLength is not greater than maxLength
            if (minLength > maxLength)
            {
                (minLength, maxLength) = (maxLength, minLength);
            }

            // Generate a random length between min and max
            int length = _random.Next(minLength, maxLength + 1);

            // Select a string generation strategy
            int strategy = _random.Next(5);

            return strategy switch
            {
                0 => GenerateAlphanumericString(length),
                1 => GenerateUnicodeString(length),
                2 => GenerateSpecialCharacterString(length),
                3 => GenerateJsonEscapeSequenceString(length),
                _ => GenerateMixedString(length)
            };
        }

        /// <summary>
        /// Generates a random alphanumeric string
        /// </summary>
        public string GenerateAlphanumericString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            StringBuilder sb = new StringBuilder(length);

            for (int i = 0; i < length; i++)
            {
                sb.Append(chars[_random.Next(chars.Length)]);
            }

            return sb.ToString();
        }

        /// <summary>
        /// Generates a string with Unicode characters
        /// </summary>
        public string GenerateUnicodeString(int length)
        {
            StringBuilder sb = new StringBuilder(length);

            for (int i = 0; i < length; i++)
            {
                // Generate a random Unicode character (avoiding control characters and surrogates)
                int codePoint;
                do
                {
                    codePoint = _random.Next(0x20, 0xD800);
                } while (codePoint >= 0xD800 && codePoint <= 0xDFFF);

                sb.Append(char.ConvertFromUtf32(codePoint));
            }

            return sb.ToString();
        }

        /// <summary>
        /// Generates a string with special characters
        /// </summary>
        public string GenerateSpecialCharacterString(int length)
        {
            const string specialChars = "!@#$%^&*()_+-=[]{}|;:,.<>?/~`";
            StringBuilder sb = new StringBuilder(length);

            for (int i = 0; i < length; i++)
            {
                sb.Append(specialChars[_random.Next(specialChars.Length)]);
            }

            return sb.ToString();
        }

        /// <summary>
        /// Generates a string with JSON escape sequences
        /// </summary>
        public string GenerateJsonEscapeSequenceString(int length)
        {
            // Ensure we have at least one escape sequence
            int escapeCount = Math.Max(1, length / 5);

            // Generate a base string
            string baseString = GenerateAlphanumericString(length - escapeCount);

            // Insert escape sequences
            StringBuilder sb = new StringBuilder(baseString);
            string[] escapeSequences = new[] { "\\\"", "\\\\", "\\/", "\\b", "\\f", "\\n", "\\r", "\\t", "\\u0000", "\\u001F", "\\u007F" };

            for (int i = 0; i < escapeCount; i++)
            {
                int position = _random.Next(sb.Length + 1);
                string escapeSequence = escapeSequences[_random.Next(escapeSequences.Length)];

                // If it's a Unicode escape, generate a random 4-digit hex value
                if (escapeSequence.StartsWith("\\u"))
                {
                    escapeSequence = $"\\u{_random.Next(0x10000):X4}";
                }

                sb.Insert(position, escapeSequence);
            }

            return sb.ToString();
        }

        /// <summary>
        /// Generates a mixed string with various character types
        /// </summary>
        public string GenerateMixedString(int length)
        {
            // Divide the length among different string types
            int alphaLength = length / 3;
            int unicodeLength = length / 3;
            int specialLength = length - alphaLength - unicodeLength;

            // Generate each part
            string alphaPart = GenerateAlphanumericString(alphaLength);
            string unicodePart = GenerateUnicodeString(unicodeLength);
            string specialPart = GenerateSpecialCharacterString(specialLength);

            // Combine and shuffle
            string combined = alphaPart + unicodePart + specialPart;
            return ShuffleString(combined);
        }

        /// <summary>
        /// Shuffles the characters in a string
        /// </summary>
        private string ShuffleString(string input)
        {
            char[] array = input.ToCharArray();
            int n = array.Length;

            while (n > 1)
            {
                n--;
                int k = _random.Next(n + 1);
                (array[k], array[n]) = (array[n], array[k]);
            }

            return new string(array);
        }

        /// <summary>
        /// Adds a random string to the JSON
        /// </summary>
        public string AddRandomString(string json)
        {
            try
            {
                var token = JToken.Parse(json);
                if (token != null)
                {
                    AddRandomStringToToken(token);
                    return token.ToString();
                }
                return json;
            }
            catch
            {
                // If parsing fails, return the original JSON
                return json;
            }
        }

        /// <summary>
        /// Adds a random string to a JToken
        /// </summary>
        private void AddRandomStringToToken(JToken token)
        {
            if (token == null)
                return;

            if (token is JObject obj)
            {
                // Add a new string property
                string propertyName = "string_" + _random.Next(1000);
                obj[propertyName] = new JString(GenerateRandomString(50));
            }
            else if (token is JArray array && array.Count > 0)
            {
                // Add a string to a random position in the array
                int position = _random.Next(array.Count + 1);
                array.Insert(position, new JString(GenerateRandomString(50)));
            }
        }

        /// <summary>
        /// Replaces a value with a random string
        /// </summary>
        public string ReplaceWithRandomString(string json)
        {
            try
            {
                var token = JToken.Parse(json);
                if (token != null)
                {
                    ReplaceRandomValueWithString(token);
                    return token.ToString();
                }
                return json;
            }
            catch
            {
                // If parsing fails, return the original JSON
                return json;
            }
        }

        /// <summary>
        /// Replaces a random value with a string
        /// </summary>
        private void ReplaceRandomValueWithString(JToken? token)
        {
            if (token == null) return;

            if (token is JObject obj)
            {
                // Get all properties
                var properties = obj.Properties.ToList();
                if (properties.Count > 0)
                {
                    // Select a random property
                    int index = _random.Next(properties.Count);
                    var property = properties[index];

                    // Replace the value with a random string
                    obj[property.Key] = new JString(GenerateRandomString());
                }
            }
            else if (token is JArray array)
            {
                if (array.Count > 0)
                {
                    // Select a random index
                    int index = _random.Next(array.Count);

                    // Replace the value with a random string
                    array[index] = new JString(GenerateRandomString());
                }
                else
                {
                    // If array is empty, add a random string
                    array.Add(new JString(GenerateRandomString()));
                }
            }
        }

        /// <summary>
        /// Generates a specialized test JSON for string testing
        /// </summary>
        public string GenerateSpecializedTestJson(string? testType = null)
        {
            JObject obj = new JObject();

            switch (testType?.ToLowerInvariant())
            {
                case "unicode":
                    // Add various Unicode strings
                    obj["unicode_basic"] = new JString(GenerateUnicodeString(50));
                    obj["unicode_mixed"] = new JString(GenerateMixedString(50));
                    obj["unicode_surrogate_pairs"] = new JString("\uD83D\uDE00\uD83D\uDE01\uD83D\uDE02\uD83D\uDE03"); // Emoji
                    break;

                case "escape":
                    // Add strings with escape sequences
                    obj["escape_sequences"] = new JString(GenerateJsonEscapeSequenceString(50));
                    obj["escape_quotes"] = new JString("String with \"quotes\" that need escaping");
                    obj["escape_backslash"] = new JString("String with \\ backslashes");
                    obj["escape_control"] = new JString("String with \b\f\n\r\t control characters");
                    break;

                case "long":
                    // Add very long strings
                    obj["long_alpha"] = new JString(GenerateAlphanumericString(1000));
                    obj["long_mixed"] = new JString(GenerateMixedString(1000));
                    obj["long_special"] = new JString(GenerateSpecialCharacterString(1000));
                    break;

                case "empty":
                    // Add empty and whitespace strings
                    obj["empty"] = new JString("");
                    obj["whitespace"] = new JString("   \t   \n   ");
                    obj["zero_width"] = new JString("\u200B\u200C\u200D"); // Zero-width characters
                    break;

                default:
                    // Add a mix of string types
                    obj["alphanumeric"] = new JString(GenerateAlphanumericString(20));
                    obj["unicode"] = new JString(GenerateUnicodeString(20));
                    obj["special"] = new JString(GenerateSpecialCharacterString(20));
                    obj["escape"] = new JString(GenerateJsonEscapeSequenceString(20));
                    obj["mixed"] = new JString(GenerateMixedString(20));
                    break;
            }

            return obj.ToString();
        }
    }
}
