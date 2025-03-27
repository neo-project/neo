# JSON Generation Strategy

## Overview

This document outlines the comprehensive JSON generation strategy for the Neo.Json.Fuzzer. The generation of diverse, valid, and edge-case JSON structures is critical for effective fuzzing of the Neo.Json library.

## Generation Constraints

### Neo.Json-Specific Constraints

- **Maximum Nesting Depth**: Default 64 levels (configurable)
- **Maximum String Length**: 10,000 characters (configurable)
- **Maximum Array Size**: 100,000 elements (configurable)
- **Maximum Object Size**: 100,000 properties (configurable)
- **Numeric Range**: -1e15 to 1e15 (to avoid excessive validation)

### Value Constraints

1. **String Values**
   - **Length Range**: Empty to 10,000 characters
   - **Character Sets**: ASCII, Unicode (BMP and supplementary planes)
   - **Special Characters**: Control characters, quotes, backslashes, Unicode escapes
   - **Content Types**: Random, structured (JSON, XML, HTML), realistic (names, addresses)

2. **Numeric Values**
   - **Integer Range**: -1e15 to 1e15 for standard tests
   - **Extended Integer Range**: Testing with integers up to 100 digits for boundary tests
   - **Floating-Point Range**: -1e15 to 1e15 with varying precision (up to 15 decimal places)
   - **Special Values**: 0, -0, very small values, very large values, boundary values (Int32.Max, Int64.Min, etc.)
   - **Non-Standard Formats**: Testing with leading zeros, plus signs, underscores, commas (when applicable)

3. **Boolean Values**
   - **Values**: true, false
   - **Distribution**: Approximately 50/50 split
   - **Edge Cases**: Values that look like booleans ("true1", "false0")

4. **Null Values**
   - **Placement**: At various nesting levels and positions
   - **Edge Cases**: Values that look like null ("null0")

### Structure Constraints

1. **Object Structures**
   - **Size Range**: Empty to 100,000 properties
   - **Property Names**: Varying length, ASCII and Unicode, special characters
   - **Property Types**: Mixed types, homogeneous types
   - **Nesting**: Varying depth up to maximum
   - **Duplicate Properties**: Testing handling of duplicate property names

2. **Array Structures**
   - **Size Range**: Empty to 100,000 elements
   - **Element Types**: Mixed types, homogeneous types
   - **Nesting**: Arrays of arrays, arrays of objects
   - **Patterns**: Repeating patterns, random patterns
   - **Deep Nesting**: Testing with nesting depths approaching Neo.Json limits

## Generation Strategies

### 1. Random Generation

The fuzzer can generate completely random JSON structures:

```csharp
public JToken GenerateRandomJson(int maxDepth, int maxChildren)
{
    if (maxDepth <= 0 || _random.Next(10) == 0)
    {
        return GenerateRandomValue();
    }

    if (_random.Next(2) == 0)
    {
        return GenerateRandomArray(maxDepth - 1, maxChildren);
    }
    else
    {
        return GenerateRandomObject(maxDepth - 1, maxChildren);
    }
}
```

### 2. Template-Based Generation

The fuzzer can generate JSON based on predefined templates:

```csharp
public JToken GenerateFromTemplate(string template)
{
    JToken baseStructure = JToken.Parse(template);
    return FillTemplateWithRandomValues(baseStructure);
}
```

### 3. Mutation-Based Generation

The fuzzer can generate new JSON by mutating existing structures:

```csharp
public JToken GenerateByMutation(JToken input, int mutationCount)
{
    JToken result = input.DeepClone();
    for (int i = 0; i < mutationCount; i++)
    {
        result = ApplyRandomMutation(result);
    }
    return result;
}
```

### 4. Schema-Based Generation

The fuzzer can generate JSON that conforms to a specific schema:

```csharp
public JToken GenerateFromSchema(string jsonSchema)
{
    // Parse the schema
    JObject schema = JObject.Parse(jsonSchema);
    
    // Generate JSON that conforms to the schema
    return GenerateFromSchemaObject(schema);
}
```

### 5. Specialized Test Generation

The fuzzer includes specialized test generators for specific testing needs:

