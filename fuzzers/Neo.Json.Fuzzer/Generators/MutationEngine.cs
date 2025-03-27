// Copyright (C) 2015-2025 The Neo Project.
//
// MutationEngine.cs file belongs to the neo project and is free
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
using System.Text.RegularExpressions;

namespace Neo.Json.Fuzzer.Generators
{
    /// <summary>
    /// Mutates JSON inputs to create new test cases
    /// </summary>
    public class MutationEngine
    {
        private readonly Random _random;
        private readonly int _minMutations;
        private readonly int _maxMutations;

        // Neo.Json specific limits
        private const int NEO_DEFAULT_MAX_NEST = 64;
        private const int NEO_LOW_MAX_NEST = 10;
        private const int NEO_HIGH_MAX_NEST = 128;

        // Reasonable limits for values
        private const int MAX_STRING_LENGTH = 10000;
        private const double MAX_NUMBER_VALUE = 1e15;
        private const int MAX_ARRAY_SIZE = 100;
        private const int MAX_OBJECT_SIZE = 100;

        // Component classes
        private readonly BaseMutationEngine _baseEngine;
        private readonly StringMutations _stringMutations;
        private readonly NumberMutations _numberMutations;
        private readonly BooleanMutations _booleanMutations;
        private readonly StructureMutations _structureMutations;
        private readonly NeoJsonSpecificMutations _neoJsonMutations;
        private readonly DOSVectorMutations _dosVectorMutations;
        private readonly CharacterMutations _characterMutations;

        // New specialized mutation components
        private readonly JPathMutations _jpathMutations;
        private readonly UnicodeMutations _unicodeMutations;
        private readonly NumericPrecisionMutations _numericPrecisionMutations;
        private readonly StreamingMutations _streamingMutations;
        private readonly ConcurrentAccessMutations _concurrentAccessMutations;

        /// <summary>
        /// Creates a new mutation engine
        /// </summary>
        /// <param name="random">Random number generator</param>
        /// <param name="minMutations">Minimum number of mutations to apply</param>
        /// <param name="maxMutations">Maximum number of mutations to apply</param>
        public MutationEngine(Random random, int minMutations = 1, int maxMutations = 5)
        {
            _random = random ?? throw new ArgumentNullException(nameof(random));
            _minMutations = Math.Max(1, minMutations);
            _maxMutations = Math.Max(_minMutations, maxMutations);

            // Initialize components
            _baseEngine = new BaseMutationEngine(random);
            _stringMutations = new StringMutations(_baseEngine, random);
            _numberMutations = new NumberMutations(_baseEngine, random);
            _booleanMutations = new BooleanMutations(_baseEngine, random);
            _structureMutations = new StructureMutations(_baseEngine, random);
            _neoJsonMutations = new NeoJsonSpecificMutations(_baseEngine, random);
            _dosVectorMutations = new DOSVectorMutations(_baseEngine, random);
            _characterMutations = new CharacterMutations(_baseEngine, random);

            // Initialize new specialized mutation components
            _jpathMutations = new JPathMutations(_baseEngine, random);
            _unicodeMutations = new UnicodeMutations(_baseEngine, random);
            _numericPrecisionMutations = new NumericPrecisionMutations(_baseEngine, random);
            _streamingMutations = new StreamingMutations(_baseEngine, random);
            _concurrentAccessMutations = new ConcurrentAccessMutations(_baseEngine, random);
        }

        /// <summary>
        /// Mutates a JSON string to create a new variant
        /// </summary>
        /// <param name="json">The JSON string to mutate</param>
        /// <returns>A mutated JSON string</returns>
        public string MutateJson(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                return "{}";
            }

            // Determine number of mutations to apply
            int mutationCount = _random.Next(_minMutations, _maxMutations + 1);

            string mutatedJson = json;

            for (int i = 0; i < mutationCount; i++)
            {
                // Select a mutation strategy
                mutatedJson = SelectAndApplyMutation(mutatedJson);
            }

            return mutatedJson;
        }

