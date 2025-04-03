// Copyright (C) 2015-2025 The Neo Project.
//
// NumericPrecisionMutations.cs file belongs to the neo project and is free
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
using System.Globalization;
using System.Linq;
using System.Text;

namespace Neo.Json.Fuzzer.Generators
{
    /// <summary>
    /// Provides numeric precision-specific mutation strategies for the mutation engine
    /// </summary>
    public class NumericPrecisionMutations
    {
        private readonly BaseMutationEngine _engine;
        private readonly Random _random;

        // Special numeric values
        private static readonly double[] _specialValues = new[]
        {
            double.Epsilon,
            double.MaxValue,
            double.MinValue,
            double.NaN,
            double.PositiveInfinity,
            double.NegativeInfinity,
            1.0 / 0.0,
            -1.0 / 0.0,
            0.0 / 0.0
        };

        // Boundary values
        private static readonly double[] _boundaryValues = new[]
        {
            0.0,
            -0.0,
            1.0,
            -1.0,
            double.Epsilon,
            -double.Epsilon,
            double.MaxValue,
            double.MinValue,
            1.7976931348623157e+308,  // Max double
            -1.7976931348623157e+308, // Min double
            4.9406564584124654e-324,  // Min positive double
            -4.9406564584124654e-324, // Max negative double
            2.2250738585072014e-308,  // Min normal double
            -2.2250738585072014e-308  // Max negative normal double
        };

        /// <summary>
        /// Initializes a new instance of the NumericPrecisionMutations class
        /// </summary>
        public NumericPrecisionMutations(BaseMutationEngine engine, Random random)
        {
            _engine = engine ?? throw new ArgumentNullException(nameof(engine));
            _random = random ?? throw new ArgumentNullException(nameof(random));
        }

        /// <summary>
        /// Applies a numeric precision-specific mutation to the JSON
        /// </summary>
        public string ApplyNumericPrecisionMutation(string json)
        {
            try
            {
                var token = JToken.Parse(json);

                // Select a mutation strategy
                int strategy = _random.Next(5);

                return strategy switch
                {
                    0 => token != null ? ReplaceWithBoundaryValues(token) : json,
                    1 => token != null ? ReplaceWithSpecialValues(token) : json,
                    2 => token != null ? ReplaceWithLargeExponent(token) : json,
                    3 => token != null ? ReplaceWithRepeatingDecimal(token) : json,
                    _ => token != null ? ReplaceWithHighPrecisionValue(token) : json
                };
            }
            catch
            {
                // If parsing fails, return the original JSON
                return json;
            }
        }

        /// <summary>
        /// Replaces numeric values with boundary values
        /// </summary>
        private string ReplaceWithBoundaryValues(JToken token)
        {
            if (token == null) return "{}";
            ModifyNumbers(token, (num) =>
            {
                // Select a random boundary value
                double boundaryValue = _boundaryValues[_random.Next(_boundaryValues.Length)];
                return new JNumber(boundaryValue);
            });
            return token.ToString();
        }

        /// <summary>
        /// Replaces numeric values with special values
        /// </summary>
        private string ReplaceWithSpecialValues(JToken token)
        {
            if (token == null) return "{}";
            ModifyNumbers(token, (num) =>
            {
                // Select a random special value
                double specialValue = _specialValues[_random.Next(_specialValues.Length)];
                return new JNumber(specialValue);
            });
            return token.ToString();
        }

        /// <summary>
        /// Replaces numeric values with values having large exponents
        /// </summary>
        private string ReplaceWithLargeExponent(JToken token)
        {
            if (token == null) return "{}";
            ModifyNumbers(token, (num) =>
            {
                // Generate a value with a large exponent
                int exponent = _random.Next(100, 308); // Max exponent for double is 308
                double sign = _random.Next(2) == 0 ? 1.0 : -1.0;
                double mantissa = _random.NextDouble() * 9.0 + 1.0; // 1.0 to 10.0
                double value = sign * mantissa * Math.Pow(10, exponent);
                return new JNumber(value);
            });
            return token.ToString();
        }

