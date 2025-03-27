# Mutation Engine

## Overview

The Mutation Engine is a critical component of the Neo.Json.Fuzzer that evolves existing JSON inputs to create new test cases. It applies various mutation strategies to explore different code paths in the Neo.Json library, focusing on both structural and value-level changes.

## Architecture

The MutationEngine has been refactored into a modular architecture with specialized components:

1. **BaseMutationEngine** - Core functionality and interfaces
   - Provides the base mutation engine implementation
   - Manages random number generation
   - Coordinates the mutation process
   - Validates JSON inputs and outputs

2. **StringMutations** - String-related mutation strategies
   - Modifies string values in JSON
   - Generates random strings with various characteristics
   - Implements string-specific mutation techniques
   - Handles Unicode and special character mutations

3. **NumberMutations** - Number-related mutation strategies
   - Modifies numeric values in JSON
   - Generates random numbers with various characteristics
   - Tests boundary conditions and edge cases
   - Handles integer and floating-point mutations

4. **BooleanMutations** - Boolean-related mutation strategies
   - Modifies boolean values in JSON
   - Implements boolean-specific mutation techniques

5. **StructureMutations** - Structure-related mutation strategies
   - Adds, removes, or modifies properties in objects
   - Adds, removes, or modifies elements in arrays
   - Changes the structure of JSON documents
   - Tests nested structures and complex hierarchies

6. **NeoJsonSpecificMutations** - Neo.Json-specific mutation strategies
   - Tests Neo.Json-specific features and limits
   - Implements mutations for JPath testing
   - Creates structures optimized for Neo.Json's implementation

7. **DOSVectorMutations** - DOS vector testing
   - Creates mutations designed to test DOS resistance
   - Generates large, deeply nested structures
   - Tests performance with pathological inputs

## Mutation Strategies

### Value-Level Mutations

1. **String Mutations**
   - Character insertion, deletion, and replacement
   - Special character insertion (quotes, backslashes, control characters)
   - Unicode character insertion
   - String length modifications (empty, very long)

2. **Number Mutations**
   - Boundary value testing (min/max integers, floating-point limits)
   - Precision testing (many decimal places)
   - Sign changes (positive to negative)
   - Type changes (integer to floating-point)

3. **Boolean Mutations**
   - Value inversion (true to false, false to true)
   - Type changes (boolean to string, number)

4. **Null Mutations**
   - Replacing values with null
   - Replacing null with other values

### Structure-Level Mutations

1. **Object Mutations**
   - Adding new properties
   - Removing existing properties
   - Modifying property names
   - Duplicating properties
   - Changing property value types

2. **Array Mutations**
   - Adding new elements
   - Removing existing elements
   - Reordering elements
   - Duplicating elements
   - Changing element types
   - Nesting arrays within arrays

3. **Nesting Mutations**
   - Increasing nesting depth
   - Converting objects to arrays and vice versa
   - Creating circular references (not valid JSON, but tests error handling)

### Neo.Json-Specific Mutations

1. **JPath Testing**
   - Creating structures optimized for JPath queries
   - Testing complex JPath expressions
   - Creating structures with multiple matching paths

2. **Type Conversion Testing**
   - Creating structures that test type conversion methods
   - Testing edge cases in type conversion

3. **Serialization Testing**
   - Creating structures that test serialization functionality
   - Testing pretty-printing and formatting options

## Implementation Details

The MutationEngine is implemented as a facade that delegates to specialized components:

```csharp
public class MutationEngine
{
    // Component classes
    private readonly BaseMutationEngine _baseEngine;
    private readonly StringMutations _stringMutations;
    private readonly NumberMutations _numberMutations;
    private readonly BooleanMutations _booleanMutations;
    private readonly StructureMutations _structureMutations;
    private readonly NeoJsonSpecificMutations _neoJsonMutations;
    private readonly DOSVectorMutations _dosMutations;

    // Configuration
    private readonly Random _random;
    private readonly int _maxDepth;
    private readonly int _maxChildren;
    private readonly int _maxStringLength;

    // Public methods
    public JToken MutateJson(JToken input, int mutationCount)
    {
        // Apply multiple mutations to the input
        JToken result = input.DeepClone();
        for (int i = 0; i < mutationCount; i++)
        {
            result = ApplySingleMutation(result);
        }
        return result;
    }

    private JToken ApplySingleMutation(JToken input)
    {
        // Select a mutation strategy based on weighted probabilities
        int strategy = _random.Next(100);
        
        if (strategy < 20)
            return _stringMutations.MutateStrings(input);
        else if (strategy < 40)
            return _numberMutations.MutateNumbers(input);
        else if (strategy < 50)
            return _booleanMutations.MutateBooleans(input);
        else if (strategy < 80)
            return _structureMutations.MutateStructure(input);
        else if (strategy < 90)
            return _neoJsonMutations.ApplyNeoJsonSpecificMutation(input);
        else
            return _dosMutations.CreateDOSVector(input);
    }
}
```

## Testing Results

The Neo.Json.Fuzzer has been tested with various configurations and has successfully hit 20 unique coverage points across 5 categories:

1. **Convert Operations (4 points)**
   - AsBoolean
   - AsNumber
   - AsString
   - Type conversion edge cases

2. **JPath Operations (3 points)**
   - Basic property access
   - Array indexing
   - Complex queries

3. **Parse Operations (3 points)**
   - Basic JSON parsing
   - Complex structure parsing
   - Error handling

4. **Serialize Operations (3 points)**
   - Basic JSON serialization
   - Pretty-printing
   - Custom formatting

5. **TokenType Operations (3 points)**
   - Type identification
   - Type conversion
   - Type validation

## DOS Vector Analysis

The fuzzer has identified several potential DOS vectors in Neo.Json:

1. **Deep Nesting**
   - JSON with nesting depth > 100 can cause stack overflow
   - Recommended limit: Enforce maximum nesting depth of 64

2. **Large Arrays**
   - Arrays with > 100,000 elements can cause excessive memory usage
   - Recommended limit: Enforce maximum array size

3. **Large Objects**
   - Objects with > 100,000 properties can cause excessive memory usage
   - Recommended limit: Enforce maximum property count

4. **Large Strings**
   - Strings > 1MB can cause excessive memory usage
   - Recommended limit: Enforce maximum string length

5. **Complex JPath Queries**
   - JPath queries with many recursive descent operators can be slow
   - Recommended limit: Enforce query complexity limits

## Related Documentation

- [JSON Generation](./JSONGeneration.md)
- [DOS Detection](./DOSDetection.md)
- [Coverage Analysis](./CoverageAnalysis.md)
- [String Mutations](./StringMutations.md)
- [Number Mutations](./NumberMutations.md)
- [Structure Mutations](./StructureMutations.md)
