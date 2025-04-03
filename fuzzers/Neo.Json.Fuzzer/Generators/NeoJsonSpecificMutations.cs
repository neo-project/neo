// Copyright (C) 2015-2025 The Neo Project.
//
// NeoJsonSpecificMutations.cs file belongs to the neo project and is free
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
    /// Provides Neo.Json-specific mutation strategies for the mutation engine
    /// </summary>
    public class NeoJsonSpecificMutations
    {
        private readonly BaseMutationEngine _engine;
        private readonly Random _random;
        private readonly StringMutations _stringMutations;

        /// <summary>
        /// Initializes a new instance of the NeoJsonSpecificMutations class
        /// </summary>
        public NeoJsonSpecificMutations(BaseMutationEngine engine, Random random)
        {
            _engine = engine ?? throw new ArgumentNullException(nameof(engine));
            _random = random ?? throw new ArgumentNullException(nameof(random));
            _stringMutations = new StringMutations(engine, random);
        }

        /// <summary>
        /// Applies a Neo.Json-specific mutation to the JSON
        /// </summary>
        public string ApplyNeoJsonSpecificMutation(string json)
        {
            try
            {
                // Select a mutation strategy
                int strategy = _random.Next(5);

                return strategy switch
                {
                    0 => ModifyNestingLevel(json),
                    1 => AddJPathTestProperty(json),
                    2 => TestTypeConversion(json),
                    3 => TestSerializationEdgeCases(json),
                    _ => TestParsingEdgeCases(json)
                };
            }
            catch
            {
                // If mutation fails, return the original JSON
                return json;
            }
        }

        /// <summary>
        /// Modifies the nesting level of the JSON
        /// </summary>
        public string ModifyNestingLevel(string json)
        {
            try
            {
                var token = JToken.Parse(json);

                // Select a test strategy for nesting level
                int strategy = _random.Next(5);

                // Add null check before using token
                if (token == null)
                {
                    return CreateExactLimitNestedStructure();
                }

                return strategy switch
                {
                    0 => ModifyWithBasicNesting(token),
                    1 => CreateExactLimitNestedStructure(),
                    2 => CreateMinimalDosVector(),
                    3 => CreateRepeatedPatternNestedStructure(),
                    _ => CreateAlternatingTypesStructure()
                };
            }
            catch
            {
                // If parsing fails, return the original JSON
                return json;
            }
        }

        /// <summary>
        /// Modifies the JSON with basic nesting
        /// </summary>
        private string ModifyWithBasicNesting(JToken token)
        {
            // Determine the maximum nesting level
            int maxNest = _random.Next(1, BaseMutationEngine.NEO_DEFAULT_MAX_NEST - 10);

            // Create a structure with the specified nesting level
            StructureMutations structureMutations = new StructureMutations(_engine, _random);
            JToken nestedStructure = structureMutations.CreateNestedStructure(maxNest);

            return nestedStructure.ToString();
        }

        /// <summary>
        /// Creates a structure with nesting exactly at the limit
        /// </summary>
        private string CreateExactLimitNestedStructure()
        {
            // Select a nesting limit to test
            int[] nestingLimits = new[] { BaseMutationEngine.NEO_LOW_MAX_NEST, BaseMutationEngine.NEO_DEFAULT_MAX_NEST, BaseMutationEngine.NEO_HIGH_MAX_NEST };
            int limit = nestingLimits[_random.Next(nestingLimits.Length)];

            // Create a structure with nesting exactly at the limit
            StructureMutations structureMutations = new StructureMutations(_engine, _random);
            JToken nestedStructure = structureMutations.CreateNestedStructure(limit);

            return nestedStructure.ToString();
        }

        /// <summary>
        /// Creates a minimal DOS vector
        /// </summary>
        private string CreateMinimalDosVector()
        {
            // Create a minimal input that has previously triggered high processing times
            string[] minimalVectors = new[]
            {
                "\"\\u0000\"",
                "\"\\u0001\"",
                "\"\\u0002\"",
                "\"\\u0003\"",
                "\"\\u0004\"",
                "\"\\u0005\"",
                "\"\\u0006\"",
                "\"\\u0007\"",
                "\"\\b\"",
                "\"\\t\"",
                "\"\\n\"",
                "\"\\f\"",
                "\"\\r\"",
                "\"\\u000e\"",
                "\"\\u000f\"",
                "\"\\u0010\"",
                "\"\\u0011\"",
                "\"\\u0012\"",
                "\"\\u0013\"",
                "\"\\u0014\"",
                "\"\\u0015\"",
                "\"\\u0016\"",
                "\"\\u0017\"",
                "\"\\u0018\"",
                "\"\\u0019\"",
                "\"\\u001a\"",
                "\"\\u001b\"",
                "\"\\u001c\"",
                "\"\\u001d\"",
                "\"\\u001e\"",
                "\"\\u001f\""
            };

            return minimalVectors[_random.Next(minimalVectors.Length)];
        }

        /// <summary>
        /// Creates a structure with repeated patterns
        /// </summary>
        private string CreateRepeatedPatternNestedStructure()
        {
            // Create a pattern for repetition
            string pattern = CreatePatternForRepetition();

            // Repeat the pattern
            int repetitions = _random.Next(10, 100);
            StringBuilder sb = new StringBuilder(pattern.Length * repetitions);

            for (int i = 0; i < repetitions; i++)
            {
                sb.Append(pattern);
            }

            return sb.ToString();
        }

        /// <summary>
        /// Creates a pattern for repetition
        /// </summary>
        private string CreatePatternForRepetition()
        {
            int patternType = _random.Next(3);

            return patternType switch
            {
                0 => "{\"a\":{}}",
                1 => "[{}]",
                _ => "{\"a\":[]}"
            };
        }

        /// <summary>
        /// Creates a structure with alternating types
        /// </summary>
        private string CreateAlternatingTypesStructure()
        {
            // Create a structure with alternating types
            StructureMutations structureMutations = new StructureMutations(_engine, _random);
            int depth = _random.Next(5, 20);
            JToken structure = structureMutations.CreateAlternatingStructure(depth);

            return structure.ToString();
        }

        /// <summary>
        /// Adds a JPath test property to the JSON
        /// </summary>
        public string AddJPathTestProperty(string json)
        {
            try
            {
                var token = JToken.Parse(json);

                if (token is JObject obj)
                {
                    // Add a property with a JPath-testable value
                    string propertyName = "jpath_test_" + _random.Next(1000);
                    obj[propertyName] = CreateJPathTestValue();

                    return obj.ToString();
                }
                else if (token is JArray array && array.Count > 0)
                {
                    // Find an object in the array
                    foreach (var item in array)
                    {
                        if (item is JObject objItem)
                        {
                            // Add a property with a JPath-testable value
                            string propertyName = "jpath_test_" + _random.Next(1000);
                            objItem[propertyName] = CreateJPathTestValue();
                            break;
                        }
                    }

                    return array.ToString();
                }

                // If not an object or array, wrap in an object
                JObject wrapper = new JObject();
                wrapper["original"] = token;
                wrapper["jpath_test"] = CreateJPathTestValue();

                return wrapper.ToString();
            }
            catch
            {
                // If parsing fails, return the original JSON
                return json;
            }
        }

        /// <summary>
        /// Creates a value for JPath testing
        /// </summary>
        private JToken CreateJPathTestValue()
        {
            int valueType = _random.Next(5);

            return valueType switch
            {
                0 => CreateJPathTestObject(),
                1 => CreateJPathTestArray(),
                2 => _stringMutations.GenerateRandomString(10),
                3 => _random.Next(100),
                _ => _random.Next(2) == 0 ? (JToken)true : (JToken)false
            };
        }

        /// <summary>
        /// Creates an object for JPath testing
        /// </summary>
        private JObject CreateJPathTestObject()
        {
            JObject obj = new JObject();

            // Add properties with various types
            obj["string"] = _stringMutations.GenerateRandomString(10);
            obj["number"] = _random.Next(100);
            obj["boolean"] = _random.Next(2) == 0;
            obj["null"] = null;

            // Add a nested object
            JObject nested = new JObject();
            nested["nested_string"] = _stringMutations.GenerateRandomString(5);
            nested["nested_number"] = _random.Next(100);
            obj["nested"] = nested;

            // Add an array
            JArray array = new JArray();
            for (int i = 0; i < 3; i++)
            {
                array.Add(_random.Next(10));
            }
            obj["array"] = array;

            return obj;
        }

        /// <summary>
        /// Creates an array for JPath testing
        /// </summary>
        private JArray CreateJPathTestArray()
        {
            JArray array = new JArray
            {
                // Add elements with various types
                _stringMutations.GenerateRandomString(10),
                _random.Next(100),
                _random.Next(2) == 0,
                null
            };

            // Add a nested object
            JObject obj = new JObject();
            obj["key"] = _stringMutations.GenerateRandomString(5);
            obj["value"] = _random.Next(100);
            array.Add(obj);

            // Add a nested array
            JArray nested = new JArray();
            for (int i = 0; i < 3; i++)
            {
                nested.Add(_random.Next(10));
            }
            array.Add(nested);

            return array;
        }

        /// <summary>
        /// Tests type conversion in Neo.Json
        /// </summary>
        public string TestTypeConversion(string json)
        {
            try
            {
                var token = JToken.Parse(json);

                // Select a type conversion to test
                int conversionType = _random.Next(5);

                return conversionType switch
                {
                    0 => token != null ? ConvertNumberToString(token) : json,
                    1 => token != null ? ConvertStringToNumber(token) : json,
                    2 => token != null ? ConvertBooleanToString(token) : json,
                    3 => token != null ? ConvertStringToBoolean(token) : json,
                    _ => token != null ? ConvertArrayToObject(token) : json
                };
            }
            catch
            {
                // If parsing fails, return the original JSON
                return json;
            }
        }

        /// <summary>
        /// Converts a number to a string
        /// </summary>
        private string ConvertNumberToString(JToken token)
        {
            if (token == null)
                return string.Empty;

            if (token is JObject obj)
            {
                // Find a numeric property
                foreach (var kvp in obj.Properties)
                {
                    if (kvp.Value != null && kvp.Value is JNumber jNumber)
                    {
                        // Convert to string
                        obj[kvp.Key] = new JString(jNumber.ToString());
                        break;
                    }
                }

                return obj.ToString();
            }
            else if (token is JArray array)
            {
                // Find a numeric element
                for (int i = 0; i < array.Count; i++)
                {
                    if (array[i] is JNumber jNumber)
                    {
                        // Convert to string
                        array[i] = new JString(jNumber.ToString());
                        break;
                    }
                }

                return array.ToString();
            }

            return token.ToString();
        }

        /// <summary>
        /// Converts a string to a number
        /// </summary>
        private string ConvertStringToNumber(JToken token)
        {
            if (token is JObject obj)
            {
                // Find a string property
                foreach (var kvp in obj.Properties)
                {
                    if (kvp.Value != null && kvp.Value is JString jString)
                    {
                        string str = jString.Value.ToLower();

                        // Try to convert to number
                        if (int.TryParse(str, out int intValue))
                        {
                            obj[kvp.Key] = intValue;
                            break;
                        }
                        else if (double.TryParse(str, out double doubleValue))
                        {
                            obj[kvp.Key] = doubleValue;
                            break;
                        }
                    }
                }

                return obj.ToString();
            }
            else if (token is JArray array)
            {
                // Find a string element
                for (int i = 0; i < array.Count; i++)
                {
                    if (array[i] is JString jString)
                    {
                        string str = jString.Value.ToLower();

                        // Try to convert to number
                        if (int.TryParse(str, out int intValue))
                        {
                            array[i] = intValue;
                            break;
                        }
                        else if (double.TryParse(str, out double doubleValue))
                        {
                            array[i] = doubleValue;
                            break;
                        }
                    }
                }

                return array.ToString();
            }

            return token.ToString();
        }

        /// <summary>
        /// Converts a boolean to a string
        /// </summary>
        private string ConvertBooleanToString(JToken token)
        {
            if (token is JObject obj)
            {
                // Find a boolean property
                foreach (var kvp in obj.Properties)
                {
                    if (kvp.Value != null && kvp.Value is JBoolean jBoolean)
                    {
                        // Convert to string
                        obj[kvp.Key] = jBoolean.AsBoolean() ? "true" : "false";
                        break;
                    }
                }

                return obj.ToString();
            }
            else if (token is JArray array)
            {
                // Find a boolean element
                for (int i = 0; i < array.Count; i++)
                {
                    if (array[i] != null && array[i] is JBoolean jBoolean)
                    {
                        // Convert to string
                        array[i] = jBoolean.AsBoolean() ? "true" : "false";
                        break;
                    }
                }

                return array.ToString();
            }

            return token.ToString();
        }

        /// <summary>
        /// Converts a string to a boolean
        /// </summary>
        private string ConvertStringToBoolean(JToken token)
        {
            if (token is JObject obj)
            {
                // Find a string property
                foreach (var kvp in obj.Properties)
                {
                    if (kvp.Value != null && kvp.Value is JString jString)
                    {
                        string str = jString.Value.ToLower();

                        // Convert to boolean
                        if (str == "true" || str == "1" || str == "yes")
                        {
                            obj[kvp.Key] = true;
                            break;
                        }
                        else if (str == "false" || str == "0" || str == "no")
                        {
                            obj[kvp.Key] = false;
                            break;
                        }
                    }
                }

                return obj.ToString();
            }
            else if (token is JArray array)
            {
                // Find a string element
                for (int i = 0; i < array.Count; i++)
                {
                    if (array[i] is JString jString)
                    {
                        string str = jString.Value.ToLower();

                        // Convert to boolean
                        if (str == "true" || str == "1" || str == "yes")
                        {
                            array[i] = true;
                            break;
                        }
                        else if (str == "false" || str == "0" || str == "no")
                        {
                            array[i] = false;
                            break;
                        }
                    }
                }

                return array.ToString();
            }

            return token.ToString();
        }

        /// <summary>
        /// Converts an array to an object
        /// </summary>
        private string ConvertArrayToObject(JToken token)
        {
            if (token is JObject obj)
            {
                // Find an array property
                foreach (var kvp in obj.Properties)
                {
                    if (kvp.Value != null && kvp.Value is JArray array && array.Count > 0)
                    {
                        // Convert to object
                        JObject newObj = new JObject();

                        for (int i = 0; i < array.Count; i++)
                        {
                            newObj["item" + i] = array[i];
                        }

                        obj[kvp.Key] = newObj;
                        break;
                    }
                }

                return obj.ToString();
            }
            else if (token is JArray array && array.Count > 0)
            {
                // Convert to object
                JObject newObj = new JObject();

                for (int i = 0; i < array.Count; i++)
                {
                    newObj["item" + i] = array[i];
                }

                return newObj.ToString();
            }

            return token.ToString();
        }

        /// <summary>
        /// Tests serialization edge cases in Neo.Json
        /// </summary>
        public string TestSerializationEdgeCases(string json)
        {
            try
            {
                // Select a serialization edge case to test
                int edgeCase = _random.Next(5);

                return edgeCase switch
                {
                    0 => AddUnicodeEscapes(json),
                    1 => AddControlCharacters(json),
                    2 => AddLongStrings(json),
                    3 => AddDeepNesting(json),
                    _ => AddCircularReference(json)
                };
            }
            catch
            {
                // If mutation fails, return the original JSON
                return json;
            }
        }

        /// <summary>
        /// Adds Unicode escapes to the JSON
        /// </summary>
        private string AddUnicodeEscapes(string json)
        {
            try
            {
                var token = JToken.Parse(json);

                if (token is JObject obj)
                {
                    // Add a property with Unicode escapes
                    string propertyName = "unicode_" + _random.Next(1000);
                    obj[propertyName] = _stringMutations.GenerateUnicodeString(10);

                    return obj.ToString();
                }
                else if (token is JArray array)
                {
                    // Add an element with Unicode escapes
                    array.Add(_stringMutations.GenerateUnicodeString(10));

                    return array.ToString();
                }

                // If not an object or array, wrap in an object
                JObject wrapper = new JObject();
                wrapper["original"] = token;
                wrapper["unicode"] = _stringMutations.GenerateUnicodeString(10);

                return wrapper.ToString();
            }
            catch
            {
                // If parsing fails, return the original JSON
                return json;
            }
        }

        /// <summary>
        /// Adds control characters to the JSON
        /// </summary>
        private string AddControlCharacters(string json)
        {
            try
            {
                var token = JToken.Parse(json);

                if (token is JObject obj)
                {
                    // Add a property with control characters
                    string propertyName = "control_" + _random.Next(1000);
                    obj[propertyName] = _stringMutations.GenerateJsonEscapeSequenceString(10);

                    return obj.ToString();
                }
                else if (token is JArray array)
                {
                    // Add an element with control characters
                    array.Add(_stringMutations.GenerateJsonEscapeSequenceString(10));

                    return array.ToString();
                }

                // If not an object or array, wrap in an object
                JObject wrapper = new JObject();
                wrapper["original"] = token;
                wrapper["control"] = _stringMutations.GenerateJsonEscapeSequenceString(10);

                return wrapper.ToString();
            }
            catch
            {
                // If parsing fails, return the original JSON
                return json;
            }
        }

        /// <summary>
        /// Adds long strings to the JSON
        /// </summary>
        private string AddLongStrings(string json)
        {
            try
            {
                var token = JToken.Parse(json);

                if (token is JObject obj)
                {
                    // Add a property with a long string
                    string propertyName = "long_string_" + _random.Next(1000);
                    obj[propertyName] = _stringMutations.GenerateRandomString(1000);

                    return obj.ToString();
                }
                else if (token is JArray array)
                {
                    // Add an element with a long string
                    array.Add(_stringMutations.GenerateRandomString(1000));

                    return array.ToString();
                }

                // If not an object or array, wrap in an object
                JObject wrapper = new JObject();
                wrapper["original"] = token;
                wrapper["long_string"] = _stringMutations.GenerateRandomString(1000);

                return wrapper.ToString();
            }
            catch
            {
                // If parsing fails, return the original JSON
                return json;
            }
        }

        /// <summary>
        /// Adds deep nesting to the JSON
        /// </summary>
        private string AddDeepNesting(string json)
        {
            try
            {
                var token = JToken.Parse(json);

                if (token is JObject obj)
                {
                    // Add a property with deep nesting
                    string propertyName = "deep_nesting_" + _random.Next(1000);
                    StructureMutations structureMutations = new StructureMutations(_engine, _random);
                    obj[propertyName] = structureMutations.CreateNestedStructure(20);

                    return obj.ToString();
                }
                else if (token is JArray array)
                {
                    // Add an element with deep nesting
                    StructureMutations structureMutations = new StructureMutations(_engine, _random);
                    array.Add(structureMutations.CreateNestedStructure(20));

                    return array.ToString();
                }

                // If not an object or array, wrap in an object
                JObject wrapper = new JObject();
                wrapper["original"] = token;
                StructureMutations structureMutations2 = new StructureMutations(_engine, _random);
                wrapper["deep_nesting"] = structureMutations2.CreateNestedStructure(20);

                return wrapper.ToString();
            }
            catch
            {
                // If parsing fails, return the original JSON
                return json;
            }
        }

        /// <summary>
        /// Adds a circular reference to the JSON (which will cause an error)
        /// </summary>
        private string AddCircularReference(string json)
        {
            // Create a simple object with a property that references itself
            // This will cause an error when serialized, which is the point
            JObject obj = new JObject();
            obj["name"] = "circular_reference";
            obj["value"] = 123;

            // This would cause a circular reference, but JObject doesn't allow it
            // So we'll just return a complex object instead
            JObject nested = new JObject();
            nested["parent"] = "reference_to_parent";
            obj["child"] = nested;

            return obj.ToString();
        }

        /// <summary>
        /// Tests parsing edge cases in Neo.Json
        /// </summary>
        public string TestParsingEdgeCases(string json)
        {
            // Select a parsing edge case to test
            int edgeCase = _random.Next(5);

            return edgeCase switch
            {
                0 => AddTrailingComma(json),
                1 => AddCommentLikeText(json),
                2 => AddExtraWhitespace(json),
                3 => AddInvalidEscapeSequence(json),
                _ => AddUnquotedPropertyName(json)
            };
        }

        /// <summary>
        /// Adds a trailing comma to the JSON (which will cause an error)
        /// </summary>
        private string AddTrailingComma(string json)
        {
            if (json.Contains("}"))
            {
                int index = json.LastIndexOf("}");
                return json.Substring(0, index) + "," + json.Substring(index);
            }
            else if (json.Contains("]"))
            {
                int index = json.LastIndexOf("]");
                return json.Substring(0, index) + "," + json.Substring(index);
            }

            return json;
        }

        /// <summary>
        /// Adds comment-like text to the JSON (which will cause an error)
        /// </summary>
        private string AddCommentLikeText(string json)
        {
            if (json.StartsWith("{"))
            {
                return "/* comment */" + json;
            }
            else if (json.StartsWith("["))
            {
                return "// comment\n" + json;
            }

            return json;
        }

        /// <summary>
        /// Adds extra whitespace to the JSON
        /// </summary>
        private string AddExtraWhitespace(string json)
        {
            StringBuilder sb = new StringBuilder(json.Length * 2);

            for (int i = 0; i < json.Length; i++)
            {
                sb.Append(json[i]);

                // Add whitespace after certain characters
                if (json[i] == '{' || json[i] == '[' || json[i] == ',' || json[i] == ':')
                {
                    int spaces = _random.Next(1, 5);
                    sb.Append(new string(' ', spaces));
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Adds an invalid escape sequence to the JSON (which will cause an error)
        /// </summary>
        private string AddInvalidEscapeSequence(string json)
        {
            if (json.Contains("\""))
            {
                int index = json.IndexOf("\"");
                int endIndex = json.IndexOf("\"", index + 1);

                if (endIndex > index)
                {
                    // Insert an invalid escape sequence
                    int insertIndex = index + 1 + _random.Next(endIndex - index - 1);
                    return json.Substring(0, insertIndex) + "\\x" + json.Substring(insertIndex);
                }
            }

            return json;
        }

        /// <summary>
        /// Adds an unquoted property name to the JSON (which will cause an error)
        /// </summary>
        private string AddUnquotedPropertyName(string json)
        {
            if (json.Contains("\""))
            {
                int index = json.IndexOf("\"");
                int endIndex = json.IndexOf("\"", index + 1);

                if (endIndex > index && json.Substring(index, endIndex - index + 1).Contains(":"))
                {
                    // Remove the quotes around a property name
                    return json.Substring(0, index) + json.Substring(index + 1, endIndex - index - 1) + json.Substring(endIndex + 1);
                }
            }

            return json;
        }
    }
}