        /// <summary>
        /// Replaces numeric values with repeating decimal values
        /// </summary>
        private string ReplaceWithRepeatingDecimal(JToken token)
        {
            if (token == null) return "{}";
            ModifyNumbers(token, (num) =>
            {
                // Generate a repeating decimal
                int integerPart = _random.Next(-100, 100);

                // Create a repeating pattern
                StringBuilder sb = new StringBuilder();
                sb.Append(integerPart);
                sb.Append('.');

                // Generate a repeating pattern (1-5 digits)
                int patternLength = _random.Next(1, 6);
                for (int i = 0; i < patternLength; i++)
                {
                    sb.Append(_random.Next(10));
                }

                // Repeat the pattern to create a long decimal
                string pattern = sb.ToString().Substring(sb.ToString().IndexOf('.') + 1);
                sb.Clear();
                sb.Append(integerPart);
                sb.Append('.');

                // Repeat the pattern 5-10 times
                int repetitions = _random.Next(5, 11);
                for (int i = 0; i < repetitions; i++)
                {
                    sb.Append(pattern);
                }

                // Parse the resulting string as a double
                if (double.TryParse(sb.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out double result))
                {
                    return new JNumber(result);
                }

                // Fallback to a simple value if parsing fails
                return new JNumber(integerPart + _random.NextDouble());
            });
            return token.ToString();
        }

        /// <summary>
        /// Replaces numeric values with high precision values
        /// </summary>
        private string ReplaceWithHighPrecisionValue(JToken token)
        {
            if (token == null) return "{}";
            ModifyNumbers(token, (num) =>
            {
                // Generate a high precision value
                StringBuilder sb = new StringBuilder();

                // Integer part (1-10 digits)
                int integerDigits = _random.Next(1, 11);
                for (int i = 0; i < integerDigits; i++)
                {
                    sb.Append(i == 0 ? _random.Next(1, 10) : _random.Next(10));
                }

                sb.Append('.');

                // Fractional part (10-15 digits)
                int fractionalDigits = _random.Next(10, 16);
                for (int i = 0; i < fractionalDigits; i++)
                {
                    sb.Append(_random.Next(10));
                }

                // Parse the resulting string as a double
                if (double.TryParse(sb.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out double result))
                {
                    return new JNumber(result);
                }

                // Fallback to a simple value if parsing fails
                return new JNumber(_random.NextDouble() * 1000);
            });
            return token.ToString();
        }

        /// <summary>
        /// Modifies all numeric values in a token
        /// </summary>
        private void ModifyNumbers(JToken? token, Func<JNumber, JNumber> modifier)
        {
            if (token == null)
                return;

            if (token is JNumber num)
            {
                // Modify the numeric value directly
                // In Neo.Json, we can't modify tokens in place
                // The parent will handle updating this token during traversal
            }
            else if (token is JObject obj)
            {
                // Process all properties
                foreach (var property in obj.Properties.ToList())
                {
                    if (property.Value != null && property.Value is JNumber propNum)
                    {
                        // Directly update the property value
                        obj[property.Key] = modifier(propNum);
                    }
                    else if (property.Value != null)
                    {
                        ModifyNumbers(property.Value, modifier);
                    }
                }
            }
            else if (token is JArray array)
            {
                // Process all array elements
                for (int i = 0; i < array.Count; i++)
                {
                    if (array[i] != null && array[i] is JNumber arrayNum)
                    {
                        // Directly update the array element
                        array[i] = modifier(arrayNum);
                    }
                    else if (array[i] != null)
                    {
                        ModifyNumbers(array[i], modifier);
                    }
                }
            }
        }

        /// <summary>
        /// Generates a numeric value with specific precision characteristics
        /// </summary>
        public double GenerateNumericValue(string? type = null)
        {
            return type switch
            {
                "boundary" => _boundaryValues[_random.Next(_boundaryValues.Length)],
                "special" => _specialValues[_random.Next(_specialValues.Length)],
                "large_exponent" => GenerateLargeExponentValue(),
                "repeating_decimal" => GenerateRepeatingDecimalValue(),
                "high_precision" => GenerateHighPrecisionValue(),
                _ => _random.NextDouble() * 1000 - 500 // Random value between -500 and 500
            };
        }

        /// <summary>
        /// Generates a value with a large exponent
        /// </summary>
        private double GenerateLargeExponentValue()
        {
            int exponent = _random.Next(100, 308);
            double sign = _random.Next(2) == 0 ? 1.0 : -1.0;
            double mantissa = _random.NextDouble() * 9.0 + 1.0;
            return sign * mantissa * Math.Pow(10, exponent);
        }

