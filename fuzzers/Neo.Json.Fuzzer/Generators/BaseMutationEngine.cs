// Copyright (C) 2015-2025 The Neo Project.
//
// BaseMutationEngine.cs file belongs to the neo project and is free
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
    /// Base class for the mutation engine that provides core functionality
    /// </summary>
    public class BaseMutationEngine
    {
        // Neo.Json specific limits
        public const int NEO_DEFAULT_MAX_NEST = 64;
        public const int NEO_LOW_MAX_NEST = 10;
        public const int NEO_HIGH_MAX_NEST = 128;

        // Reasonable limits for values
        public const int MAX_STRING_LENGTH = 10000;
        public const double MAX_NUMBER_VALUE = 1e15;
        public const int MAX_ARRAY_SIZE = 100;
        public const int MAX_OBJECT_SIZE = 100;

        // Random number generator
        protected readonly Random _random;

        // Mutation parameters
        protected readonly int _minMutations;
        protected readonly int _maxMutations;

        /// <summary>
        /// Creates a new base mutation engine
        /// </summary>
        /// <param name="random">Random number generator</param>
        /// <param name="minMutations">Minimum number of mutations to apply</param>
        /// <param name="maxMutations">Maximum number of mutations to apply</param>
        public BaseMutationEngine(Random random, int minMutations = 1, int maxMutations = 5)
        {
            _random = random ?? throw new ArgumentNullException(nameof(random));
            _minMutations = Math.Max(1, minMutations);
            _maxMutations = Math.Max(_minMutations, maxMutations);
        }

        /// <summary>
        /// Checks if a string is valid JSON
        /// </summary>
        public bool IsValidJson(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                return false;
            }

            try
            {
                JToken.Parse(json);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Generates a random JSON property name
        /// </summary>
        public string GenerateRandomPropertyName()
        {
            int length = _random.Next(3, 10);
            const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789_";

            char[] propertyName = new char[length];

            // First character should be a letter
            propertyName[0] = chars[_random.Next(52)]; // Only letters for first character

            // Rest can be any valid character
            for (int i = 1; i < length; i++)
            {
                propertyName[i] = chars[_random.Next(chars.Length)];
            }

            return new string(propertyName);
        }

        /// <summary>
        /// Generates a random JSON value
        /// </summary>
        public JToken GenerateRandomValue()
        {
            int valueType = _random.Next(5);

            return valueType switch
            {
                0 => _random.Next(100),                                // Number
                1 => _random.NextDouble() * 100,                       // Decimal
                2 => _random.Next(2) == 0,                             // Boolean
                3 => GenerateRandomString(10),                         // String
                _ => JToken.Null                                       // Null
            };
        }

        /// <summary>
        /// Generates a random string with controlled length
        /// </summary>
        public string GenerateRandomString(int maxLength)
        {
            int length = _random.Next(1, maxLength + 1);
            const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789 ";

            char[] stringValue = new char[length];
            for (int i = 0; i < length; i++)
            {
                stringValue[i] = chars[_random.Next(chars.Length)];
            }

            return new string(stringValue);
        }

        /// <summary>
        /// Gets a random character that might appear in JSON
        /// </summary>
        public char GetRandomJsonCharacter()
        {
            string validChars = "{}[]\":,0123456789.+-eEtrufalsn ";
            return validChars[_random.Next(validChars.Length)];
        }
    }
}
