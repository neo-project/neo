// Copyright (C) 2015-2025 The Neo Project.
//
// BooleanMutations.cs file belongs to the neo project and is free
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

namespace Neo.Json.Fuzzer.Generators
{
    /// <summary>
    /// Provides boolean-related mutation strategies for the mutation engine
    /// </summary>
    public class BooleanMutations
    {
        private readonly BaseMutationEngine _engine;
        private readonly Random _random;

        /// <summary>
        /// Initializes a new instance of the BooleanMutations class
        /// </summary>
        public BooleanMutations(BaseMutationEngine engine, Random random)
        {
            _engine = engine ?? throw new ArgumentNullException(nameof(engine));
            _random = random ?? throw new ArgumentNullException(nameof(random));
        }

        /// <summary>
        /// Modifies a boolean value in the JSON
        /// </summary>
        public string ModifyBooleanValue(string json)
        {
            try
            {
                var token = JToken.Parse(json);
                if (token != null)
                {
                    ModifyBooleanValuesRecursive(token);
                }
                return token?.ToString() ?? json;
            }
            catch
            {
                // If parsing fails, return the original JSON
                return json;
            }
        }

        /// <summary>
        /// Recursively modifies boolean values in a JToken
        /// </summary>
        private void ModifyBooleanValuesRecursive(JToken token)
        {
            if (token == null)
                return;

            if (token is JObject obj)
            {
                // Collect boolean properties
                var boolProperties = new List<(string Key, JToken Value)>();
                foreach (var kvp in obj.Properties)
                {
                    if (kvp.Value != null && kvp.Value is JBoolean)
                    {
                        boolProperties.Add((kvp.Key, kvp.Value));
                    }
                }

                // Modify a random boolean property if any exist
                if (boolProperties.Count > 0 && _random.NextDouble() < 0.5)
                {
                    var (key, value) = boolProperties[_random.Next(boolProperties.Count)];
                    obj[key] = GenerateBooleanValue(value.AsBoolean());
                }

                // Recursively process all properties
                foreach (var kvp in obj.Properties)
                {
                    if (kvp.Value != null)
                    {
                        ModifyBooleanValuesRecursive(kvp.Value);
                    }
                }
            }
            else if (token is JArray array)
            {
                // Recursively process all array elements
                foreach (var item in array)
                {
                    if (item != null)
                    {
                        ModifyBooleanValuesRecursive(item);
                    }
                }
            }
        }

        /// <summary>
        /// Generates a boolean value, potentially inverting the input
        /// </summary>
        public bool GenerateBooleanValue(bool? currentValue = null)
        {
            // If we have a current value, decide whether to invert it
            if (currentValue.HasValue && _random.NextDouble() < 0.8)
            {
                return !currentValue.Value;
            }

            // Otherwise, generate a random boolean
            return _random.Next(2) == 0;
        }

        /// <summary>
        /// Converts a non-boolean value to a boolean
        /// </summary>
        public bool ConvertToBooleanValue(JToken token)
        {
            if (token == null)
                return false;

            switch (token)
            {
                case JBoolean boolToken:
                    return boolToken.Value;

                case JNumber number:
                    // Convert numbers to boolean (0 = false, non-0 = true)
                    double numberValue = number.Value;
                    return Math.Abs(numberValue) > double.Epsilon;

                case JString str:
                    // Convert strings to boolean
                    string stringValue = str.Value;
                    return !string.IsNullOrEmpty(stringValue) &&
                           !string.Equals(stringValue, "false", StringComparison.OrdinalIgnoreCase) &&
                           !string.Equals(stringValue, "0", StringComparison.OrdinalIgnoreCase);

                case JArray array:
                    // Arrays are true if they have elements
                    return array.Count > 0;

                case JObject obj:
                    // Objects are true if they have properties
                    return obj.Properties.Count() > 0;

                default:
                    return false;
            }
        }