        /// <summary>
        /// Generates a repeating decimal value
        /// </summary>
        private double GenerateRepeatingDecimalValue()
        {
            int integerPart = _random.Next(-100, 100);

            StringBuilder sb = new StringBuilder();
            sb.Append(integerPart);
            sb.Append('.');

            int patternLength = _random.Next(1, 6);
            for (int i = 0; i < patternLength; i++)
            {
                sb.Append(_random.Next(10));
            }

            string pattern = sb.ToString().Substring(sb.ToString().IndexOf('.') + 1);
            sb.Clear();
            sb.Append(integerPart);
            sb.Append('.');

            int repetitions = _random.Next(5, 11);
            for (int i = 0; i < repetitions; i++)
            {
                sb.Append(pattern);
            }

            if (double.TryParse(sb.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out double result))
            {
                return result;
            }

            return integerPart + _random.NextDouble();
        }

        /// <summary>
        /// Generates a high precision value
        /// </summary>
        private double GenerateHighPrecisionValue()
        {
            StringBuilder sb = new StringBuilder();

            int integerDigits = _random.Next(1, 11);
            for (int i = 0; i < integerDigits; i++)
            {
                sb.Append(i == 0 ? _random.Next(1, 10) : _random.Next(10));
            }

            sb.Append('.');

            int fractionalDigits = _random.Next(10, 16);
            for (int i = 0; i < fractionalDigits; i++)
            {
                sb.Append(_random.Next(10));
            }

            if (double.TryParse(sb.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out double result))
            {
                return result;
            }

            return _random.NextDouble() * 1000;
        }

        /// <summary>
        /// Generates a specialized test JSON for numeric precision testing
        /// </summary>
        /// <param name="testType">The type of numeric test to generate (optional)</param>
        /// <returns>A JSON string designed for numeric precision testing</returns>
        public string GenerateSpecializedTestJson(string? testType = null)
        {
            JObject obj = new JObject();

            switch (testType?.ToLowerInvariant())
            {
                case "boundary":
                    // Test boundary values
                    return GenerateBoundaryValuesTest();

                case "special":
                    // Test special values (NaN, Infinity, etc.)
                    return GenerateSpecialValuesTest();

                case "precision":
                    // Test precision issues
                    return GeneratePrecisionTest();

                case "exponent":
                    // Test exponent notation
                    return GenerateExponentTest();

                case "decimal":
                    // Test decimal precision
                    return GenerateDecimalPrecisionTest();

                case "integer":
                    // Test integer boundaries
                    return GenerateIntegerBoundariesTest();

                case "mixed":
                    // Mix of different numeric formats
                    return GenerateMixedNumericTest();

                default:
                    // Default numeric test with various values
                    obj["type"] = "numeric_precision_test";
                    obj["timestamp"] = DateTime.UtcNow.ToString("o");

                    // Add various numeric test cases
                    obj["boundary_samples"] = CreateNumericArray(_boundaryValues.Take(5).ToArray());
                    obj["special_samples"] = CreateNumericArray(_specialValues.Take(3).ToArray());
                    obj["precision_samples"] = CreateNumericArray(GeneratePrecisionTestValues(5));
                    obj["exponent_samples"] = CreateNumericArray(GenerateExponentTestValues(5));

                    return obj.ToString();
            }
        }

        /// <summary>
        /// Creates a JArray from an array of double values
        /// </summary>
        private JArray CreateNumericArray(double[] values)
        {
            JArray array = new JArray();
            foreach (var value in values)
            {
                array.Add(new JNumber(value));
            }
            return array;
        }

        /// <summary>
        /// Generates a test focused on boundary values
        /// </summary>
        private string GenerateBoundaryValuesTest()
        {
            JObject obj = new JObject();
            obj["type"] = "boundary_values_test";

            // Add all boundary values
            JArray values = new JArray();
            foreach (var value in _boundaryValues)
            {
                values.Add(new JNumber(value));
            }
            obj["boundary_values"] = values;

            // Add nested objects with boundary values
            JObject nested = new JObject();
            for (int i = 0; i < Math.Min(5, _boundaryValues.Length); i++)
            {
                nested[$"value_{i}"] = new JNumber(_boundaryValues[i]);
            }
            obj["nested_values"] = nested;

            return obj.ToString();
        }

