# Numeric Precision Testing Strategy

## Overview

This document outlines the comprehensive numeric precision testing strategy for the Neo.Json.Fuzzer. Proper handling of numeric values is critical for a JSON library to ensure accurate representation, parsing, and serialization of numbers with different precisions and ranges.

## Test Categories

### 1. Integer Values

- **Standard Integers**: Testing common integer values (0, 1, -1, etc.)
- **Boundary Values**: Testing values at the edges of integer ranges (Int32.Min, Int32.Max, Int64.Min, Int64.Max)
- **Large Integers**: Testing values beyond standard integer ranges (requiring Int64 or BigInteger)
- **Zero Values**: Testing zero and negative zero handling
- **Integer Representation**: Testing various representations (decimal, scientific, with leading zeros)
- **Non-Standard Formats**: Testing formats like integers with plus signs, underscores, or commas
- **Mixed Type Arrays**: Testing arrays with mixed integer representations

### 2. Floating-Point Values

- **Standard Decimals**: Testing common decimal values (0.5, -1.25, etc.)
- **Scientific Notation**: Testing values in scientific notation (1e10, 1.5e-20, etc.)
- **Precision Limits**: Testing values with many decimal places (testing precision limits)
- **Special Values**: Testing special floating-point values (NaN, Infinity, -Infinity)

### 3. Numeric Conversions

- **String-to-Number**: Testing conversion from string representations to numeric values
- **Number-to-String**: Testing conversion from numeric values to string representations
- **Type Conversions**: Testing conversion between different numeric types (int to double, etc.)
- **Rounding Behavior**: Testing rounding behavior in conversions (up to 15 decimal places)

## Test Implementation

### Generation Strategies

1. **Boundary Testing**:
   - Generate values at and around integer boundaries (Int32.Min, Int32.Max, Int64.Min, Int64.Max)
   - Generate values at and around floating-point precision boundaries
   - Test with values just below and above power-of-10 boundaries
   - Test with boundary +/- 1 values (Int32.MaxValue + 1, Int64.MinValue - 1)

2. **Precision Testing**:
   - Generate values with increasing numbers of decimal places (up to 15 decimal places)
   - Generate values with specific patterns of digits to test precision
   - Test with values that require exact decimal representation

3. **Special Value Testing**:
   - Generate special numeric values (NaN, Infinity, -Infinity)
   - Generate values that might cause overflow or underflow
   - Test with values that are at the limits of representable numbers
   - Test with extremely large integers (20, 30, 50, 100 digits)

### Mutation Strategies

1. **Digit Manipulation**:
   - Add, remove, or modify digits in numeric values
   - Change decimal point position
   - Add or remove leading/trailing zeros

2. **Notation Changes**:
   - Convert between standard and scientific notation
   - Modify exponent values in scientific notation
   - Add or remove decimal points
   - Test with trailing decimal points (e.g., "12345.")

3. **Sign Manipulation**:
   - Change signs of numeric values
   - Test with explicit positive signs (+123)
   - Test with negative zero (-0)
   - Test with negative large integers

4. **Format Variations**:
   - Test with non-standard formats (hex, binary, octal notation)
   - Test with separators (underscores, commas, spaces)
   - Test with values that look like other types ("true1", "false0", "null0")

## Implementation Details

The numeric precision testing is implemented in the `NumericPrecisionMutations.cs` file, which provides several specialized test methods:

```csharp
// Generate comprehensive integer boundary tests
private string GenerateIntegerBoundariesTest()
{
    JObject root = new JObject();
    
    // Integer boundary values
    root["max_int32"] = int.MaxValue;
    root["min_int32"] = int.MinValue;
    root["max_int64"] = long.MaxValue;
    root["min_int64"] = long.MinValue;
    root["max_uint32"] = uint.MaxValue;
    root["max_uint64"] = ulong.MaxValue;
    
    // Integer boundary +/- 1
    root["max_int32_plus_1"] = (long)int.MaxValue + 1;
    root["min_int32_minus_1"] = (long)int.MinValue - 1;
    
    // Large integer values
    root["large_integer_20_digits"] = "12345678901234567890";
    root["large_integer_100_digits"] = new string('9', 100);
    
    // ... additional test cases ...
    
    return root.ToString();
}
```

Additional specialized test methods include:
- `GeneratePrecisionTestValues()`: Tests decimal precision up to 15 decimal places
- `GenerateExponentTestValues()`: Tests scientific notation with various exponents
- `GenerateSpecialValuesTest()`: Tests special numeric values and edge cases

## Expected Results

- **Parsing Accuracy**: All valid numeric values should be parsed with appropriate precision
- **Serialization Accuracy**: All numeric values should be serialized with appropriate precision
- **Type Preservation**: Integer values should remain integers, floating-point values should preserve decimal places
- **Error Handling**: Invalid numeric formats should be handled gracefully with appropriate errors
- **Boundary Handling**: Values at or beyond integer boundaries should be handled correctly
- **Format Tolerance**: Various numeric formats should be parsed correctly when valid

## Testing Results

Testing has identified several areas for improvement in Neo.Json's numeric handling:

1. **Large Integer Handling**: Very large integers (beyond 64-bit) may not be handled consistently
2. **Non-Standard Formats**: Some non-standard formats (like underscores in numbers) are rejected
3. **Precision Limits**: Decimal precision is limited to 15 decimal places
4. **Duplicate Property Handling**: Neo.Json throws an error on duplicate property names

## Related Documentation

- [JSON Generation](./JSONGeneration.md)
- [Mutation Engine](./MutationEngine.md)
- [DOS Detection](./DOSDetection.md)
