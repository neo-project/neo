// Copyright (C) 2015-2025 The Neo Project.
//
// NumberMutations.cs file belongs to the neo project and is free
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
    /// Provides number-related mutation strategies for the mutation engine
    /// </summary>
    public class NumberMutations
    {
        private readonly BaseMutationEngine _engine;
        private readonly Random _random;
        
        // Special numeric values for testing
        private static readonly double[] _specialDoubles = new double[]
        {
            double.NaN,
            double.PositiveInfinity,
            double.NegativeInfinity,
            double.Epsilon,
            double.MaxValue,
            double.MinValue,
            0.0,
            -0.0,
            1.0,
            -1.0
        };
        
        // Integer boundary values for testing
        private static readonly long[] _specialIntegers = new long[]
        {
            long.MaxValue,
            long.MinValue,
            int.MaxValue,
            int.MinValue,
            short.MaxValue,
            short.MinValue,
            byte.MaxValue,
            0,
            1,
            -1
        };
        
        /// <summary>
        /// Initializes a new instance of the NumberMutations class
        /// </summary>
        public NumberMutations(BaseMutationEngine engine, Random random)
        {
            _engine = engine ?? throw new ArgumentNullException(nameof(engine));
            _random = random ?? throw new ArgumentNullException(nameof(random));
        }
        
        /// <summary>
        /// Modifies a numeric value in the JSON
        /// </summary>
        public string ModifyNumberValue(string json)
        {
            try
            {
                var token = JToken.Parse(json);
                
                // Add null check before using token
                if (token == null)
                {
                    return json;
                }
                
                ModifyNumberValuesRecursive(token);
                return token.ToString();
            }
            catch
            {
                // If parsing fails, return the original JSON
                return json;
            }
        }
        
        /// <summary>
        /// Recursively modifies numeric values in a JToken
        /// </summary>
        private void ModifyNumberValuesRecursive(JToken? token)
        {
            // Add null check at the beginning
            if (token == null)
            {
                return;
            }
            
            if (token is JObject obj)
            {
                // Collect all numeric properties
                List<(string, JToken)> numberProperties = new();
                foreach (var kvp in obj.Properties)
                {
                    if (kvp.Value is JNumber)
                    {
                        numberProperties.Add((kvp.Key, kvp.Value));
                    }
                }
                
                // Modify a random numeric property if any exist
                if (numberProperties.Count > 0 && _random.NextDouble() < 0.3)
                {
                    var (key, value) = numberProperties[_random.Next(numberProperties.Count)];
                    // Check if it's an integer by seeing if it equals its rounded value
                    double numValue = value.AsNumber();
                    bool isInteger = Math.Abs(numValue - Math.Round(numValue)) < double.Epsilon;
                    obj[key] = GenerateRandomNumber(isInteger);
                }
                
                // Recursively process all properties
                foreach (var kvp in obj.Properties)
                {
                    ModifyNumberValuesRecursive(kvp.Value);
                }
            }
            else if (token is JArray array)
            {
                // Recursively process all array elements
                foreach (var item in array)
                {
                    ModifyNumberValuesRecursive(item);
                }
            }
        }
        
        /// <summary>
        /// Generates a random number (integer or double)
        /// </summary>
        public JToken GenerateRandomNumber(bool integerOnly = false)
        {
            int strategy = _random.Next(integerOnly ? 3 : 5);
            
            return strategy switch
            {
                0 => GenerateRandomInteger(),
                1 => GenerateSpecialInteger(),
                2 => GenerateBoundaryInteger(),
                3 => GenerateRandomDouble(),
                _ => GenerateSpecialDouble()
            };
        }
        
        /// <summary>
        /// Generates a random integer
        /// </summary>
        public long GenerateRandomInteger()
        {
            int magnitude = _random.Next(10);
            
            return magnitude switch
            {
                0 => _random.Next(10),                                // 0-9
                1 => _random.Next(100),                               // 0-99
                2 => _random.Next(1000),                              // 0-999
                3 => _random.Next(10000),                             // 0-9999
                4 => _random.Next(100000),                            // 0-99999
                5 => _random.Next(1000000),                           // 0-999999
                6 => _random.Next(10000000),                          // 0-9999999
                7 => _random.Next(100000000),                         // 0-99999999
                8 => _random.Next(1000000000),                        // 0-999999999
                _ => _random.Next(int.MinValue, int.MaxValue)         // Full int range
            };
        }
        
        /// <summary>
        /// Generates a special integer value
        /// </summary>
        public long GenerateSpecialInteger()
        {
            return _specialIntegers[_random.Next(_specialIntegers.Length)];
        }
        
        /// <summary>
        /// Generates an integer near a boundary value
        /// </summary>
        public long GenerateBoundaryInteger()
        {
            long baseValue = GenerateSpecialInteger();
            int offset = _random.Next(-10, 11);
            
            try
            {
                checked
                {
                    return baseValue + offset;
                }
            }
            catch (OverflowException)
            {
                // If overflow occurs, return the base value
                return baseValue;
            }
        }
        
        /// <summary>
        /// Generates a random double value
        /// </summary>
        public double GenerateRandomDouble()
        {
            int magnitude = _random.Next(10);
            double baseValue = _random.NextDouble();
            
            return magnitude switch
            {
                0 => baseValue,                                       // 0.0-1.0
                1 => baseValue * 10,                                  // 0.0-10.0
                2 => baseValue * 100,                                 // 0.0-100.0
                3 => baseValue * 1000,                                // 0.0-1000.0
                4 => baseValue * 10000,                               // 0.0-10000.0
                5 => baseValue * 100000,                              // 0.0-100000.0
                6 => baseValue * 1000000,                             // 0.0-1000000.0
                7 => baseValue * 10000000,                            // 0.0-10000000.0
                8 => baseValue * 100000000,                           // 0.0-100000000.0
                _ => baseValue * 1000000000                           // 0.0-1000000000.0
            };
        }
        
        /// <summary>
        /// Generates a special double value
        /// </summary>
        public double GenerateSpecialDouble()
        {
            return _specialDoubles[_random.Next(_specialDoubles.Length)];
        }
        
        /// <summary>
        /// Generates a double near a boundary value
        /// </summary>
        public double GenerateBoundaryDouble()
        {
            double baseValue = GenerateSpecialDouble();
            
            // Skip NaN and infinities
            if (double.IsNaN(baseValue) || double.IsInfinity(baseValue))
            {
                return baseValue;
            }
            
            double offset = _random.NextDouble() * 10 - 5; // -5.0 to 5.0
            
            try
            {
                return baseValue + offset;
            }
            catch
            {
                // If any error occurs, return the base value
                return baseValue;
            }
        }
        
        /// <summary>
        /// Generates a very small double value
        /// </summary>
        public double GenerateVerySmallDouble()
        {
            return double.Epsilon * (_random.NextDouble() * 100 + 1);
        }
        
        /// <summary>
        /// Generates a very large double value
        /// </summary>
        public double GenerateVeryLargeDouble()
        {
            return double.MaxValue / (_random.NextDouble() * 100 + 1);
        }
        
        /// <summary>
        /// Generates a double with a specific number of decimal places
        /// </summary>
        public double GenerateDoubleWithPrecision(int decimalPlaces)
        {
            double value = _random.NextDouble() * 1000;
            double multiplier = Math.Pow(10, decimalPlaces);
            
            return Math.Round(value * multiplier) / multiplier;
        }
        
        /// <summary>
        /// Generates a number with scientific notation
        /// </summary>
        public double GenerateScientificNotation()
        {
            double mantissa = _random.NextDouble() * 10;
            int exponent = _random.Next(-10, 11);
            
            return mantissa * Math.Pow(10, exponent);
        }
    }
}