        /// <summary>
        /// Generates a test focused on special values
        /// </summary>
        private string GenerateSpecialValuesTest()
        {
            JObject obj = new JObject();
            obj["type"] = "special_values_test";

            // Add all special values
            JArray values = new JArray();
            foreach (var value in _specialValues)
            {
                // Note: Some special values may not be directly representable in JSON
                // and will be handled according to the JSON implementation
                values.Add(new JNumber(value));
            }
            obj["special_values"] = values;

            return obj.ToString();
        }

        /// <summary>
        /// Generates a test focused on precision issues
        /// </summary>
        private string GeneratePrecisionTest()
        {
            JObject obj = new JObject();
            obj["type"] = "precision_test";

            // Generate precision test values
            double[] precisionValues = GeneratePrecisionTestValues(20);

            // Add precision test values
            JArray values = new JArray();
            foreach (var value in precisionValues)
            {
                values.Add(new JNumber(value));
            }
            obj["precision_values"] = values;

            // Add string representations for comparison
            JArray stringReps = new JArray();
            foreach (var value in precisionValues)
            {
                stringReps.Add(value.ToString(CultureInfo.InvariantCulture));
            }
            obj["string_representations"] = stringReps;

            return obj.ToString();
        }

        /// <summary>
        /// Generates values designed to test precision issues
        /// </summary>
        private double[] GeneratePrecisionTestValues(int count)
        {
            List<double> values = new List<double>
            {
                // Add some known problematic values
                0.1,
                0.2,
                0.1 + 0.2,
                0.3
            };

            // Generate additional values
            for (int i = values.Count; i < count; i++)
            {
                // Generate values with many decimal places
                double mantissa = _random.NextDouble();
                int decimalPlaces = _random.Next(1, 15); // Up to 15 decimal places (valid for Math.Round)
                double value = Math.Round(mantissa, decimalPlaces);
                values.Add(value);
            }

            return values.ToArray();
        }

        /// <summary>
        /// Generates a test focused on exponent notation
        /// </summary>
        private string GenerateExponentTest()
        {
            JObject obj = new JObject();
            obj["type"] = "exponent_test";

            // Generate exponent test values
            double[] exponentValues = GenerateExponentTestValues(20);

            // Add exponent test values
            JArray values = new JArray();
            foreach (var value in exponentValues)
            {
                values.Add(new JNumber(value));
            }
            obj["exponent_values"] = values;

            // Add string representations for comparison
            JArray stringReps = new JArray();
            foreach (var value in exponentValues)
            {
                stringReps.Add(value.ToString("E", CultureInfo.InvariantCulture));
            }
            obj["string_representations"] = stringReps;

            return obj.ToString();
        }

        /// <summary>
        /// Generates values designed to test exponent notation
        /// </summary>
        private double[] GenerateExponentTestValues(int count)
        {
            List<double> values = new List<double>
            {
                // Add some known values with exponents
                1e-10,
                1e10,
                1.23456e20,
                9.87654e-15
            };

            // Generate additional values
            for (int i = values.Count; i < count; i++)
            {
                // Generate values with exponents
                double mantissa = _random.NextDouble() * 10;
                int exponent = _random.Next(-30, 30);
                double value = mantissa * Math.Pow(10, exponent);
                values.Add(value);
            }

            return values.ToArray();
        }

        /// <summary>
        /// Generates a test focused on decimal precision
        /// </summary>
        private string GenerateDecimalPrecisionTest()
        {
            JObject obj = new JObject();
            obj["type"] = "decimal_precision_test";

            // Generate values with specific decimal places
            JObject decimalTests = new JObject();
            for (int places = 1; places <= 15; places++)
            {
                JArray values = new JArray();
                for (int i = 0; i < 5; i++)
                {
                    double value = Math.Round(_random.NextDouble(), places);
                    values.Add(new JNumber(value));
                }
                decimalTests[$"decimal_places_{places}"] = values;
            }
            obj["decimal_tests"] = decimalTests;

            return obj.ToString();
        }

