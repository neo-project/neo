// Copyright (C) 2015-2025 The Neo Project.
//
// StructureMutations.cs file belongs to the neo project and is free
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
    /// Provides structure-related mutation strategies for the mutation engine
    /// </summary>
    public class StructureMutations
    {
        private readonly BaseMutationEngine _engine;
        private readonly Random _random;
        private readonly StringMutations _stringMutations;
        
        /// <summary>
        /// Initializes a new instance of the StructureMutations class
        /// </summary>
        public StructureMutations(BaseMutationEngine engine, Random random)
        {
            _engine = engine ?? throw new ArgumentNullException(nameof(engine));
            _random = random ?? throw new ArgumentNullException(nameof(random));
            _stringMutations = new StringMutations(engine, random);
        }
        
        /// <summary>
        /// Applies a random structure mutation to the JSON
        /// </summary>
        public string ApplyRandomMutation(string json)
        {
            try
            {
                var token = JToken.Parse(json);
                if (token == null)
                {
                    return json;
                }
                
                int strategy = _random.Next(10);
                
                switch (strategy)
                {
                    case 0:
                        AddRandomProperty(token);
                        break;
                    case 1:
                        RemoveRandomProperty(token);
                        break;
                    case 2:
                        RenameRandomProperty(token);
                        break;
                    case 3:
                        DuplicateRandomProperty(token);
                        break;
                    case 4:
                        AddRandomArrayElement(token);
                        break;
                    case 5:
                        RemoveRandomArrayElement(token);
                        break;
                    case 6:
                        SwapRandomElements(token);
                        break;
                    case 7:
                        NestRandomProperty(token);
                        break;
                    case 8:
                        UnnestRandomProperty(token);
                        break;
                    case 9:
                        ConvertBetweenArrayAndObject(token);
                        break;
                }
                
                return token.ToString();
            }
            catch
            {
                // If parsing fails, return the original JSON
                return json;
            }
        }
        
        /// <summary>
        /// Adds a random property to a JSON object
        /// </summary>
        public void AddRandomProperty(JToken token)
        {
            if (token == null)
                return;
                
            if (token is JObject obj)
            {
                // Generate a random property name
                string propertyName = _engine.GenerateRandomPropertyName();
                
                // Generate a random value
                JToken value = _engine.GenerateRandomValue();
                
                // Add the property
                obj[propertyName] = value;
            }
            else if (token is JArray array)
            {
                // Recursively process a random array element
                if (array.Count > 0)
                {
                    int index = _random.Next(array.Count);
                    JToken? element = array[index];
                    if (element != null)
                    {
                        AddRandomProperty(element);
                    }
                }
            }
        }
        
        /// <summary>
        /// Removes a random property from a JSON object
        /// </summary>
        public void RemoveRandomProperty(JToken token)
        {
            if (token == null)
                return;
                
            if (token is JObject obj)
            {
                // Get all properties
                var properties = obj.Properties.ToList();
                
                // Remove a random property if any exist
                if (properties.Count > 0)
                {
                    int index = _random.Next(properties.Count);
                    var property = properties[index];
                    JToken? removedValue;
                    ((IDictionary<string, JToken?>)obj).Remove(property.Key, out removedValue);
                }
            }
            else if (token is JArray array)
            {
                // Recursively process a random array element
                if (array.Count > 0)
                {
                    int index = _random.Next(array.Count);
                    JToken? element = array[index];
                    if (element != null)
                    {
                        RemoveRandomProperty(element);
                    }
                }
            }
        }
        
        /// <summary>
        /// Renames a random property in a JSON object
        /// </summary>
        public void RenameRandomProperty(JToken token)
        {
            if (token == null)
                return;
                
            if (token is JObject obj)
            {
                // Get all properties
                var properties = obj.Properties.ToList();
                
                // Rename a random property if any exist
                if (properties.Count > 0)
                {
                    int index = _random.Next(properties.Count);
                    var property = properties[index];
                    
                    // Generate a new name
                    string newName = _engine.GenerateRandomPropertyName();
                    
                    // Clone the value
                    JToken? value = property.Value;
                    if (value != null)
                    {
                        // Add with new name and remove old one
                        obj[newName] = CloneToken(value);
                        JToken? removedValue;
                        ((IDictionary<string, JToken?>)obj).Remove(property.Key, out removedValue);
                    }
                }
            }
            else if (token is JArray array)
            {
                // Recursively process a random array element
                if (array.Count > 0)
                {
                    int index = _random.Next(array.Count);
                    JToken? element = array[index];
                    if (element != null)
                    {
                        RenameRandomProperty(element);
                    }
                }
            }
        }
        
        /// <summary>
        /// Duplicates a random property in a JSON object
        /// </summary>
        public void DuplicateRandomProperty(JToken token)
        {
            if (token == null)
                return;
                
            if (token is JObject obj)
            {
                // Get all properties
                var properties = obj.Properties.ToList();
                
                // Duplicate a random property if any exist
                if (properties.Count > 0)
                {
                    int index = _random.Next(properties.Count);
                    var property = properties[index];
                    
                    // Generate a new name
                    string newName = property.Key + "_copy";
                    
                    // Clone the value
                    JToken? value = property.Value;
                    if (value != null)
                    {
                        obj[newName] = CloneToken(value);
                    }
                }
            }
            else if (token is JArray array)
            {
                // Recursively process a random array element
                if (array.Count > 0)
                {
                    int index = _random.Next(array.Count);
                    JToken? element = array[index];
                    if (element != null)
                    {
                        DuplicateRandomProperty(element);
                    }
                }
            }
        }
        
        /// <summary>
        /// Adds a random element to a JSON array
        /// </summary>
        public void AddRandomArrayElement(JToken token)
        {
            if (token == null)
                return;
                
            if (token is JArray array)
            {
                // Generate a random value
                JToken value = _engine.GenerateRandomValue();
                
                // Add to the array
                array.Add(value);
            }
            else if (token is JObject obj)
            {
                // Find an array property
                var arrayProperties = new List<(string Key, JArray ArrayValue)>();
                foreach (var kvp in obj.Properties)
                {
                    if (kvp.Value is JArray arr)
                    {
                        arrayProperties.Add((kvp.Key, arr));
                    }
                }
                
                // Add to a random array property if any exist
                if (arrayProperties.Count > 0)
                {
                    int index = _random.Next(arrayProperties.Count);
                    var (_, arrayValue) = arrayProperties[index];
                    
                    // Generate a random value
                    JToken value = _engine.GenerateRandomValue();
                    
                    // Add to the array
                    arrayValue.Add(value);
                }
            }
        }
        
        /// <summary>
        /// Removes a random element from a JSON array
        /// </summary>
        public void RemoveRandomArrayElement(JToken token)
        {
            if (token == null)
                return;
                
            if (token is JArray array)
            {
                // Remove a random element if any exist
                if (array.Count > 0)
                {
                    int index = _random.Next(array.Count);
                    array.RemoveAt(index);
                }
            }
            else if (token is JObject obj)
            {
                // Find an array property
                var arrayProperties = new List<(string Key, JArray ArrayValue)>();
                foreach (var kvp in obj.Properties)
                {
                    if (kvp.Value is JArray arr)
                    {
                        arrayProperties.Add((kvp.Key, arr));
                    }
                }
                
                // Remove from a random array property if any exist
                if (arrayProperties.Count > 0)
                {
                    int index = _random.Next(arrayProperties.Count);
                    var (_, arrayValue) = arrayProperties[index];
                    
                    // Remove a random element if any exist
                    if (arrayValue.Count > 0)
                    {
                        int elementIndex = _random.Next(arrayValue.Count);
                        arrayValue.RemoveAt(elementIndex);
                    }
                }
            }
        }
        
        /// <summary>
        /// Swaps two random elements in a JSON array
        /// </summary>
        public void SwapRandomElements(JToken token)
        {
            if (token == null)
                return;
                
            if (token is JArray array)
            {
                // Swap two random elements if at least two exist
                if (array.Count >= 2)
                {
                    int index1 = _random.Next(array.Count);
                    int index2 = _random.Next(array.Count);
                    
                    // Make sure indices are different
                    while (index1 == index2)
                    {
                        index2 = _random.Next(array.Count);
                    }
                    
                    // Swap elements
                    JToken? temp = array[index1];
                    array[index1] = array[index2];
                    array[index2] = temp;
                }
            }
            else if (token is JObject obj)
            {
                // Find an array property
                var arrayProperties = new List<(string Key, JArray ArrayValue)>();
                foreach (var kvp in obj.Properties)
                {
                    if (kvp.Value is JArray arr)
                    {
                        arrayProperties.Add((kvp.Key, arr));
                    }
                }
                
                // Swap in a random array property if any exist
                if (arrayProperties.Count > 0)
                {
                    int index = _random.Next(arrayProperties.Count);
                    var (_, arrayValue) = arrayProperties[index];
                    
                    // Swap two random elements if at least two exist
                    if (arrayValue.Count >= 2)
                    {
                        int index1 = _random.Next(arrayValue.Count);
                        int index2 = _random.Next(arrayValue.Count);
                        
                        // Make sure indices are different
                        while (index1 == index2)
                        {
                            index2 = _random.Next(arrayValue.Count);
                        }
                        
                        // Swap elements
                        JToken? temp = arrayValue[index1];
                        arrayValue[index1] = arrayValue[index2];
                        arrayValue[index2] = temp;
                    }
                }
            }
        }
        
        /// <summary>
        /// Nests a random property inside a new object
        /// </summary>
        public void NestRandomProperty(JToken token)
        {
            if (token == null)
                return;
                
            if (token is JObject obj)
            {
                // Get all properties
                var properties = obj.Properties.ToList();
                
                // Nest a random property if any exist
                if (properties.Count > 0)
                {
                    int index = _random.Next(properties.Count);
                    var property = properties[index];
                    
                    // Create a new object
                    JObject newObj = new JObject();
                    
                    // Move the property value to the new object
                    JToken? value = property.Value;
                    if (value != null)
                    {
                        newObj["nested"] = CloneToken(value);
                        
                        // Replace the original property with the new object
                        JToken? removedValue;
                        ((IDictionary<string, JToken?>)obj).Remove(property.Key, out removedValue);
                        obj[property.Key] = newObj;
                    }
                }
            }
            else if (token is JArray array)
            {
                // Recursively process a random array element
                if (array.Count > 0)
                {
                    int index = _random.Next(array.Count);
                    JToken? element = array[index];
                    if (element != null)
                    {
                        NestRandomProperty(element);
                    }
                }
            }
        }
        
        /// <summary>
        /// Unnests a random nested property
        /// </summary>
        public void UnnestRandomProperty(JToken token)
        {
            if (token == null)
                return;
                
            if (token is JObject obj)
            {
                // Find properties that contain objects
                var nestedProperties = new List<(string Key, JObject NestedObj)>();
                foreach (var kvp in obj.Properties)
                {
                    if (kvp.Value is JObject nestedObj)
                    {
                        nestedProperties.Add((kvp.Key, nestedObj));
                    }
                }
                
                // Unnest a random property if any exist
                if (nestedProperties.Count > 0)
                {
                    int index = _random.Next(nestedProperties.Count);
                    var (key, nestedObj) = nestedProperties[index];
                    
                    // If the nested object has properties, unnest the first one
                    if (nestedObj.Properties.Any())
                    {
                        var firstProp = nestedObj.Properties.First();
                        obj[key] = firstProp.Value;
                    }
                }
            }
            else if (token is JArray array)
            {
                // Recursively process a random array element
                if (array.Count > 0)
                {
                    int index = _random.Next(array.Count);
                    JToken? element = array[index];
                    if (element != null)
                    {
                        UnnestRandomProperty(element);
                    }
                }
            }
        }
        
        /// <summary>
        /// Converts between array and object representations
        /// </summary>
        public void ConvertBetweenArrayAndObject(JToken token)
        {
            if (token == null)
                return;
                
            if (token is JObject obj)
            {
                // Convert object to array
                JArray newArray = new JArray();
                
                // Add each property value to the array
                foreach (var kvp in obj.Properties)
                {
                    if (kvp.Value != null)
                    {
                        newArray.Add(CloneToken(kvp.Value));
                    }
                }
                
                // Find the property in the parent object that contains this token
                // and replace it with the new array
                foreach (var property in obj.Properties)
                {
                    // Since we can't directly access the parent in Neo.Json,
                    // we'll have to modify the object itself
                    if (property.Value != null)
                    {
                        JToken? removedValue;
                        ((IDictionary<string, JToken?>)obj).Remove(property.Key, out removedValue);
                        obj["items"] = newArray;
                        break;
                    }
                }
            }
            else if (token is JArray array)
            {
                // Convert array to object
                JObject newObj = new JObject();
                
                // Add each array element as a property
                for (int i = 0; i < array.Count; i++)
                {
                    if (array[i] != null)
                    {
                        newObj[$"item{i}"] = CloneToken(array[i]);
                    }
                }
                
                // Since we can't directly modify the parent, we'll clear the array
                // and add a single element that is the new object
                if (array.Count > 0)
                {
                    array.Clear();
                    array.Add(newObj);
                }
            }
        }
        
        /// <summary>
        /// Creates a deeply nested structure with a specified maximum nesting level
        /// </summary>
        public JToken CreateNestedStructure(int maxNest)
        {
            if (maxNest <= 0)
            {
                return new JNumber(_random.Next(100));
            }
            
            if (_random.NextDouble() < 0.5)
            {
                // Create a nested object
                JObject obj = new JObject();
                obj["nested"] = CreateNestedStructure(maxNest - 1);
                return obj;
            }
            else
            {
                // Create a nested array
                JArray array = new JArray();
                array.Add(CreateNestedStructure(maxNest - 1));
                return array;
            }
        }
        
        /// <summary>
        /// Creates a deeply nested array structure with a specified maximum nesting level
        /// </summary>
        public JToken CreateNestedArrayStructure(int maxNest)
        {
            if (maxNest <= 0)
            {
                return new JNumber(_random.Next(100));
            }
            
            JArray array = new JArray();
            array.Add(CreateNestedArrayStructure(maxNest - 1));
            
            return array;
        }
        
        /// <summary>
        /// Creates a structure with alternating object and array nesting
        /// </summary>
        public JToken CreateAlternatingStructure(int maxNest, bool startWithObject = true)
        {
            if (maxNest <= 0)
            {
                return new JNumber(_random.Next(100));
            }
            
            if (startWithObject)
            {
                JObject obj = new JObject();
                obj["nested"] = CreateAlternatingStructure(maxNest - 1, false);
                return obj;
            }
            else
            {
                JArray array = new JArray();
                array.Add(CreateAlternatingStructure(maxNest - 1, true));
                return array;
            }
        }
        
        /// <summary>
        /// Creates a structure with multiple properties at each level
        /// </summary>
        public JToken CreateBranchingStructure(int maxNest, int branchFactor = 2)
        {
            if (maxNest <= 0)
            {
                return new JNumber(_random.Next(100));
            }
            
            JObject obj = new JObject();
            
            for (int i = 0; i < branchFactor; i++)
            {
                obj["branch" + i] = CreateBranchingStructure(maxNest - 1, branchFactor);
            }
            
            return obj;
        }
        
        /// <summary>
        /// Creates a deep clone of a JToken since Neo.Json doesn't have a DeepClone method
        /// </summary>
        public JToken? CloneToken(JToken? token)
        {
            if (token == null)
                return null;
                
            if (token is JObject obj)
            {
                JObject newObj = new JObject();
                foreach (var prop in obj.Properties)
                {
                    if (prop.Value != null)
                    {
                        newObj[prop.Key] = CloneToken(prop.Value);
                    }
                    else
                    {
                        newObj[prop.Key] = null;
                    }
                }
                return newObj;
            }
            else if (token is JArray array)
            {
                JArray newArray = new JArray();
                foreach (var item in array)
                {
                    newArray.Add(item != null ? CloneToken(item) : null);
                }
                return newArray;
            }
            else if (token is JNumber num)
            {
                return new JNumber(num.Value);
            }
            else if (token is JString str)
            {
                return new JString(str.Value);
            }
            else if (token is JBoolean boolean)
            {
                return new JBoolean(boolean.Value);
            }
            
            // Default fallback - only reached for null or unknown token types
            try
            {
                return token != null ? JToken.Parse(token.ToString()) : null;
            }
            catch
            {
                // If parsing fails, return null as a safe fallback
                return null;
            }
        }
    }
}