```csharp
public JToken GenerateSpecializedTestJson(string testType)
{
    switch (testType?.ToLowerInvariant())
    {
        case "integer_boundaries":
            return JToken.Parse(GenerateIntegerBoundariesTest());
        case "numeric_precision":
            return JToken.Parse(GeneratePrecisionTest());
        case "unicode_handling":
            return JToken.Parse(GenerateUnicodeTest());
        // Additional specialized test types
        default:
            return GenerateRandomJson(10, 10);
    }
}
```

## Neo.Json-Specific Generation

### 1. JPath Testing Structures

The fuzzer generates structures specifically designed to test JPath functionality:

```csharp
public JToken CreateJPathTestObject()
{
    JObject obj = new JObject();
    obj["simple"] = "value";
    obj["array"] = new JArray(1, 2, 3, 4, 5);
    obj["nested"] = new JObject();
    obj["nested"]["property"] = "nestedValue";
    obj["nested"]["array"] = new JArray("a", "b", "c");
    
    // Add more complex structures for JPath testing
    return obj;
}
```

### 2. Type Conversion Structures

The fuzzer generates structures to test Neo.Json's type conversion capabilities:

```csharp
public JToken CreateTypeConversionTest()
{
    JObject obj = new JObject();
    obj["stringAsNumber"] = "123";
    obj["booleanAsString"] = "true";
    obj["numberAsBoolean"] = 1;
    obj["stringAsBoolean"] = "false";
    
    // Add more type conversion test cases
    return obj;
}
```

### 3. Integer Boundary Structures

The fuzzer generates structures designed to test Neo.Json's handling of integer boundaries:

```csharp
public JToken CreateIntegerBoundaryTest()
{
    JObject obj = new JObject();
    obj["max_int32"] = int.MaxValue;
    obj["min_int32"] = int.MinValue;
    obj["max_int64"] = long.MaxValue;
    obj["min_int64"] = long.MinValue;
    obj["max_int32_plus_1"] = (long)int.MaxValue + 1;
    obj["large_integer_100_digits"] = new string('9', 100);
    
    // Add more integer boundary test cases
    return obj;
}
```

### 4. DOS Vector Structures

The fuzzer generates structures designed to test Neo.Json's resistance to DOS attacks:

```csharp
public JToken CreateDOSVector(int size, int depth)
{
    // Create a deeply nested structure with many elements
    JArray array = new JArray();
    for (int i = 0; i < size; i++)
    {
        JObject obj = new JObject();
        JObject current = obj;
        
        for (int j = 0; j < depth; j++)
        {
            string propertyName = $"level{j}";
            current[propertyName] = j == depth - 1 ? 
                (JToken)i.ToString() : new JObject();
            current = (JObject)current[propertyName];
        }
        
        array.Add(obj);
    }
    
    return array;
}
```

## Implementation Details

The JSON generation is implemented in several components:

1. **BaseMutationEngine**: Provides core generation functionality and Neo.Json-specific constants
2. **StringMutations**: Generates and mutates string values
3. **NumberMutations**: Generates and mutates numeric values
4. **NumericPrecisionMutations**: Specialized testing for numeric precision and integer boundaries
5. **BooleanMutations**: Generates and mutates boolean values
6. **StructureMutations**: Generates and mutates JSON structures
7. **NeoJsonSpecificMutations**: Generates Neo.Json-specific test cases
8. **DOSVectorMutations**: Generates potential DOS vectors

## Testing Results

The JSON generation strategies have identified several areas for improvement in Neo.Json:

1. **Duplicate Property Handling**: Neo.Json throws an error on duplicate property names, which differs from the JSON specification
2. **Maximum Nesting Depth**: Hard limit of 64 levels with limited configurability
3. **Performance with Large Inputs**: Significant performance degradation with deeply nested structures
4. **Numeric Precision Issues**: Potential issues with very large or precise numeric values
5. **Non-Standard Format Rejection**: Some non-standard formats (like underscores in numbers) are rejected

## Related Documentation

- [Mutation Engine](./MutationEngine.md)
- [JPath Testing](./JPathTesting.md)
- [DOS Detection](./DOSDetection.md)
- [Unicode Testing](./UnicodeTesting.md)
- [Numeric Precision Testing](./NumericPrecisionTesting.md)
