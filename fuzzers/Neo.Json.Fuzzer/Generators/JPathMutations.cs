// Copyright (C) 2015-2025 The Neo Project.
//
// JPathMutations.cs file belongs to the neo project and is free
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
    /// Provides JPath-specific mutation strategies for the mutation engine
    /// </summary>
    public class JPathMutations
    {
        private readonly BaseMutationEngine _engine;
        private readonly Random _random;
        private readonly StringMutations _stringMutations;
        
        // JPath operators and syntax elements
        private static readonly string[] _jPathOperators = new[] { ".", "..", "*", "[", "]", "?", "@", "$", "(", ")", "==", "!=", ">", "<", ">=", "<=", "&&", "||" };
        
        /// <summary>
        /// Initializes a new instance of the JPathMutations class
        /// </summary>
        public JPathMutations(BaseMutationEngine engine, Random random)
        {
            _engine = engine ?? throw new ArgumentNullException(nameof(engine));
            _random = random ?? throw new ArgumentNullException(nameof(random));
            _stringMutations = new StringMutations(engine, random);
        }
        
        /// <summary>
        /// Creates a JSON structure specifically designed to test JPath functionality
        /// </summary>
        public JToken CreateJPathTestStructure()
        {
            int structureType = _random.Next(5);
            
            return structureType switch
            {
                0 => CreateDeeplyNestedObject(5),
                1 => CreateComplexArray(10),
                2 => CreateMixedStructure(),
                3 => CreateStructureWithSpecialPropertyNames(),
                _ => CreateLargeStructure()
            };
        }
        
        /// <summary>
        /// Creates a deeply nested object for testing recursive descent
        /// </summary>
        private JObject CreateDeeplyNestedObject(int depth)
        {
            if (depth <= 0)
            {
                JObject leaf = new JObject();
                leaf["value"] = new JNumber(_random.Next(100));
                leaf["name"] = new JString(_stringMutations.GenerateRandomString(5));
                return leaf;
            }
            
            JObject obj = new JObject();
            obj["level"] = new JNumber(depth);
            obj["name"] = new JString($"level_{depth}");
            obj["child"] = CreateDeeplyNestedObject(depth - 1);
            
            // Add some properties at this level
            obj["property_" + _random.Next(100)] = new JNumber(_random.Next(1000));
            obj["flag_" + _random.Next(100)] = new JBoolean(_random.Next(2) == 0);
            
            return obj;
        }
        
        /// <summary>
        /// Creates a complex array with mixed types and nested structures
        /// </summary>
        private JArray CreateComplexArray(int size)
        {
            JArray array = new JArray();
            
            for (int i = 0; i < size; i++)
            {
                int elementType = _random.Next(5);
                
                switch (elementType)
                {
                    case 0:
                        array.Add(new JNumber(_random.Next(100)));
                        break;
                    case 1:
                        array.Add(new JString(_stringMutations.GenerateRandomString(8)));
                        break;
                    case 2:
                        array.Add(new JBoolean(_random.Next(2) == 0));
                        break;
                    case 3:
                        JObject obj = new JObject();
                        obj["index"] = new JNumber(i);
                        obj["value"] = new JNumber(_random.Next(1000));
                        array.Add(obj);
                        break;
                    case 4:
                        JArray nestedArray = new JArray();
                        for (int j = 0; j < 3; j++)
                        {
                            nestedArray.Add(new JNumber(i * 10 + j));
                        }
                        array.Add(nestedArray);
                        break;
                }
            }
            
            return array;
        }
        
        /// <summary>
        /// Creates a mixed structure with both objects and arrays
        /// </summary>
        private JToken CreateMixedStructure()
        {
            JObject root = new JObject();
            
            // Add simple properties
            root["name"] = new JString("JPath Test Structure");
            root["created"] = new JString(DateTime.Now.ToString("o"));
            root["version"] = new JNumber(1.0);
            
            // Add an array of objects
            JArray items = new JArray();
            for (int i = 0; i < 5; i++)
            {
                JObject item = new JObject();
                item["id"] = new JNumber(i);
                item["name"] = new JString($"Item {i}");
                item["active"] = new JBoolean(i % 2 == 0);
                
                // Add nested properties for filter testing
                JObject metadata = new JObject();
                metadata["created"] = new JString(DateTime.Now.AddDays(-i).ToString("o"));
                metadata["priority"] = new JNumber(i % 3);
                JArray tags = new JArray();
                tags.Add(new JString("tag1"));
                tags.Add(new JString("tag2"));
                tags.Add(new JString($"tag{i}"));
                metadata["tags"] = tags;
                item["metadata"] = metadata;
                
                items.Add(item);
            }
            root["items"] = items;
            
            // Add a deeply nested path
            JObject current = root;
            for (int i = 0; i < 5; i++)
            {
                JObject next = new JObject();
                next["level"] = new JNumber(i);
                next["value"] = new JNumber(_random.Next(100));
                current["next"] = next;
                current = next;
            }
            
            return root;
        }
        
        /// <summary>
        /// Creates a structure with special property names
        /// </summary>
        private JObject CreateStructureWithSpecialPropertyNames()
        {
            JObject obj = new JObject();
            
            // Property with spaces
            obj["property with spaces"] = new JString("requires ['property with spaces'] syntax");
            
            // Property with quotes
            obj["property\"with\"quotes"] = new JString("requires escaping");
            
            // Property with special characters
            obj["property!@#$%^&*()"] = new JString("special characters");
            
            // Numeric property name
            obj["123"] = new JString("numeric property name");
            
            // Empty property name
            obj[""] = new JString("empty property name");
            
            // Unicode property name
            obj["unicode\u00A9property"] = new JString("unicode in property name");
            
            // Very long property name
            StringBuilder longName = new StringBuilder();
            for (int i = 0; i < 50; i++)
            {
                longName.Append("long");
            }
            obj[longName.ToString()] = new JString("very long property name");
            
            // Nested objects with special names
            JObject nested = new JObject();
            nested["normal"] = new JString("normal property");
            nested["special!"] = new JString("special property");
            obj["nested"] = nested;
            
            return obj;
        }
        
        /// <summary>
        /// Creates a large structure for testing complex JPath queries
        /// </summary>
        private JObject CreateLargeStructure()
        {
            JObject root = new JObject();
            
            // Add a large array of objects
            JArray items = new JArray();
            int itemCount = _random.Next(20, 50);
            
            for (int i = 0; i < itemCount; i++)
            {
                JObject item = new JObject();
                item["id"] = new JNumber(i);
                item["name"] = new JString($"Item {i}");
                item["category"] = new JString(GetRandomCategory());
                item["price"] = new JNumber(_random.Next(1, 1000) / 10.0);
                item["inStock"] = new JBoolean(_random.Next(2) == 0);
                
                // Add tags array
                JArray tags = new JArray();
                int tagCount = _random.Next(1, 5);
                for (int j = 0; j < tagCount; j++)
                {
                    tags.Add(new JString(GetRandomTag()));
                }
                item["tags"] = tags;
                
                // Add nested attributes
                JObject attributes = new JObject();
                int attrCount = _random.Next(2, 6);
                for (int j = 0; j < attrCount; j++)
                {
                    attributes[GetRandomAttributeName()] = new JString(GetRandomAttributeValue());
                }
                item["attributes"] = attributes;
                
                items.Add(item);
            }
            
            root["items"] = items;
            
            // Add metadata
            JObject metadata = new JObject();
            metadata["totalCount"] = new JNumber(itemCount);
            metadata["lastUpdated"] = new JString(DateTime.Now.ToString("o"));
            metadata["version"] = new JString("1.0");
            root["metadata"] = metadata;
            
            // Add configuration
            JObject config = new JObject();
            config["pageSize"] = new JNumber(_random.Next(10, 50));
            config["sortBy"] = new JString(GetRandomSortField());
            config["sortDirection"] = new JString(_random.Next(2) == 0 ? "asc" : "desc");
            root["config"] = config;
            
            return root;
        }
        
        /// <summary>
        /// Applies a JPath-specific mutation to the JSON
        /// </summary>
        public string ApplyJPathMutation(string json)
        {
            try
            {
                var token = JToken.Parse(json);
                
                // Select a mutation strategy
                int strategy = _random.Next(5);
                
                // Add null check before using token
                if (token == null)
                {
                    return CreateJPathTestStructure().ToString();
                }
                
                return strategy switch
                {
                    0 => AddJPathTestableStructure(token),
                    1 => AddFilterableProperties(token),
                    2 => AddArraysForSlicing(token),
                    3 => AddNestedPropertiesForRecursion(token),
                    _ => AddPropertiesWithSpecialNames(token)
                };
            }
            catch
            {
                // If parsing fails, return the original JSON
                return json;
            }
        }
        
        /// <summary>
        /// Adds a structure that can be tested with JPath queries
        /// </summary>
        private string AddJPathTestableStructure(JToken token)
        {
            if (token == null) return "{}";
            
            if (token is JObject obj)
            {
                // Add a JPath-testable structure to the object
                obj["jpath_test"] = CreateJPathTestStructure();
                return obj.ToString();
            }
            else if (token is JArray array && array.Count > 0)
            {
                // Add a JPath-testable structure to the first object in the array
                for (int i = 0; i < array.Count; i++)
                {
                    if (array[i] is JObject itemObj)
                    {
                        itemObj["jpath_test"] = CreateJPathTestStructure();
                        break;
                    }
                }
                return array.ToString();
            }
            
            // If token is not an object or array, wrap it in an object
            JObject wrapper = new JObject();
            wrapper["original"] = token;
            wrapper["jpath_test"] = CreateJPathTestStructure();
            return wrapper.ToString();
        }
        
        /// <summary>
        /// Adds properties that can be filtered with JPath expressions
        /// </summary>
        private string AddFilterableProperties(JToken token)
        {
            if (token == null) return "{}";
            
            if (token is JObject obj)
            {
                // Add filterable properties
                obj["filterable"] = CreateFilterableObject();
                return obj.ToString();
            }
            else if (token is JArray array && array.Count > 0)
            {
                // Add filterable properties to array items
                for (int i = 0; i < array.Count; i++)
                {
                    if (array[i] is JObject itemObj)
                    {
                        itemObj["filterable"] = CreateFilterableObject();
                    }
                }
                return array.ToString();
            }
            
            // If token is not an object or array, wrap it in an object
            JObject wrapper = new JObject();
            wrapper["original"] = token;
            wrapper["filterable"] = CreateFilterableObject();
            return wrapper.ToString();
        }
        
        /// <summary>
        /// Creates an object with properties that can be filtered
        /// </summary>
        private JObject CreateFilterableObject()
        {
            JObject obj = new JObject();
            
            // Add numeric properties for comparison operators
            obj["id"] = new JNumber(_random.Next(1000));
            obj["price"] = new JNumber(_random.Next(1, 10000) / 100.0);
            obj["quantity"] = new JNumber(_random.Next(100));
            
            // Add string properties for equality and pattern matching
            obj["name"] = new JString(_stringMutations.GenerateRandomString(8));
            obj["category"] = new JString(GetRandomCategory());
            obj["code"] = new JString($"CODE-{_random.Next(1000):D4}");
            
            // Add boolean properties
            obj["active"] = new JBoolean(_random.Next(2) == 0);
            obj["featured"] = new JBoolean(_random.Next(2) == 0);
            obj["inStock"] = new JBoolean(_random.Next(2) == 0);
            
            // Add date properties
            obj["created"] = new JString(DateTime.Now.AddDays(-_random.Next(100)).ToString("o"));
            obj["updated"] = new JString(DateTime.Now.AddDays(-_random.Next(10)).ToString("o"));
            
            return obj;
        }
        
        /// <summary>
        /// Adds arrays that can be sliced with JPath expressions
        /// </summary>
        private string AddArraysForSlicing(JToken token)
        {
            if (token == null) return "{}";
            
            if (token is JObject obj)
            {
                // Add arrays for slicing
                obj["sliceable"] = CreateSliceableArrays();
                return obj.ToString();
            }
            else if (token is JArray array && array.Count > 0)
            {
                // If it's already an array, make it more sliceable
                return EnhanceArrayForSlicing(array).ToString();
            }
            
            // If token is not an object or array, wrap it in an object
            JObject wrapper = new JObject();
            wrapper["original"] = token;
            wrapper["sliceable"] = CreateSliceableArrays();
            return wrapper.ToString();
        }
        
        /// <summary>
        /// Creates arrays that can be sliced with JPath expressions
        /// </summary>
        private JObject CreateSliceableArrays()
        {
            JObject obj = new JObject();
            
            // Add a simple numeric array
            JArray numbers = new JArray();
            int numCount = _random.Next(20, 50);
            for (int i = 0; i < numCount; i++)
            {
                numbers.Add(new JNumber(i));
            }
            obj["numbers"] = numbers;
            
            // Add an array of objects
            JArray items = new JArray();
            int itemCount = _random.Next(10, 30);
            for (int i = 0; i < itemCount; i++)
            {
                JObject item = new JObject();
                item["id"] = new JNumber(i);
                item["value"] = new JNumber(_random.Next(1000));
                items.Add(item);
            }
            obj["items"] = items;
            
            // Add a jagged array
            JArray jagged = new JArray();
            int outerCount = _random.Next(5, 10);
            for (int i = 0; i < outerCount; i++)
            {
                JArray inner = new JArray();
                int innerCount = _random.Next(3, 8);
                for (int j = 0; j < innerCount; j++)
                {
                    inner.Add(new JNumber(i * 10 + j));
                }
                jagged.Add(inner);
            }
            obj["jagged"] = jagged;
            
            return obj;
        }
        
        /// <summary>
        /// Enhances an array to make it more suitable for slicing tests
        /// </summary>
        private JArray EnhanceArrayForSlicing(JArray array)
        {
            // Ensure the array has enough elements
            int targetSize = Math.Max(array.Count, 20);
            
            while (array.Count < targetSize)
            {
                int elementType = _random.Next(4);
                
                switch (elementType)
                {
                    case 0:
                        array.Add(new JNumber(_random.Next(1000)));
                        break;
                    case 1:
                        array.Add(new JString(_stringMutations.GenerateRandomString(8)));
                        break;
                    case 2:
                        array.Add(new JBoolean(_random.Next(2) == 0));
                        break;
                    case 3:
                        JObject obj = new JObject();
                        obj["id"] = new JNumber(array.Count);
                        obj["value"] = new JString($"Item {array.Count}");
                        array.Add(obj);
                        break;
                }
            }
            
            return array;
        }
        
        /// <summary>
        /// Adds nested properties for testing recursive descent
        /// </summary>
        private string AddNestedPropertiesForRecursion(JToken token)
        {
            if (token == null) return "{}";
            
            if (token is JObject obj)
            {
                // Add a deeply nested structure
                obj["nested"] = CreateDeeplyNestedStructure(5);
                return obj.ToString();
            }
            else if (token is JArray array && array.Count > 0)
            {
                // Add nested structures to array items
                for (int i = 0; i < array.Count; i++)
                {
                    if (array[i] is JObject itemObj)
                    {
                        itemObj["nested"] = CreateDeeplyNestedStructure(_random.Next(3, 6));
                    }
                }
                return array.ToString();
            }
            
            // If token is not an object or array, wrap it in an object
            JObject wrapper = new JObject();
            wrapper["original"] = token;
            wrapper["nested"] = CreateDeeplyNestedStructure(5);
            return wrapper.ToString();
        }
        
        /// <summary>
        /// Creates a deeply nested structure for testing recursive descent
        /// </summary>
        private JObject CreateDeeplyNestedStructure(int depth)
        {
            JObject root = new JObject();
            
            // Create a chain of nested objects with the same property name
            JObject current = root;
            for (int i = 0; i < depth; i++)
            {
                current["data"] = new JObject();
                JToken? dataToken = current["data"];
                if (dataToken is JObject dataObj)
                {
                    current = dataObj;
                    current["level"] = new JNumber(i);
                    current["value"] = new JString($"Level {i} value");
                }
                else
                {
                    // If for some reason the data token is not a JObject, break the loop
                    break;
                }
            }
            
            // Add a leaf value
            current["data"] = new JString("Leaf node");
            
            return root;
        }
        
        /// <summary>
        /// Adds properties with special names that require specific JPath syntax
        /// </summary>
        private string AddPropertiesWithSpecialNames(JToken token)
        {
            if (token == null) return "{}";
            
            if (token is JObject obj)
            {
                // Add properties with special names
                obj["special_names"] = CreateObjectWithSpecialNames();
                return obj.ToString();
            }
            else if (token is JArray array && array.Count > 0)
            {
                // Add special names to array items
                for (int i = 0; i < array.Count; i++)
                {
                    if (array[i] is JObject itemObj)
                    {
                        itemObj["special_names"] = CreateObjectWithSpecialNames();
                    }
                }
                return array.ToString();
            }
            
            // If token is not an object or array, wrap it in an object
            JObject wrapper = new JObject();
            wrapper["original"] = token;
            wrapper["special_names"] = CreateObjectWithSpecialNames();
            return wrapper.ToString();
        }
        
        /// <summary>
        /// Creates an object with property names that require special JPath syntax
        /// </summary>
        private JObject CreateObjectWithSpecialNames()
        {
            JObject obj = new JObject();
            
            // Property with spaces
            obj["name with spaces"] = new JString("Requires ['name with spaces'] syntax");
            
            // Property with special characters
            obj["name!@#"] = new JString("Requires ['name!@#'] syntax");
            
            // Property with quotes
            obj["name\"with\"quotes"] = new JString("Requires escaping");
            
            // Property that starts with a number
            obj["123name"] = new JString("Starts with a number");
            
            // Property with a JPath operator in the name
            obj["name.with.dots"] = new JString("Contains dots");
            obj["name[with]brackets"] = new JString("Contains brackets");
            
            return obj;
        }
        
        /// <summary>
        /// Gets a random category name
        /// </summary>
        private string GetRandomCategory()
        {
            string[] categories = { "Electronics", "Books", "Clothing", "Food", "Toys", "Sports", "Home", "Garden", "Automotive", "Health" };
            return categories[_random.Next(categories.Length)];
        }
        
        /// <summary>
        /// Gets a random tag
        /// </summary>
        private string GetRandomTag()
        {
            string[] tags = { "new", "sale", "featured", "popular", "limited", "exclusive", "clearance", "premium", "best-seller", "eco-friendly" };
            return tags[_random.Next(tags.Length)];
        }
        
        /// <summary>
        /// Gets a random attribute name
        /// </summary>
        private string GetRandomAttributeName()
        {
            string[] attributes = { "color", "size", "weight", "material", "origin", "brand", "model", "year", "style", "finish" };
            return attributes[_random.Next(attributes.Length)];
        }
        
        /// <summary>
        /// Gets a random attribute value
        /// </summary>
        private string GetRandomAttributeValue()
        {
            string[] values = { "red", "large", "heavy", "cotton", "imported", "premium", "deluxe", "standard", "modern", "glossy" };
            return values[_random.Next(values.Length)];
        }
        
        /// <summary>
        /// Gets a random sort field
        /// </summary>
        private string GetRandomSortField()
        {
            string[] fields = { "id", "name", "price", "category", "created", "popularity" };
            return fields[_random.Next(fields.Length)];
        }
        
        /// <summary>
        /// Generates a specialized test JSON for JPath testing
        /// </summary>
        public string GenerateSpecializedTestJson(string? testType = null)
        {
            JToken structure = testType?.ToLowerInvariant() switch
            {
                "nested" => CreateDeeplyNestedObject(8),
                "array" => CreateComplexArray(20),
                "mixed" => CreateMixedStructure(),
                "special_names" => CreateStructureWithSpecialPropertyNames(),
                "large" => CreateLargeStructure(),
                "filterable" => CreateFilterableObject(),
                "sliceable" => CreateSliceableArrays(),
                "recursive" => CreateDeeplyNestedStructure(10),
                _ => CreateJPathTestStructure() // Default
            };
            
            return structure.ToString();
        }
    }
}
