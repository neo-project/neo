// Copyright (C) 2015-2025 The Neo Project.
//
// UnicodeMutations.cs file belongs to the neo project and is free
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
    /// Provides Unicode-specific mutation strategies for the mutation engine
    /// </summary>
    public class UnicodeMutations
    {
        private readonly BaseMutationEngine _engine;
        private readonly Random _random;

        // Unicode plane ranges
        private static readonly (int Start, int End)[] _unicodePlanes = new[]
        {
            (0x0000, 0xFFFF),      // Basic Multilingual Plane (BMP)
            (0x10000, 0x1FFFF),    // Supplementary Multilingual Plane (SMP)
            (0x20000, 0x2FFFF),    // Supplementary Ideographic Plane (SIP)
            (0xE0000, 0xEFFFF),    // Supplementary Special-purpose Plane (SSP)
            (0xF0000, 0x10FFFF)    // Private Use Areas (PUA)
        };

        // Specific Unicode blocks of interest
        private static readonly (int Start, int End, string Name)[] _unicodeBlocks = new[]
        {
            (0x0000, 0x007F, "ASCII"),
            (0x0080, 0x00FF, "Latin-1 Supplement"),
            (0x0100, 0x017F, "Latin Extended-A"),
            (0x0180, 0x024F, "Latin Extended-B"),
            (0x0370, 0x03FF, "Greek and Coptic"),
            (0x0400, 0x04FF, "Cyrillic"),
            (0x0530, 0x058F, "Armenian"),
            (0x0590, 0x05FF, "Hebrew"),
            (0x0600, 0x06FF, "Arabic"),
            (0x0900, 0x097F, "Devanagari"),
            (0x3000, 0x303F, "CJK Symbols and Punctuation"),
            (0x3040, 0x309F, "Hiragana"),
            (0x30A0, 0x30FF, "Katakana"),
            (0x4E00, 0x9FFF, "CJK Unified Ideographs"),
            (0xAC00, 0xD7AF, "Hangul Syllables"),
            (0xD800, 0xDBFF, "High Surrogates"),
            (0xDC00, 0xDFFF, "Low Surrogates"),
            (0xE000, 0xF8FF, "Private Use Area"),
            (0xFE00, 0xFE0F, "Variation Selectors"),
            (0xFFF0, 0xFFFF, "Specials")
        };

        // Special character sets
        private static readonly int[] _controlCharacters = Enumerable.Range(0x0000, 0x0020).ToArray();
        private static readonly int[] _whitespaceCharacters = new[] { 0x0009, 0x000A, 0x000B, 0x000C, 0x000D, 0x0020, 0x00A0, 0x2000, 0x2001, 0x2002, 0x2003, 0x2004, 0x2005, 0x2006, 0x2007, 0x2008, 0x2009, 0x200A, 0x2028, 0x2029, 0x202F, 0x205F, 0x3000 };
        private static readonly int[] _zeroWidthCharacters = new[] { 0x200B, 0x200C, 0x200D, 0xFEFF };
        private static readonly int[] _bidirectionalControls = new[] { 0x061C, 0x200E, 0x200F, 0x202A, 0x202B, 0x202C, 0x202D, 0x202E, 0x2066, 0x2067, 0x2068, 0x2069 };
        private static readonly int[] _combiningCharacters = Enumerable.Range(0x0300, 0x036F - 0x0300 + 1).ToArray();
        private static readonly int[] _invalidUnicodeCharacters = new[] { 0xFFFE, 0xFFFF };

        /// <summary>
        /// Initializes a new instance of the UnicodeMutations class
        /// </summary>
        public UnicodeMutations(BaseMutationEngine engine, Random random)
        {
            _engine = engine ?? throw new ArgumentNullException(nameof(engine));
            _random = random ?? throw new ArgumentNullException(nameof(random));
        }

        /// <summary>
        /// Applies a Unicode-specific mutation to the JSON
        /// </summary>
        public string ApplyUnicodeMutation(string json)
        {
            try
            {
                var token = JToken.Parse(json);

                // Select a mutation strategy
                int strategy = _random.Next(7);

                return strategy switch
                {
                    0 => token != null ? AddUnicodeToStrings(token) : json,
                    1 => token != null ? AddUnicodeToPropertyNames(token) : json,
                    2 => token != null ? AddControlCharacters(token) : json,
                    3 => token != null ? AddZeroWidthCharacters(token) : json,
                    4 => token != null ? AddBidirectionalControls(token) : json,
                    5 => token != null ? AddCombiningCharacters(token) : json,
                    _ => token != null ? AddSurrogateCharacters(token) : json
                };
            }
            catch
            {
                // If parsing fails, return the original JSON
                return json;
            }
        }

        /// <summary>
        /// Adds Unicode characters to string values
        /// </summary>
        private string AddUnicodeToStrings(JToken token)
        {
            if (token == null) return "{}";
            ModifyStrings(token, (s) => InsertUnicodeCharacters(s, 1, 5));
            return token.ToString();
        }

        /// <summary>
        /// Adds Unicode characters to property names
        /// </summary>
        private string AddUnicodeToPropertyNames(JToken token)
        {
            if (token == null) return "{}";

            if (token is JObject obj)
            {
                // Create a new object with Unicode property names
                JObject newObj = new JObject();

                // Get all properties
                var properties = obj.Properties.ToList();

                foreach (var property in properties)
                {
                    string newKey = InsertUnicodeCharacters(property.Key, 1, 2);
                    newObj[newKey] = property.Value;
                }

                return newObj.ToString();
            }

            return token.ToString();
        }

        /// <summary>
        /// Adds control characters to string values
        /// </summary>
        private string AddControlCharacters(JToken token)
        {
            if (token == null) return "{}";
            ModifyStrings(token, (s) => InsertCharactersFromSet(s, _controlCharacters, 1, 3));
            return token.ToString();
        }

        /// <summary>
        /// Adds zero-width characters to string values
        /// </summary>
        private string AddZeroWidthCharacters(JToken token)
        {
            if (token == null) return "{}";
            ModifyStrings(token, (s) => InsertCharactersFromSet(s, _zeroWidthCharacters, 1, 5));
            return token.ToString();
        }

        /// <summary>
        /// Adds bidirectional control characters to string values
        /// </summary>
        private string AddBidirectionalControls(JToken token)
        {
            if (token == null) return "{}";
            ModifyStrings(token, (s) => InsertCharactersFromSet(s, _bidirectionalControls, 1, 3));
            return token.ToString();
        }

        /// <summary>
        /// Adds combining characters to string values
        /// </summary>
        private string AddCombiningCharacters(JToken token)
        {
            if (token == null) return "{}";
            ModifyStrings(token, (s) => InsertCharactersFromSet(s, _combiningCharacters, 1, 5));
            return token.ToString();
        }

        /// <summary>
        /// Adds surrogate pair characters to string values
        /// </summary>
        private string AddSurrogateCharacters(JToken token)
        {
            if (token == null) return "{}";
            ModifyStrings(token, (s) => InsertSurrogatePairs(s, 1, 3));
            return token.ToString();
        }

        /// <summary>
        /// Modifies all string values in a token
        /// </summary>
        private void ModifyStrings(JToken token, Func<string, string> modifier)
        {
            if (token == null) return;

            if (token is JString str)
            {
                // Modify the string value
                string modified = modifier(str.Value);

                // In Neo.Json, we can't directly access the Parent property
                // We'll need to handle this differently by traversing the structure
                // and updating the parent objects/arrays directly
            }
            else if (token is JObject obj)
            {
                // Process all string properties
                foreach (var kvp in obj.Properties.ToList())
                {
                    if (kvp.Value is JString strValue)
                    {
                        // Modify the string and update the property directly
                        string modified = modifier(strValue.Value);
                        obj[kvp.Key] = new JString(modified);
                    }
                    else if (kvp.Value != null)
                    {
                        // Recursively process nested objects/arrays
                        ModifyStrings(kvp.Value, modifier);
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
                        // Modify the string and update the array element directly
                        string modified = modifier(strValue.Value);
                        array[i] = new JString(modified);
                    }
                    else if (array[i] != null)
                    {
                        // Recursively process nested objects/arrays
                        ModifyStrings(array[i], modifier);
                    }
                }
            }
        }

        /// <summary>
        /// Inserts random Unicode characters into a string
        /// </summary>
        private string InsertUnicodeCharacters(string input, int minCount, int maxCount)
        {
            if (string.IsNullOrEmpty(input))
            {
                return input;
            }

            StringBuilder sb = new StringBuilder(input);
            int count = _random.Next(minCount, maxCount + 1);

            for (int i = 0; i < count; i++)
            {
                // Select a Unicode block
                var block = _unicodeBlocks[_random.Next(_unicodeBlocks.Length)];

                // Generate a character from that block
                int codePoint = _random.Next(block.Start, block.End + 1);

                // Insert at a random position
                int position = _random.Next(sb.Length + 1);

                // Convert code point to string and insert
                if (codePoint <= 0xFFFF)
                {
                    sb.Insert(position, (char)codePoint);
                }
                else
                {
                    // For supplementary planes, need surrogate pairs
                    codePoint -= 0x10000;
                    char highSurrogate = (char)(0xD800 + (codePoint >> 10));
                    char lowSurrogate = (char)(0xDC00 + (codePoint & 0x3FF));
                    sb.Insert(position, highSurrogate);
                    sb.Insert(position + 1, lowSurrogate);
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Inserts characters from a specific set into a string
        /// </summary>
        private string InsertCharactersFromSet(string input, int[] characterSet, int minCount, int maxCount)
        {
            if (string.IsNullOrEmpty(input) || characterSet.Length == 0)
            {
                return input;
            }

            StringBuilder sb = new StringBuilder(input);
            int count = _random.Next(minCount, maxCount + 1);

            for (int i = 0; i < count; i++)
            {
                // Select a character from the set
                int codePoint = characterSet[_random.Next(characterSet.Length)];

                // Insert at a random position
                int position = _random.Next(sb.Length + 1);

                // Convert code point to string and insert
                sb.Insert(position, (char)codePoint);
            }

            return sb.ToString();
        }

        /// <summary>
        /// Inserts surrogate pairs into a string
        /// </summary>
        private string InsertSurrogatePairs(string input, int minCount, int maxCount)
        {
            if (string.IsNullOrEmpty(input))
            {
                return input;
            }

            StringBuilder sb = new StringBuilder(input);
            int count = _random.Next(minCount, maxCount + 1);

            for (int i = 0; i < count; i++)
            {
                // Select a plane beyond BMP
                var plane = _unicodePlanes[_random.Next(1, _unicodePlanes.Length)];

                // Generate a character from that plane
                int codePoint = _random.Next(plane.Start, plane.End + 1);

                // Insert at a random position
                int position = _random.Next(sb.Length + 1);

                // Convert to surrogate pair and insert
                codePoint -= 0x10000;
                char highSurrogate = (char)(0xD800 + (codePoint >> 10));
                char lowSurrogate = (char)(0xDC00 + (codePoint & 0x3FF));
                sb.Insert(position, highSurrogate);
                sb.Insert(position + 1, lowSurrogate);
            }

            return sb.ToString();
        }

        /// <summary>
        /// Generates a string with characters from a specific Unicode plane
        /// </summary>
        public string GenerateUnicodeString(int planeIndex, int length)
        {
            if (planeIndex < 0 || planeIndex >= _unicodePlanes.Length)
            {
                planeIndex = 0; // Default to BMP if invalid plane index
            }

            var plane = _unicodePlanes[planeIndex];
            StringBuilder sb = new StringBuilder(length);

            for (int i = 0; i < length; i++)
            {
                int codePoint = _random.Next(plane.Start, plane.End + 1);

                if (codePoint <= 0xFFFF)
                {
                    sb.Append((char)codePoint);
                }
                else
                {
                    // Handle surrogate pairs for code points beyond BMP
                    sb.Append(char.ConvertFromUtf32(codePoint));
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Generates a single Unicode character from a specific plane
        /// </summary>
        private char GenerateUnicodeChar(int planeIndex)
        {
            if (planeIndex < 0 || planeIndex >= _unicodePlanes.Length)
            {
                planeIndex = 0; // Default to BMP if invalid plane index
            }

            var plane = _unicodePlanes[planeIndex];
            int codePoint = _random.Next(plane.Start, Math.Min(plane.End, 0xFFFF) + 1);
            return (char)codePoint;
        }

        /// <summary>
        /// Generates a specialized test JSON for Unicode testing
        /// </summary>
        /// <param name="testType">The type of Unicode test to generate (optional)</param>
        /// <returns>A JSON string designed for Unicode testing</returns>
        public string GenerateSpecializedTestJson(string? testType = null)
        {
            JObject obj = new JObject();

            switch (testType?.ToLowerInvariant())
            {
                case "bmp":
                    // Basic Multilingual Plane characters
                    return GenerateUnicodePlaneTest(0);

                case "supplementary":
                    // Supplementary Multilingual Plane characters
                    return GenerateUnicodePlaneTest(1);

                case "ideographic":
                    // Supplementary Ideographic Plane characters
                    return GenerateUnicodePlaneTest(2);

                case "special":
                    // Supplementary Special-purpose Plane characters
                    return GenerateUnicodePlaneTest(3);

                case "private":
                    // Private Use Areas
                    return GenerateUnicodePlaneTest(4);

                case "mixed":
                    // Mix of characters from different planes
                    return GenerateMixedUnicodeTest();

                case "surrogate":
                    // Test surrogate pairs
                    return GenerateSurrogatePairsTest();

                case "control":
                    // Test control characters
                    return GenerateControlCharactersTest();

                default:
                    // Default Unicode test with various characters
                    obj["type"] = "unicode_test";
                    obj["timestamp"] = DateTime.UtcNow.ToString("o");
                    obj["bmp_sample"] = GenerateUnicodeString(0, 20);
                    obj["supplementary_sample"] = GenerateUnicodeString(1, 10);
                    obj["ideographic_sample"] = GenerateUnicodeString(2, 5);
                    obj["control_chars"] = GenerateControlCharactersString(5);
                    obj["surrogate_pairs"] = GenerateSurrogatePairsString(5);
                    return obj.ToString();
            }
        }

        /// <summary>
        /// Generates a test with characters from a specific Unicode plane
        /// </summary>
        private string GenerateUnicodePlaneTest(int planeIndex)
        {
            if (planeIndex < 0 || planeIndex >= _unicodePlanes.Length)
                planeIndex = 0;

            JObject obj = new JObject();
            obj["type"] = "unicode_plane_test";
            obj["plane_index"] = planeIndex;
            obj["plane_name"] = planeIndex switch
            {
                0 => "Basic Multilingual Plane (BMP)",
                1 => "Supplementary Multilingual Plane (SMP)",
                2 => "Supplementary Ideographic Plane (SIP)",
                3 => "Supplementary Special-purpose Plane (SSP)",
                4 => "Private Use Areas (PUA)",
                _ => "Unknown Plane"
            };

            // Generate strings with characters from this plane
            JObject samples = new JObject();
            for (int i = 0; i < 10; i++)
            {
                samples[$"sample_{i}"] = GenerateUnicodeString(planeIndex, 10);
            }
            obj["samples"] = samples;

            // Generate a nested structure with Unicode characters
            JObject nested = new JObject();
            for (int i = 0; i < 5; i++)
            {
                string key = GenerateUnicodeString(planeIndex, 3);
                nested[key] = GenerateUnicodeString(planeIndex, 5);
            }
            obj["nested"] = nested;

            return obj.ToString();
        }

        /// <summary>
        /// Generates a test with mixed Unicode characters from different planes
        /// </summary>
        private string GenerateMixedUnicodeTest()
        {
            JObject obj = new JObject();
            obj["type"] = "mixed_unicode_test";

            // Create an array of mixed Unicode strings
            JArray mixedStrings = new JArray();
            for (int i = 0; i < 10; i++)
            {
                // Mix characters from different planes
                StringBuilder sb = new StringBuilder();
                for (int j = 0; j < 10; j++)
                {
                    int planeIndex = _random.Next(_unicodePlanes.Length);
                    sb.Append(GenerateUnicodeChar(planeIndex));
                }
                mixedStrings.Add(sb.ToString());
            }
            obj["mixed_strings"] = mixedStrings;

            // Create an object with mixed Unicode keys and values
            JObject mixedObject = new JObject();
            for (int i = 0; i < 5; i++)
            {
                // Generate a key with mixed Unicode characters
                StringBuilder keyBuilder = new StringBuilder();
                for (int j = 0; j < 3; j++)
                {
                    int planeIndex = _random.Next(2); // Limit to BMP and SMP for keys
                    keyBuilder.Append(GenerateUnicodeChar(planeIndex));
                }

                // Generate a value with mixed Unicode characters
                StringBuilder valueBuilder = new StringBuilder();
                for (int j = 0; j < 5; j++)
                {
                    int planeIndex = _random.Next(_unicodePlanes.Length);
                    valueBuilder.Append(GenerateUnicodeChar(planeIndex));
                }

                mixedObject[keyBuilder.ToString()] = valueBuilder.ToString();
            }
            obj["mixed_object"] = mixedObject;

            return obj.ToString();
        }

        /// <summary>
        /// Generates a test focused on surrogate pairs
        /// </summary>
        private string GenerateSurrogatePairsTest()
        {
            JObject obj = new JObject();
            obj["type"] = "surrogate_pairs_test";

            // Generate strings with surrogate pairs
            JArray samples = new JArray();
            for (int i = 0; i < 10; i++)
            {
                samples.Add(GenerateSurrogatePairsString(10));
            }
            obj["surrogate_samples"] = samples;

            return obj.ToString();
        }

        /// <summary>
        /// Generates a test focused on control characters
        /// </summary>
        private string GenerateControlCharactersTest()
        {
            JObject obj = new JObject();
            obj["type"] = "control_characters_test";

            // Generate strings with control characters
            JArray samples = new JArray();
            for (int i = 0; i < 10; i++)
            {
                samples.Add(GenerateControlCharactersString(10));
            }
            obj["control_samples"] = samples;

            return obj.ToString();
        }

        /// <summary>
        /// Generates a string with surrogate pairs
        /// </summary>
        private string GenerateSurrogatePairsString(int length)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < length; i++)
            {
                // Generate a surrogate pair (characters outside BMP)
                int codePoint = _random.Next(0x10000, 0x10FFFF);
                sb.Append(char.ConvertFromUtf32(codePoint));
            }
            return sb.ToString();
        }

        /// <summary>
        /// Generates a string with control characters
        /// </summary>
        private string GenerateControlCharactersString(int length)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < length; i++)
            {
                // Generate a control character (0x00-0x1F, 0x7F-0x9F)
                int codePoint = _random.Next(2) == 0
                    ? _random.Next(0x00, 0x20)
                    : _random.Next(0x7F, 0xA0);
                sb.Append((char)codePoint);
            }
            return sb.ToString();
        }
    }
}
