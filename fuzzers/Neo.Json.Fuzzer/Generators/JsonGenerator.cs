// Copyright (C) 2015-2025 The Neo Project.
//
// JsonGenerator.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Neo.Json.Fuzzer.Generators
{
    /// <summary>
    /// Generates random but valid and invalid JSON for fuzzing
    /// </summary>
    public class JsonGenerator
    {
        private readonly Random _random;
        private readonly int _maxDepth;
        private readonly int _maxChildren;
        private readonly int _maxStringLength;

        // Special characters to test string handling
        private static readonly char[] _specialChars = new[]
        {
            '\\', '\"', '/', '\b', '\f', '\n', '\r', '\t',
            '\u0000', '\u0001', '\u001F', '\u007F', '\u0080', '\u00FF',
            '\u0100', '\u2000', '\u3000', '\uD800', '\uDFFF', '\uFFFF'
        };

        // Neo.Json specific limits
        private const int NEO_DEFAULT_MAX_NEST = 64;
        private const int NEO_LOW_MAX_NEST = 10;
        private const int NEO_HIGH_MAX_NEST = 128;

        // JSON structure templates for more targeted generation
        private static readonly string[] _jsonTemplates = new[]
        {
            "{}",
            "[]",
            "{{\"key\": {0}}}",
            "[{0}]",
            "{{\"nested\": {0}}}",
            "{{\"array\": [{0}]}}",
            "{{\"a\": {0}, \"b\": {1}}}",
            "[{0}, {1}]",
            "{{\"deeply\": {{\"nested\": {{\"object\": {0}}}}}}}",
            "[[{0}]]"
        };

        // Neo.Json specific templates for testing JPath functionality
        private static readonly string[] _jPathTestTemplates = new[]
        {
            "{{\"root\": {{\"child\": {{\"grandchild\": {0}}}}}}}",
            "{{\"array\": [{0}, {1}, {2}]}}",
            "{{\"properties\": {{\"a\": {0}, \"b\": {1}, \"c\": {2}}}}}",
            "{{\"mixed\": {{\"array\": [{0}, {1}], \"object\": {{\"key\": {2}}}}}}}",
            "[{{\"id\": 1, \"value\": {0}}}, {{\"id\": 2, \"value\": {1}}}]"
        };

        /// <summary>
        /// Initializes a new instance of the JsonGenerator class
        /// </summary>
        /// <param name="random">Random number generator</param>
        /// <param name="maxDepth">Maximum nesting depth for generated JSON</param>
        /// <param name="maxChildren">Maximum number of children per object or array</param>
        /// <param name="maxStringLength">Maximum length for generated strings</param>
        public JsonGenerator(Random random, int maxDepth = 64, int maxChildren = 10, int maxStringLength = 1000)
        {
            _random = random ?? throw new ArgumentNullException(nameof(random));
            _maxDepth = Math.Max(1, Math.Min(maxDepth, NEO_HIGH_MAX_NEST)); // Ensure reasonable limits
            _maxChildren = Math.Max(1, Math.Min(maxChildren, 100)); // Cap at 100 to avoid excessive size
            _maxStringLength = Math.Max(1, Math.Min(maxStringLength, 10000)); // Cap at 10000 to avoid excessive memory usage
        }

        /// <summary>
        /// Generates a random JSON string
        /// </summary>
        /// <returns>A string containing the generated JSON</returns>
        public string GenerateRandomJson()
        {
            // Choose a generation strategy with weighted probabilities
            int strategy = _random.Next(100);

            // Higher probability for valid JSON to ensure better coverage
            return strategy switch
            {
                < 30 => GenerateSimpleJson(),
                < 60 => GenerateComplexJson(),
                < 75 => GenerateNeoSpecificJson(), // Neo.Json specific test cases
                < 85 => GenerateEdgeCaseJson(),
                < 95 => GenerateNestedJson(),
                _ => GenerateMalformedJson() // Lower probability for malformed JSON
            };
        }

        /// <summary>
        /// Generates a simple JSON value (string, number, boolean, null)
        /// </summary>
        private string GenerateSimpleJson()
        {
            int type = _random.Next(7);

            return type switch
            {
                0 => $"\"{GenerateRandomString()}\"",
                1 => GenerateRandomNumber(),
                2 => "true",
                3 => "false",
                4 => "null",
                5 => "{}",
                _ => "[]"
            };
        }

        /// <summary>
        /// Generates a complex JSON structure with multiple types
        /// </summary>
        private string GenerateComplexJson()
        {
            // Create a JSON object with various property types
            var properties = new List<string>();
            int propertyCount = _random.Next(1, Math.Min(_maxChildren, 10));

            for (int i = 0; i < propertyCount; i++)
            {
                string key = GenerateRandomString(1, 20);
                string value = GenerateJsonValue(1); // Depth 1 to avoid excessive nesting
                properties.Add($"\"{key}\": {value}");
            }

            return $"{{{string.Join(", ", properties)}}}";
        }

        /// <summary>
        /// Generates JSON specifically targeting Neo.Json features
        /// </summary>
        private string GenerateNeoSpecificJson()
        {
            int feature = _random.Next(5);

            return feature switch
            {
                0 => GenerateJPathTestJson(), // Test JPath functionality
                1 => GenerateNestingLevelJson(), // Test different max_nest values
                2 => GenerateTypeConversionJson(), // Test type conversion methods
                3 => GenerateSerializationTestJson(), // Test serialization
                _ => GenerateNeoJsonEdgeCaseJson() // Neo.Json specific edge cases
            };
        }

        /// <summary>
        /// Generates JSON specifically for testing JPath functionality
        /// </summary>
        private string GenerateJPathTestJson()
        {
            string template = _jPathTestTemplates[_random.Next(_jPathTestTemplates.Length)];
            
            if (template.Contains("{0}") && template.Contains("{1}") && template.Contains("{2}"))
            {
                return string.Format(template, 
                    GenerateSimpleJson(), 
                    GenerateSimpleJson(), 
                    GenerateSimpleJson());
            }
            else if (template.Contains("{0}") && template.Contains("{1}"))
            {
                return string.Format(template, 
                    GenerateSimpleJson(), 
                    GenerateSimpleJson());
            }
            else if (template.Contains("{0}"))
            {
                return string.Format(template, GenerateSimpleJson());
            }
            
            return template;
        }

        /// <summary>
        /// Generates JSON to test different nesting level limits in Neo.Json
        /// </summary>
        private string GenerateNestingLevelJson()
        {
            // Choose a nesting level close to one of Neo.Json's limits
            int nestingTarget = _random.Next(3) switch
            {
                0 => NEO_LOW_MAX_NEST - _random.Next(3), // Just below low limit
                1 => NEO_DEFAULT_MAX_NEST - _random.Next(5), // Just below default limit
                _ => NEO_HIGH_MAX_NEST - _random.Next(10) // Just below high limit
            };
            
            // Generate nested structure
            string json = "null";
            for (int i = 0; i < nestingTarget; i++)
            {
                if (_random.Next(2) == 0)
                {
                    json = $"{{\"level{i}\": {json}}}";
                }
                else
                {
                    json = $"[{json}]";
                }
            }
            
            return json;
        }

        /// <summary>
        /// Generates JSON to test type conversion methods in Neo.Json
        /// </summary>
        private string GenerateTypeConversionJson()
        {
            int type = _random.Next(6);
            
            return type switch
            {
                0 => $"{{\"boolean\": {(_random.Next(2) == 0 ? "true" : "false")}}}",
                1 => $"{{\"number\": {GenerateRandomNumber()}}}",
                2 => $"{{\"string\": \"{GenerateRandomString()}\"}}",
                3 => (_random.Next(2) == 0 ? "true" : "false"), // Direct boolean
                4 => GenerateRandomNumber().ToString(), // Direct number
                _ => $"\"{GenerateRandomString()}\"" // Direct string
            };
        }

        /// <summary>
        /// Generates JSON to test serialization functionality
        /// </summary>
        private string GenerateSerializationTestJson()
        {
            // Create a complex but valid JSON structure for serialization testing
            var properties = new List<string>();
            int propertyCount = _random.Next(3, 8);
            
            for (int i = 0; i < propertyCount; i++)
            {
                string key = $"prop{i}";
                
                // Mix of different value types
                string value = (i % 5) switch
                {
                    0 => $"\"{GenerateRandomString(5, 20)}\"",
                    1 => GenerateRandomNumber().ToString(),
                    2 => _random.Next(2) == 0 ? "true" : "false",
                    3 => GenerateJsonArray(1),
                    _ => GenerateJsonObject(1)
                };
                
                properties.Add($"\"{key}\": {value}");
            }
            
            return $"{{{string.Join(", ", properties)}}}";
        }

        /// <summary>
        /// Generates JSON targeting Neo.Json specific edge cases
        /// </summary>
        private string GenerateNeoJsonEdgeCaseJson()
        {
            int edgeCase = _random.Next(5);
            
            return edgeCase switch
            {
                0 => $"{{\"unicode\": \"{GenerateUnicodeString(20, 50)}\"}}",
                1 => $"{{\"escape\": \"\\b\\f\\n\\r\\t\\\\\\\"\\/\"}}",
                2 => $"{{\"emptyArray\": [], \"emptyObject\": {{}}, \"null\": null}}",
                3 => $"{{\"nestedEmpty\": {{\"empty\": {{}}}}, \"nestedArray\": [[]]}}",
                _ => $"{{\"mixedTypes\": [{GenerateRandomNumber().ToString()}, \"{GenerateRandomString()}\", {(_random.Next(2) == 0 ? "true" : "false")}, null]}}"
            };
        }

        /// <summary>
        /// Generates JSON specifically targeting edge cases
        /// </summary>
        private string GenerateEdgeCaseJson()
        {
            int edgeCase = _random.Next(8);

            return edgeCase switch
            {
                0 => GenerateDeepNestedJson(), // Test max nesting
                1 => GenerateLongStringJson(), // Test string handling
                2 => GenerateLargeNumberJson(), // Test numeric boundaries
                3 => GenerateLargeArrayJson(), // Test array handling
                4 => GenerateLargeObjectJson(), // Test object handling
                5 => GenerateUnicodeHeavyJson(), // Test Unicode handling
                6 => GenerateSpecialCharJson(), // Test special character handling
                _ => GenerateDuplicateKeysJson() // Test duplicate key handling
            };
        }

        /// <summary>
        /// Generates intentionally malformed JSON to test error handling
        /// </summary>
        private string GenerateMalformedJson()
        {
            string validJson = GenerateSimpleJson();
            int malformationType = _random.Next(10);

            return malformationType switch
            {
                0 => validJson.Replace("{", ""), // Missing opening brace
                1 => validJson.Replace("}", ""), // Missing closing brace
                2 => validJson.Replace("[", ""), // Missing opening bracket
                3 => validJson.Replace("]", ""), // Missing closing bracket
                4 => validJson.Replace("\"", ""), // Missing quotes
                5 => validJson.Replace(",", ",,"), // Extra commas
                6 => validJson.Replace(":", ""), // Missing colons
                7 => $"{validJson}{validJson}", // Concatenated JSON
                8 => validJson.Substring(0, validJson.Length / 2), // Truncated JSON
                _ => InsertRandomCharacters(validJson) // Random corruption
            };
        }

        /// <summary>
        /// Generates nested JSON structures to test depth handling
        /// </summary>
        private string GenerateNestedJson()
        {
            // Choose a template
            string template = _jsonTemplates[_random.Next(_jsonTemplates.Length)];
            
            // Fill in the template with random values
            if (template.Contains("{0}") && template.Contains("{1}"))
            {
                return string.Format(template, GenerateJsonValue(1), GenerateJsonValue(1));
            }
            else if (template.Contains("{0}"))
            {
                return string.Format(template, GenerateJsonValue(1));
            }
            else
            {
                return template;
            }
        }

        /// <summary>
        /// Generates a deeply nested JSON structure to test max depth handling
        /// </summary>
        private string GenerateDeepNestedJson()
        {
            // Generate a deeply nested structure close to Neo.Json's limits
            int targetDepth = _random.Next(3) switch
            {
                0 => NEO_LOW_MAX_NEST - 1, // Test low limit
                1 => NEO_DEFAULT_MAX_NEST - 1, // Test default limit
                _ => NEO_HIGH_MAX_NEST - 1 // Test high limit
            };
            
            string json = "null";

            for (int i = 0; i < targetDepth; i++)
            {
                if (_random.Next(2) == 0)
                {
                    // Nest in an object
                    json = $"{{\"level{i}\": {json}}}";
                }
                else
                {
                    // Nest in an array
                    json = $"[{json}]";
                }
            }

            return json;
        }

        /// <summary>
        /// Generates JSON with a very long string
        /// </summary>
        private string GenerateLongStringJson()
        {
            // Use a reasonable length to avoid excessive memory usage
            int length = _random.Next(100, Math.Min(_maxStringLength, 5000));
            string longString = GenerateRandomString(length, length);
            return $"{{\"longString\": \"{longString}\"}}";
        }

        /// <summary>
        /// Generates JSON with large numeric values
        /// </summary>
        private string GenerateLargeNumberJson()
        {
            int type = _random.Next(5);
            string number = type switch
            {
                0 => $"{1e15 + _random.Next(1000)}", // Large but valid number
                1 => $"{-1e15 - _random.Next(1000)}", // Large negative
                2 => $"{_random.Next(1000) * 0.0001}", // Small decimal
                3 => $"{_random.Next(1000)}.{_random.Next(1000)}", // Mixed decimal
                _ => $"{_random.Next(1000)}.{_random.Next(1000)}e{_random.Next(-10, 10)}" // Scientific notation
            };

            return $"{{\"largeNumber\": {number}}}";
        }

        /// <summary>
        /// Generates JSON with a large array
        /// </summary>
        private string GenerateLargeArrayJson()
        {
            // Use a reasonable size to avoid excessive memory usage
            int size = _random.Next(10, Math.Min(_maxChildren, 100));
            var elements = new List<string>();

            for (int i = 0; i < size; i++)
            {
                elements.Add(GenerateSimpleJson());
            }

            return $"[{string.Join(", ", elements)}]";
        }

        /// <summary>
        /// Generates JSON with a large object
        /// </summary>
        private string GenerateLargeObjectJson()
        {
            // Use a reasonable size to avoid excessive memory usage
            int size = _random.Next(10, Math.Min(_maxChildren, 50));
            var properties = new List<string>();

            for (int i = 0; i < size; i++)
            {
                string key = $"prop{i}";
                string value = GenerateSimpleJson();
                properties.Add($"\"{key}\": {value}");
            }

            return $"{{{string.Join(", ", properties)}}}";
        }

        /// <summary>
        /// Generates JSON with heavy use of Unicode characters
        /// </summary>
        private string GenerateUnicodeHeavyJson()
        {
            var unicodeStrings = new List<string>();
            
            for (int i = 0; i < 3; i++) // Reduced from 5 to 3 for efficiency
            {
                unicodeStrings.Add($"\"{GenerateUnicodeString(10, 30)}\"");
            }
            
            return $"{{\"unicodeStrings\": [{string.Join(", ", unicodeStrings)}]}}";
        }

        /// <summary>
        /// Generates a string with Unicode characters
        /// </summary>
        private string GenerateUnicodeString(int minLength, int maxLength)
        {
            int length = _random.Next(minLength, maxLength + 1);
            StringBuilder sb = new();
            
            for (int j = 0; j < length; j++)
            {
                // Generate a random Unicode character, avoiding surrogate pairs
                char c;
                do
                {
                    c = (char)_random.Next(0x20, 0xFFFF);
                } while (c >= 0xD800 && c <= 0xDFFF);
                
                sb.Append(c);
            }
            
            return sb.ToString();
        }

        /// <summary>
        /// Generates JSON with special characters that need escaping
        /// </summary>
        private string GenerateSpecialCharJson()
        {
            var specialStrings = new List<string>();
            
            for (int i = 0; i < 3; i++) // Reduced from 5 to 3 for efficiency
            {
                StringBuilder sb = new();
                int length = _random.Next(10, 30);
                
                for (int j = 0; j < length; j++)
                {
                    // Mix normal and special characters
                    if (_random.Next(3) == 0)
                    {
                        sb.Append(_specialChars[_random.Next(_specialChars.Length)]);
                    }
                    else
                    {
                        // Normal ASCII characters
                        sb.Append((char)_random.Next(32, 127));
                    }
                }
                
                specialStrings.Add($"\"{sb}\"");
            }
            
            return $"{{\"specialStrings\": [{string.Join(", ", specialStrings)}]}}";
        }

        /// <summary>
        /// Generates JSON with duplicate keys to test handling
        /// </summary>
        private string GenerateDuplicateKeysJson()
        {
            string key = GenerateRandomString(1, 10);
            string value1 = GenerateSimpleJson();
            string value2 = GenerateSimpleJson();
            
            return $"{{\"{key}\": {value1}, \"{key}\": {value2}}}";
        }

        /// <summary>
        /// Generates a JSON value of a random type with controlled depth
        /// </summary>
        private string GenerateJsonValue(int depth)
        {
            // Limit recursion
            if (depth >= _maxDepth)
            {
                return GenerateSimpleJson();
            }

            int type = _random.Next(7);

            return type switch
            {
                0 => $"\"{GenerateRandomString()}\"",
                1 => GenerateRandomNumber(),
                2 => "true",
                3 => "false",
                4 => "null",
                5 => GenerateJsonObject(depth + 1),
                _ => GenerateJsonArray(depth + 1)
            };
        }

        /// <summary>
        /// Generates a JSON object with controlled depth
        /// </summary>
        private string GenerateJsonObject(int depth)
        {
            if (depth >= _maxDepth)
            {
                return "{}";
            }

            int propertyCount = _random.Next(0, Math.Min(_maxChildren, 5));
            var properties = new List<string>();

            for (int i = 0; i < propertyCount; i++)
            {
                string key = GenerateRandomString(1, 10);
                string value = GenerateJsonValue(depth + 1);
                properties.Add($"\"{key}\": {value}");
            }

            return $"{{{string.Join(", ", properties)}}}";
        }

        /// <summary>
        /// Generates a JSON array with controlled depth
        /// </summary>
        private string GenerateJsonArray(int depth)
        {
            if (depth >= _maxDepth)
            {
                return "[]";
            }

            int elementCount = _random.Next(0, Math.Min(_maxChildren, 5));
            var elements = new List<string>();

            for (int i = 0; i < elementCount; i++)
            {
                elements.Add(GenerateJsonValue(depth + 1));
            }

            return $"[{string.Join(", ", elements)}]";
        }

        /// <summary>
        /// Generates a random string of specified length
        /// </summary>
        private string GenerateRandomString(int minLength = 0, int maxLength = 20)
        {
            int length = _random.Next(minLength, maxLength + 1);
            var sb = new StringBuilder(length);

            for (int i = 0; i < length; i++)
            {
                // Occasionally insert special characters
                if (_random.Next(10) == 0)
                {
                    sb.Append(_specialChars[_random.Next(_specialChars.Length)]);
                }
                else
                {
                    // Normal ASCII characters
                    sb.Append((char)_random.Next(32, 127));
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Generates a random number as a string
        /// </summary>
        private string GenerateRandomNumber()
        {
            int type = _random.Next(5);

            return type switch
            {
                0 => _random.Next(-1000, 1000).ToString(), // Integer
                1 => (_random.NextDouble() * 1000).ToString("0.0###"), // Decimal with controlled precision
                2 => (_random.Next(-1000, 1000) * 0.01).ToString("0.00"), // Small decimal with fixed precision
                3 => (_random.NextDouble() * 1e6).ToString("E3"), // Scientific notation with controlled precision
                _ => "0" // Zero
            };
        }

        /// <summary>
        /// Inserts random characters into a string to create malformed JSON
        /// </summary>
        private string InsertRandomCharacters(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return input;
            }

            var sb = new StringBuilder(input);
            int insertions = _random.Next(1, 3); // Reduced from 5 to 3 for more controlled corruption

            for (int i = 0; i < insertions; i++)
            {
                int position = _random.Next(sb.Length);
                char c = (char)_random.Next(32, 127);
                sb.Insert(position, c);
            }

            return sb.ToString();
        }
    }
}
