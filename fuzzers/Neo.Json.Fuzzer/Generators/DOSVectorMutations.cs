// Copyright (C) 2015-2025 The Neo Project.
//
// DOSVectorMutations.cs file belongs to the neo project and is free
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
    /// Provides specialized mutations for DOS vector testing
    /// </summary>
    public class DOSVectorMutations
    {
        private readonly BaseMutationEngine _engine;
        private readonly Random _random;
        private readonly StringMutations _stringMutations;
        private readonly StructureMutations _structureMutations;
        
        /// <summary>
        /// Initializes a new instance of the DOSVectorMutations class
        /// </summary>
        public DOSVectorMutations(BaseMutationEngine engine, Random random)
        {
            _engine = engine ?? throw new ArgumentNullException(nameof(engine));
            _random = random ?? throw new ArgumentNullException(nameof(random));
            _stringMutations = new StringMutations(engine, random);
            _structureMutations = new StructureMutations(engine, random);
        }
        
        /// <summary>
        /// Creates a minimal DOS vector
        /// </summary>
        public string CreateMinimalDosVector()
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
        /// Creates a deeply nested structure for DOS testing
        /// </summary>
        public string CreateDeeplyNestedStructure(int maxNest = 50)
        {
            JToken structure = _structureMutations.CreateNestedStructure(maxNest);
            return structure.ToString();
        }
        
        /// <summary>
        /// Creates a deeply nested array structure for DOS testing
        /// </summary>
        public string CreateDeeplyNestedArrayStructure(int maxNest = 50)
        {
            JToken structure = _structureMutations.CreateNestedArrayStructure(maxNest);
            return structure.ToString();
        }
        
        /// <summary>
        /// Creates a structure with alternating types for DOS testing
        /// </summary>
        public string CreateAlternatingTypesStructure(int maxNest = 20)
        {
            JToken structure = _structureMutations.CreateAlternatingStructure(maxNest);
            return structure.ToString();
        }
        
        /// <summary>
        /// Creates a structure with mixed types for DOS testing
        /// </summary>
        public string CreateNestedMixedTypesStructure(int maxNest = 20)
        {
            JToken structure = CreateNestedMixedTypesToken(maxNest);
            return structure.ToString();
        }
        
        /// <summary>
        /// Creates a nested structure with mixed types
        /// </summary>
        private JToken CreateNestedMixedTypesToken(int depth)
        {
            if (depth <= 0)
            {
                // Return a random primitive value
                int valueType = _random.Next(4);
                return valueType switch
                {
                    0 => _stringMutations.GenerateRandomString(1, 10),
                    1 => _random.NextDouble() * 100,
                    2 => _random.Next(2) == 0,
                    _ => JToken.Null
                };
            }
            
            // Alternate between objects and arrays
            if (depth % 2 == 0)
            {
                JObject obj = new JObject();
                
                // Add properties with different types
                int propertyCount = _random.Next(2, 5);
                for (int i = 0; i < propertyCount; i++)
                {
                    string key = $"prop{i}";
                    obj[key] = CreateNestedMixedTypesToken(depth - 1);
                }
                
                return obj;
            }
            else
            {
                JArray array = new JArray();
                
                // Add elements with different types
                int elementCount = _random.Next(2, 5);
                for (int i = 0; i < elementCount; i++)
                {
                    array.Add(CreateNestedMixedTypesToken(depth - 1));
                }
                
                return array;
            }
        }
        
        /// <summary>
        /// Creates a structure with repeated patterns for DOS testing
        /// </summary>
        public string CreateRepeatedPatternStructure(int repetitions = 100)
        {
            // Create a pattern for repetition
            string pattern = CreatePatternForRepetition();
            
            // Repeat the pattern
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
        /// Creates a structure with nesting exactly at the limit
        /// </summary>
        public string CreateExactLimitNestedStructure(int limit = BaseMutationEngine.NEO_DEFAULT_MAX_NEST)
        {
            JToken structure = _structureMutations.CreateNestedStructure(limit);
            return structure.ToString();
        }
        
        /// <summary>
        /// Creates a structure with nesting slightly above the limit
        /// </summary>
        public string CreateAboveLimitNestedStructure(int limit = BaseMutationEngine.NEO_DEFAULT_MAX_NEST)
        {
            JToken structure = _structureMutations.CreateNestedStructure(limit + 1);
            return structure.ToString();
        }
        
        /// <summary>
        /// Creates a structure with a large number of properties
        /// </summary>
        public string CreateLargePropertyCountStructure(int propertyCount = 1000)
        {
            JObject obj = new JObject();
            
            for (int i = 0; i < propertyCount; i++)
            {
                obj["prop" + i] = i;
            }
            
            return obj.ToString();
        }
        
        /// <summary>
        /// Creates a structure with a large number of array elements
        /// </summary>
        public string CreateLargeArrayStructure(int elementCount = 1000)
        {
            JArray array = new JArray();
            
            for (int i = 0; i < elementCount; i++)
            {
                array.Add(i);
            }
            
            return array.ToString();
        }
        
        /// <summary>
        /// Creates a structure with very long property names
        /// </summary>
        public string CreateLongPropertyNamesStructure(int nameLength = 1000, int propertyCount = 10)
        {
            JObject obj = new JObject();
            
            for (int i = 0; i < propertyCount; i++)
            {
                string propertyName = _stringMutations.GenerateAlphanumericString(nameLength);
                obj[propertyName] = i;
            }
            
            return obj.ToString();
        }
        
        /// <summary>
        /// Creates a structure with very long string values
        /// </summary>
        public string CreateLongStringValuesStructure(int valueLength = 10000, int propertyCount = 10)
        {
            JObject obj = new JObject();
            
            for (int i = 0; i < propertyCount; i++)
            {
                string value = _stringMutations.GenerateRandomString(valueLength);
                obj["prop" + i] = value;
            }
            
            return obj.ToString();
        }
        
        /// <summary>
        /// Creates a structure with a branching factor that increases exponentially
        /// </summary>
        public string CreateExponentialBranchingStructure(int depth = 5)
        {
            JToken structure = CreateExponentialBranchingRecursive(depth, 2);
            return structure.ToString();
        }
        
        /// <summary>
        /// Recursively creates a structure with exponential branching
        /// </summary>
        private JToken CreateExponentialBranchingRecursive(int depth, int branchFactor)
        {
            if (depth <= 0)
            {
                return _random.Next(100);
            }
            
            JObject obj = new JObject();
            
            for (int i = 0; i < branchFactor; i++)
            {
                obj["branch" + i] = CreateExponentialBranchingRecursive(depth - 1, branchFactor * 2);
            }
            
            return obj;
        }
        
        /// <summary>
        /// Creates a structure with Unicode characters that may be expensive to process
        /// </summary>
        public string CreateUnicodeHeavyStructure(int propertyCount = 10, int stringLength = 100)
        {
            JObject obj = new JObject();
            
            for (int i = 0; i < propertyCount; i++)
            {
                string value = _stringMutations.GenerateUnicodeString(stringLength);
                obj["prop" + i] = value;
            }
            
            return obj.ToString();
        }
        
        /// <summary>
        /// Creates a structure with escape sequences that may be expensive to process
        /// </summary>
        public string CreateEscapeSequenceHeavyStructure(int propertyCount = 10, int stringLength = 100)
        {
            JObject obj = new JObject();
            
            for (int i = 0; i < propertyCount; i++)
            {
                string value = _stringMutations.GenerateJsonEscapeSequenceString(stringLength);
                obj["prop" + i] = value;
            }
            
            return obj.ToString();
        }
        
        /// <summary>
        /// Creates a structure with a mix of all DOS vector techniques
        /// </summary>
        public string CreateCompositeDosVector()
        {
            JObject obj = new JObject();
            
            // Add a deeply nested structure
            obj["nested"] = JToken.Parse(CreateDeeplyNestedStructure(20));
            
            // Add a structure with alternating types
            obj["alternating"] = JToken.Parse(CreateAlternatingTypesStructure(10));
            
            // Add a structure with repeated patterns
            obj["repeated"] = JToken.Parse(CreateRepeatedPatternStructure(50));
            
            // Add a structure with a large number of properties
            obj["large_properties"] = JToken.Parse(CreateLargePropertyCountStructure(100));
            
            // Add a structure with long string values
            obj["long_strings"] = JToken.Parse(CreateLongStringValuesStructure(1000, 3));
            
            return obj.ToString();
        }
        
        /// <summary>
        /// Applies a DOS vector mutation to the JSON
        /// </summary>
        /// <param name="json">The JSON string to mutate</param>
        /// <returns>A mutated JSON string designed to test DOS resistance</returns>
        public string ApplyDOSVectorMutation(string json)
        {
            try
            {
                // Select a mutation strategy
                int strategy = _random.Next(5);
                
                return strategy switch
                {
                    0 => CreateDeepNesting(json),
                    1 => CreateLargeInput(json),
                    2 => CreateComplexObject(json),
                    3 => CreateBacktrackingRegex(json),
                    _ => CreateMinimalDosVector()
                };
            }
            catch (Exception)
            {
                // Fallback to a simple DOS vector if mutation fails
                return CreateMinimalDosVector();
            }
        }
        
        /// <summary>
        /// Creates a deeply nested JSON structure
        /// </summary>
        private string CreateDeepNesting(string json)
        {
            try
            {
                // Create a deeply nested array or object
                int nestingType = _random.Next(2);
                int depth = _random.Next(50, 200);
                
                if (nestingType == 0)
                {
                    // Create a deeply nested array
                    JArray root = new JArray();
                    JArray current = root;
                    
                    for (int i = 0; i < depth; i++)
                    {
                        JArray next = new JArray();
                        current.Add(next);
                        current = next;
                    }
                    
                    // Add a final value
                    current.Add(_random.Next(100));
                    
                    return root.ToString();
                }
                else
                {
                    // Create a deeply nested object
                    JObject root = new JObject();
                    JObject current = root;
                    
                    for (int i = 0; i < depth; i++)
                    {
                        JObject next = new JObject();
                        current[$"level_{i}"] = next;
                        current = next;
                    }
                    
                    // Add a final value
                    current["value"] = _random.Next(100);
                    
                    return root.ToString();
                }
            }
            catch (Exception)
            {
                // Fallback to the original JSON if an error occurs
                return json;
            }
        }
        
        /// <summary>
        /// Creates a very large JSON input
        /// </summary>
        private string CreateLargeInput(string json)
        {
            try
            {
                // Create a large array or object
                int structureType = _random.Next(2);
                int size = _random.Next(1000, 10000);
                
                if (structureType == 0)
                {
                    // Create a large array
                    JArray array = new JArray();
                    
                    for (int i = 0; i < size; i++)
                    {
                        JObject item = new JObject();
                        item["id"] = i;
                        item["value"] = _random.Next(10000);
                        item["name"] = $"Item_{i}";
                        array.Add(item);
                    }
                    
                    return array.ToString();
                }
                else
                {
                    // Create a large object
                    JObject obj = new JObject();
                    
                    for (int i = 0; i < size; i++)
                    {
                        obj[$"key_{i}"] = _random.Next(10000);
                    }
                    
                    return obj.ToString();
                }
            }
            catch (Exception)
            {
                // Fallback to the original JSON if an error occurs
                return json;
            }
        }
        
        /// <summary>
        /// Creates a complex object with many nested properties
        /// </summary>
        private string CreateComplexObject(string json)
        {
            try
            {
                JObject root = new JObject();
                
                // Add a variety of property types
                root["id"] = _random.Next(1000);
                root["name"] = $"Complex_Object_{_random.Next(100)}";
                root["created"] = DateTime.UtcNow.ToString("o");
                
                // Add a nested array
                JArray array = new JArray();
                int arraySize = _random.Next(50, 200);
                
                for (int i = 0; i < arraySize; i++)
                {
                    JObject item = new JObject();
                    item["index"] = i;
                    
                    // Add nested properties
                    JObject nestedProps = new JObject();
                    int propCount = _random.Next(5, 20);
                    
                    for (int j = 0; j < propCount; j++)
                    {
                        nestedProps[$"prop_{j}"] = $"value_{j}_{_random.Next(100)}";
                    }
                    
                    item["properties"] = nestedProps;
                    array.Add(item);
                }
                
                root["items"] = array;
                
                // Add deeply nested objects
                JObject current = root;
                int depth = _random.Next(10, 30);
                
                for (int i = 0; i < depth; i++)
                {
                    JObject nested = new JObject();
                    nested["depth"] = i;
                    nested["value"] = _random.Next(1000);
                    
                    current["nested"] = nested;
                    current = nested;
                }
                
                return root.ToString();
            }
            catch (Exception)
            {
                // Fallback to the original JSON if an error occurs
                return json;
            }
        }
        
        /// <summary>
        /// Creates JSON with strings that could trigger backtracking in regex
        /// </summary>
        private string CreateBacktrackingRegex(string json)
        {
            try
            {
                JObject root = new JObject();
                
                // Create strings that might cause regex engines to backtrack heavily
                string[] backtrackingPatterns = new[]
                {
                    new string('a', 100) + new string('b', 100),
                    string.Join("", Enumerable.Repeat("a?", 100)) + new string('a', 100),
                    string.Join("", Enumerable.Repeat("(a|b)", 20)),
                    new string('a', 50) + new string('b', 50) + new string('c', 50),
                    string.Concat(Enumerable.Repeat("\\u0000", 100))
                };
                
                // Add these patterns to the JSON
                for (int i = 0; i < backtrackingPatterns.Length; i++)
                {
                    root[$"pattern_{i}"] = backtrackingPatterns[i];
                }
                
                // Create an array of problematic strings
                JArray array = new JArray();
                for (int i = 0; i < 20; i++)
                {
                    array.Add(backtrackingPatterns[_random.Next(backtrackingPatterns.Length)]);
                }
                
                root["patterns"] = array;
                
                return root.ToString();
            }
            catch (Exception)
            {
                // Fallback to the original JSON if an error occurs
                return json;
            }
        }
    }
}