        /// <summary>
        /// Selects and applies a random mutation strategy
        /// </summary>
        private string SelectAndApplyMutation(string json)
        {
            try
            {
                // Try to parse the JSON first to determine if it's valid
                bool isValidJson = _baseEngine.IsValidJson(json);

                // Different mutation strategies based on whether the JSON is valid
                if (isValidJson)
                {
                    // For valid JSON, apply structured mutations with weighted probabilities
                    int probability = _random.Next(100);

                    return probability switch
                    {
                        < 30 => _structureMutations.ApplyRandomMutation(json),
                        < 45 => _neoJsonMutations.ApplyNeoJsonSpecificMutation(json),
                        < 55 => _jpathMutations.ApplyJPathMutation(json),
                        < 65 => _unicodeMutations.ApplyUnicodeMutation(json),
                        < 75 => _numericPrecisionMutations.ApplyNumericPrecisionMutation(json),
                        < 85 => _streamingMutations.ApplyStreamingMutation(json),
                        < 95 => _concurrentAccessMutations.ApplyConcurrentAccessMutation(json),
                        _ => _dosVectorMutations.ApplyDOSVectorMutation(json)
                    };
                }
                else
                {
                    // For invalid JSON, apply character-level mutations
                    return _characterMutations.ApplyCharacterMutation(json);
                }
            }
            catch
            {
                // If any error occurs during mutation, fall back to character-level mutations
                return _characterMutations.ApplyCharacterMutation(json);
            }
        }

        /// <summary>
        /// Checks if a string is valid JSON
        /// </summary>
        private bool IsValidJson(string json)
        {
            return _baseEngine.IsValidJson(json);
        }

        /// <summary>
        /// Generates a JSON structure specifically designed for testing a particular aspect
        /// </summary>
        /// <param name="testType">The type of test to generate JSON for</param>
        /// <returns>A JSON string designed for the specified test type</returns>
        public string GenerateSpecializedTestJson(string? testType)
        {
            if (string.IsNullOrEmpty(testType))
            {
                return "{}";
            }

            testType = testType.ToLowerInvariant();

            return testType switch
            {
                // JPath testing
                "jpath" => _jpathMutations.GenerateSpecializedTestJson(),
                "jpath_simple" => _jpathMutations.GenerateSpecializedTestJson("simple"),
                "jpath_wildcard" => _jpathMutations.GenerateSpecializedTestJson("wildcard"),
                "jpath_filter" => _jpathMutations.GenerateSpecializedTestJson("filter"),
                "jpath_union" => _jpathMutations.GenerateSpecializedTestJson("union"),
                "jpath_recursive" => _jpathMutations.GenerateSpecializedTestJson("recursive"),
                "jpath_slice" => _jpathMutations.GenerateSpecializedTestJson("slice"),

                // Unicode testing
                "unicode" => _unicodeMutations.GenerateSpecializedTestJson(),
                "unicode_bmp" => _unicodeMutations.GenerateSpecializedTestJson("ASCII"),
                "unicode_supplementary" => _unicodeMutations.GenerateSpecializedTestJson("CJK Unified Ideographs"),

                // Numeric precision testing
                "numeric" => _numericPrecisionMutations.GenerateSpecializedTestJson(),
                "numeric_integer" => _numericPrecisionMutations.GenerateSpecializedTestJson("integer"),
                "numeric_float" => _numericPrecisionMutations.GenerateSpecializedTestJson("float"),
                "numeric_boundary" => _numericPrecisionMutations.GenerateSpecializedTestJson("boundary"),
                "numeric_scientific" => _numericPrecisionMutations.GenerateSpecializedTestJson("scientific"),
                "numeric_precision" => _numericPrecisionMutations.GenerateSpecializedTestJson("precision"),

                // Streaming testing
                "streaming" => _streamingMutations.GenerateSpecializedTestJson(),
                "streaming_large" => _streamingMutations.GenerateSpecializedTestJson("large"),
                "streaming_nested" => _streamingMutations.GenerateSpecializedTestJson("nested"),
                "streaming_chunked" => _streamingMutations.GenerateSpecializedTestJson("chunked"),

                // Concurrent access testing
                "concurrent" => _concurrentAccessMutations.GenerateSpecializedTestJson(),
                "concurrent_shared" => _concurrentAccessMutations.GenerateSpecializedTestJson("shared"),
                "concurrent_race" => _concurrentAccessMutations.GenerateSpecializedTestJson("race"),

                // Default to a simple object
                _ => "{}"
            };
        }
    }
}
