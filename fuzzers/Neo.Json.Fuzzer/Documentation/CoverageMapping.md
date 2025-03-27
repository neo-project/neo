# Neo.Json Coverage Mapping

## Overview

This document maps each Neo.Json class and method to specific test cases in the Neo.Json.Fuzzer. This mapping ensures comprehensive coverage of the Neo.Json library and helps identify areas that may need additional testing.

## Coverage Structure

Coverage is tracked across the following dimensions:
1. **Class Coverage**: Ensuring all classes in Neo.Json are tested
2. **Method Coverage**: Ensuring all public methods are exercised
3. **Parameter Coverage**: Testing different parameter combinations
4. **Edge Case Coverage**: Testing boundary conditions and special cases
5. **Error Handling Coverage**: Testing error conditions and exceptions

## Class and Method Mapping

### JToken (Base Class)

| Method/Property | Test Category | Coverage Points | Implementation |
|-----------------|---------------|-----------------|----------------|
| Parse() | Parse | Parse:DefaultNesting, Parse:LowNesting, Parse:HighNesting | JsonRunner.TestParsingWithMaxNest |
| Parse(string, int) | Parse | Parse:CustomNesting | JsonRunner.TestParsingWithMaxNest |
| AsBoolean() | Convert | Convert:AsBoolean | JsonRunner.TestTypeConversions |
| AsNumber() | Convert | Convert:AsNumber | JsonRunner.TestTypeConversions |
| AsString() | Convert | Convert:AsString | JsonRunner.TestTypeConversions |
| GetBoolean() | Convert | Convert:GetBoolean | JsonRunner.TestTypeConversions |
| GetNumber() | Convert | Convert:GetNumber | JsonRunner.TestTypeConversions |
| GetString() | Convert | Convert:GetString | JsonRunner.TestTypeConversions |
| ToString() | Serialize | Serialize:ToString | JsonRunner.TestSerialization |
| ToString(bool) | Serialize | Serialize:ToStringIndented | JsonRunner.TestSerialization |
| ToByteArray() | Serialize | Serialize:ToByteArray | JsonRunner.TestSerialization |
| JsonPath() | JPath | JPath:Root, JPath:Properties, JPath:Array | JsonRunner.TestJPathOperations |
| Type property | TokenType | TokenType:* | CollectCoveragePoints |

### JObject

| Method/Property | Test Category | Coverage Points | Implementation |
|-----------------|---------------|-----------------|----------------|
| Constructor | Structure | TokenType:JObject | StructureMutations |
| Properties | Structure | JObject:Properties | StructureMutations |
| Item[string] get/set | Structure | JObject:ItemAccess | StructureMutations |
| ContainsProperty() | Structure | JObject:Contains | NeoJsonSpecificMutations |
| Remove() | Structure | JObject:Remove | StructureMutations |

### JArray

| Method/Property | Test Category | Coverage Points | Implementation |
|-----------------|---------------|-----------------|----------------|
| Constructor | Structure | TokenType:JArray | StructureMutations |
| Item[int] get/set | Structure | JArray:ItemAccess | StructureMutations |
| Add() | Structure | JArray:Add | StructureMutations |
| AddRange() | Structure | JArray:AddRange | StructureMutations |
| Clear() | Structure | JArray:Clear | StructureMutations |
| Insert() | Structure | JArray:Insert | StructureMutations |
| Remove() | Structure | JArray:Remove | StructureMutations |
| RemoveAt() | Structure | JArray:RemoveAt | StructureMutations |

### JString

| Method/Property | Test Category | Coverage Points | Implementation |
|-----------------|---------------|-----------------|----------------|
| Constructor | Value | TokenType:JString | StringMutations |
| Value property | Value | JString:Value | StringMutations |
| Unicode handling | Unicode | JString:Unicode | StringMutations |
| Escape sequences | Unicode | JString:Escape | StringMutations |

### JNumber

| Method/Property | Test Category | Coverage Points | Implementation |
|-----------------|---------------|-----------------|----------------|
| Constructor | Value | TokenType:JNumber | NumberMutations |
| Value property | Value | JNumber:Value | NumberMutations |
| Precision handling | Numeric | JNumber:Precision | NumberMutations |
| Range handling | Numeric | JNumber:Range | NumberMutations |

### JBoolean

| Method/Property | Test Category | Coverage Points | Implementation |
|-----------------|---------------|-----------------|----------------|
| Constructor | Value | TokenType:JBoolean | BooleanMutations |
| Value property | Value | JBoolean:Value | BooleanMutations |

### JPath

| Method/Property | Test Category | Coverage Points | Implementation |
|-----------------|---------------|-----------------|----------------|
| Basic queries | JPath | JPath:Basic | NeoJsonSpecificMutations |
| Wildcard queries | JPath | JPath:Wildcard | NeoJsonSpecificMutations |
| Recursive descent | JPath | JPath:Recursive | NeoJsonSpecificMutations |
| Filter expressions | JPath | JPath:Filter | NeoJsonSpecificMutations |
| Union operations | JPath | JPath:Union | NeoJsonSpecificMutations |

## Coverage Gaps and Priorities

### High Priority Gaps

1. **JPath Complex Queries**: More comprehensive testing of complex JPath queries
2. **Unicode Handling**: Expanded testing of Unicode characters across all planes
3. **Numeric Precision**: More thorough testing of numeric precision edge cases
4. **Streaming Scenarios**: Testing of incremental parsing and large document handling
5. **Concurrent Access**: Testing thread safety of all operations

### Medium Priority Gaps

1. **Error Recovery**: Testing recovery from various error conditions
2. **Memory Efficiency**: Testing memory usage patterns with different JSON structures
3. **Serialization Options**: More comprehensive testing of serialization options
4. **Custom Type Conversion**: Testing conversion to/from custom types
5. **Performance Profiling**: More detailed performance analysis

### Low Priority Gaps

1. **Integration Scenarios**: Testing integration with other Neo components
2. **Backward Compatibility**: Testing compatibility with older versions
3. **Documentation Examples**: Testing all examples in documentation
4. **Interoperability**: Testing interoperability with other JSON libraries

## Implementation Plan

1. Address high priority gaps first
2. Update this coverage mapping document as new tests are added
3. Track coverage metrics over time to ensure increasing coverage
4. Periodically review for new gaps as Neo.Json evolves