        /// <summary>
        /// Generates a test focused on integer boundaries
        /// </summary>
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
            root["max_int64_plus_1"] = $"{long.MaxValue}1"; // String representation to exceed long.MaxValue
            root["min_int64_minus_1"] = $"-{long.MinValue}1"; // String representation to exceed long.MinValue

            // Large integer values that might cause overflow
            root["large_integer_20_digits"] = "12345678901234567890";
            root["large_integer_30_digits"] = "123456789012345678901234567890";
            root["large_integer_50_digits"] = "12345678901234567890123456789012345678901234567890";
            root["large_integer_100_digits"] = new string('9', 100);

            // Negative large integer values
            root["negative_large_integer_20_digits"] = "-12345678901234567890";
            root["negative_large_integer_30_digits"] = "-123456789012345678901234567890";
            root["negative_large_integer_50_digits"] = "-12345678901234567890123456789012345678901234567890";
            root["negative_large_integer_100_digits"] = "-" + new string('9', 100);

            // Integer with leading zeros (should be treated as decimal, not octal)
            root["leading_zeros"] = "00012345";
            root["negative_leading_zeros"] = "-00012345";

            // Integer with plus sign
            root["plus_sign"] = "+12345";

            // Integer with underscore separators (not standard JSON but sometimes accepted)
            root["with_underscores"] = "1_000_000";

            // Integer with commas (not standard JSON but sometimes accepted)
            root["with_commas"] = "1,000,000";

            // Integer with spaces (not standard JSON)
            root["with_spaces"] = "1 000 000";

            // Integer with trailing decimal point
            root["trailing_decimal"] = "12345.";

            // Integer with scientific notation
            root["scientific_positive_exp"] = "1e10";
            root["scientific_negative_exp"] = "1e-10";

            // Integer with hex notation (not standard JSON)
            root["hex_notation"] = "0x1234";

            // Integer with binary notation (not standard JSON)
            root["binary_notation"] = "0b1010";

            // Integer with octal notation (not standard JSON)
            root["octal_notation"] = "0o1234";

            // Edge case: very close to integer boundary
            root["almost_max_int64"] = long.MaxValue - 1;
            root["almost_min_int64"] = long.MinValue + 1;

            // Edge case: integers that look like other types
            root["looks_like_boolean_true"] = "true1";
            root["looks_like_boolean_false"] = "false0";
            root["looks_like_null"] = "null0";

            // Array of mixed integer representations
            JArray mixedArray = new JArray
            {
                123,
                "456",
                789.0,
                "1e3",
                "0123",
                "+987"
            };
            root["mixed_integer_array"] = mixedArray;

            // Nested objects with integer values
            JObject nestedObj = new JObject();
            nestedObj["level1"] = 12345;
            JObject level2 = new JObject();
            level2["level2"] = 67890;
            JObject level3 = new JObject();
            level3["level3"] = long.MaxValue;
            level2["nested"] = level3;
            nestedObj["nested"] = level2;
            root["nested_integers"] = nestedObj;

            return root.ToString();
        }

        /// <summary>
        /// Generates a test with mixed numeric formats
        /// </summary>
        private string GenerateMixedNumericTest()
        {
            JObject obj = new JObject();
            obj["type"] = "mixed_numeric_test";

            // Create an array with mixed numeric values
            JArray mixedValues = new JArray
            {
                // Add some integers
                new JNumber(0),
                new JNumber(42),
                new JNumber(-100),

                // Add some decimals
                new JNumber(3.14159),
                new JNumber(-2.71828),
                new JNumber(0.1 + 0.2), // Precision issue

                // Add some exponents
                new JNumber(1e10),
                new JNumber(1.23e-15),

                // Add some boundary values
                new JNumber(double.Epsilon),
                new JNumber(double.MaxValue / 2)
            };

            obj["mixed_values"] = mixedValues;

            // Create a nested structure with different numeric formats
            JObject nested = new JObject();
            nested["integer"] = new JNumber(123456789);
            nested["decimal"] = new JNumber(0.123456789);
            nested["exponent"] = new JNumber(1.23456789e20);
            nested["negative"] = new JNumber(-987654321);

            JArray nestedArray = new JArray
            {
                new JNumber(1),
                new JNumber(2.5),
                new JNumber(3e10)
            };
            nested["array"] = nestedArray;

            obj["nested"] = nested;

            return obj.ToString();
        }
    }
}
