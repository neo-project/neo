# JPath Testing Strategy

## Overview

This document outlines the comprehensive JPath testing strategy for the Neo.Json.Fuzzer. JPath is a powerful query language for JSON, similar to XPath for XML, allowing for complex navigation and selection of elements within JSON structures.

## Test Categories

### 1. Basic JPath Queries

- **Root element**: `$` - Testing access to the root element
- **Property access**: `$.property` - Testing direct property access
- **Array access**: `$[0]` - Testing array element access by index
- **All properties**: `$.*` - Testing wildcard property access
- **All array elements**: `$[*]` - Testing wildcard array element access

### 2. Advanced JPath Queries

- **Deep wildcard**: `$..property` - Testing recursive descent
- **Multiple property selection**: `$['prop1','prop2']` - Testing multiple property selection
- **Array slicing**: `$[start:end:step]` - Testing array slicing operations
- **Filter expressions**: `$[?(@.property > value)]` - Testing filter expressions
- **Script expressions**: `$[(@.length-1)]` - Testing script expressions

### 3. Complex Queries

- **Nested paths**: `$.store.book[0].title` - Testing deeply nested property access
- **Combined operators**: `$..book[?(@.price < 10)].title` - Testing combinations of operators
- **Union operations**: `$..book[0,1]` - Testing union operations
- **Complex filters**: `$..book[?(@.price > 10 && @.category == 'fiction')]` - Testing complex filters

## Test Implementation

### Generation Strategies

1. **Structure Generation**:
   - Generate JSON structures with varying complexity
   - Create deeply nested objects and arrays
   - Include properties with special characters and Unicode
   - Generate structures with repeated property names at different levels

2. **Query Generation**:
   - Generate simple to complex JPath queries
   - Create queries that test all JPath operators
   - Generate queries that should return empty results
   - Create queries that should return multiple results

3. **Edge Case Generation**:
   - Test with extremely deep nesting (up to Neo.Json limits)
   - Test with very large arrays
   - Test with property names that include special characters
   - Test with empty objects and arrays

### Mutation Strategies

1. **Structure Mutation**:
   - Add, remove, or modify properties and values
   - Change property types (string to number, object to array, etc.)
   - Modify array elements and their order

2. **Query Mutation**:
   - Modify existing queries to test slightly different paths
   - Change operators (e.g., from `$.*` to `$..*`)
   - Introduce syntax errors to test error handling

## Expected Results

- **Query Execution**: All valid JPath queries should execute correctly
- **Result Accuracy**: Query results should match expected values
- **Performance**: Query execution time should be reasonable
- **Error Handling**: Invalid queries should produce appropriate error messages

## Related Documentation

- [JSON Generation](./JSONGeneration.md)
- [Mutation Engine](./MutationEngine.md)
- [Structure Mutations](./StructureMutations.md)