        /// <summary>
        /// Replaces a value with its boolean equivalent
        /// </summary>
        public string ReplaceBooleanEquivalent(string json)
        {
            try
            {
                var token = JToken.Parse(json);
                if (token != null)
                {
                    ReplaceBooleanEquivalentRecursive(token);
                }
                return token?.ToString() ?? json;
            }
            catch
            {
                // If parsing fails, return the original JSON
                return json;
            }
        }

        /// <summary>
        /// Recursively replaces values with their boolean equivalents
        /// </summary>
        private void ReplaceBooleanEquivalentRecursive(JToken token)
        {
            if (token == null)
                return;

            if (token is JObject obj)
            {
                // Collect non-boolean properties
                var nonBoolProperties = new List<(string Key, JToken Value)>();
                foreach (var kvp in obj.Properties)
                {
                    if (kvp.Value != null && !(kvp.Value is JBoolean))
                    {
                        nonBoolProperties.Add((kvp.Key, kvp.Value));
                    }
                }

                // Replace a random non-boolean property with its boolean equivalent
                if (nonBoolProperties.Count > 0 && _random.NextDouble() < 0.2)
                {
                    var (key, value) = nonBoolProperties[_random.Next(nonBoolProperties.Count)];
                    obj[key] = ConvertToBooleanValue(value);
                }

                // Recursively process all properties
                foreach (var kvp in obj.Properties)
                {
                    if (kvp.Value != null)
                    {
                        ReplaceBooleanEquivalentRecursive(kvp.Value);
                    }
                }
            }
            else if (token is JArray array)
            {
                // Replace a random array element with its boolean equivalent
                if (array.Count > 0 && _random.NextDouble() < 0.2)
                {
                    int index = _random.Next(array.Count);
                    JToken value = array[index];

                    if (value != null && !(value is JBoolean))
                    {
                        array[index] = ConvertToBooleanValue(value);
                    }
                }

                // Recursively process all array elements
                foreach (var item in array)
                {
                    if (item != null)
                    {
                        ReplaceBooleanEquivalentRecursive(item);
                    }
                }
            }
        }

        /// <summary>
        /// Replaces a boolean value with a string representation
        /// </summary>
        public string ReplaceBooleanWithString(string json)
        {
            try
            {
                var token = JToken.Parse(json);
                if (token != null)
                {
                    ReplaceBooleanWithStringRecursive(token);
                }
                return token?.ToString() ?? json;
            }
            catch
            {
                // If parsing fails, return the original JSON
                return json;
            }
        }

        /// <summary>
        /// Recursively replaces boolean values with string representations
        /// </summary>
        private void ReplaceBooleanWithStringRecursive(JToken token)
        {
            if (token == null)
                return;

            if (token is JObject obj)
            {
                // Collect boolean properties
                var boolProperties = new List<(string Key, JToken Value)>();
                foreach (var kvp in obj.Properties)
                {
                    if (kvp.Value != null && kvp.Value is JBoolean)
                    {
                        boolProperties.Add((kvp.Key, kvp.Value));
                    }
                }

                // Replace a random boolean property with its string representation
                if (boolProperties.Count > 0 && _random.NextDouble() < 0.3)
                {
                    var (key, value) = boolProperties[_random.Next(boolProperties.Count)];
                    bool boolValue = value.AsBoolean();

                    // Choose a string representation
                    int strategy = _random.Next(3);
                    string stringValue = strategy switch
                    {
                        0 => boolValue ? "true" : "false",
                        1 => boolValue ? "True" : "False",
                        _ => boolValue ? "1" : "0"
                    };

                    obj[key] = stringValue;
                }

                // Recursively process all properties
                foreach (var kvp in obj.Properties)
                {
                    if (kvp.Value != null)
                    {
                        ReplaceBooleanWithStringRecursive(kvp.Value);
                    }
                }
            }
            else if (token is JArray array)
            {
                // Recursively process all array elements
                foreach (var item in array)
                {
                    if (item != null)
                    {
                        ReplaceBooleanWithStringRecursive(item);
                    }
                }
            }
        }
    }
}
