// Copyright (C) 2015-2025 The Neo Project.
//
// ConfigurationManager.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.IO;
using System.Text.Json;

namespace Neo.VM.Fuzzer.Utils
{
    /// <summary>
    /// Manages the configuration settings for the Neo VM Fuzzer
    /// </summary>
    public class ConfigurationManager
    {
        private static ConfigurationManager? _instance;

        /// <summary>
        /// Gets the singleton instance of the ConfigurationManager
        /// </summary>
        public static ConfigurationManager Instance => _instance ??= new ConfigurationManager();

        /// <summary>
        /// Gets or sets the number of fuzzing iterations to run
        /// </summary>
        public int Iterations { get; set; } = 1000;

        /// <summary>
        /// Gets or sets the random seed for reproducibility
        /// </summary>
        public int Seed { get; set; } = new Random().Next();

        /// <summary>
        /// Gets or sets the output directory for fuzzing results
        /// </summary>
        public string OutputDirectory { get; set; } = Path.Combine(Directory.GetCurrentDirectory(), "fuzzer_output");

        /// <summary>
        /// Gets or sets the directory containing initial corpus scripts
        /// </summary>
        public string? CorpusDirectory { get; set; }

        /// <summary>
        /// Gets or sets the timeout in milliseconds for script execution
        /// </summary>
        public int TimeoutMs { get; set; } = 1000;

        /// <summary>
        /// Gets or sets the mutation rate (0.0 to 1.0)
        /// </summary>
        public double MutationRate { get; set; } = 0.1;

        /// <summary>
        /// Gets or sets whether to output verbose information
        /// </summary>
        public bool Verbose { get; set; } = false;

        /// <summary>
        /// Gets or sets whether to use guided fuzzing
        /// </summary>
        public bool GuidedFuzzing { get; set; } = true;

        /// <summary>
        /// Gets or sets the maximum script size in bytes
        /// </summary>
        public int MaxScriptSize { get; set; } = 1024;

        /// <summary>
        /// Gets or sets the minimum script size in bytes
        /// </summary>
        public int MinScriptSize { get; set; } = 10;

        /// <summary>
        /// Gets or sets the probability of generating arithmetic-heavy scripts
        /// </summary>
        public double ArithmeticProbability { get; set; } = 0.3;

        /// <summary>
        /// Gets or sets the probability of generating stack-heavy scripts
        /// </summary>
        public double StackProbability { get; set; } = 0.3;

        /// <summary>
        /// Gets or sets the probability of generating array-heavy scripts
        /// </summary>
        public double ArrayProbability { get; set; } = 0.2;

        /// <summary>
        /// Gets or sets the probability of generating random scripts
        /// </summary>
        public double RandomProbability { get; set; } = 0.2;

        /// <summary>
        /// Gets or sets the probability of using crossover mutation
        /// </summary>
        public double CrossoverProbability { get; set; } = 0.2;

        /// <summary>
        /// Gets or sets the maximum number of scripts to keep in the corpus
        /// </summary>
        public int MaxCorpusSize { get; set; } = 1000;

        /// <summary>
        /// Gets or sets whether to save all crashes
        /// </summary>
        public bool SaveAllCrashes { get; set; } = true;

        /// <summary>
        /// Gets or sets whether to save all interesting scripts
        /// </summary>
        public bool SaveAllInteresting { get; set; } = true;

        /// <summary>
        /// Gets or sets the interval for saving progress reports
        /// </summary>
        public int ProgressReportInterval { get; set; } = 100;

        /// <summary>
        /// Private constructor to enforce singleton pattern
        /// </summary>
        private ConfigurationManager()
        {
        }

        /// <summary>
        /// Loads configuration from a JSON file
        /// </summary>
        /// <param name="filePath">Path to the configuration file</param>
        /// <returns>True if configuration was loaded successfully, false otherwise</returns>
        public bool LoadFromFile(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    return false;
                }

                string json = File.ReadAllText(filePath);
                var config = JsonSerializer.Deserialize<ConfigurationManager>(json);

                if (config == null)
                {
                    return false;
                }

                // Copy properties from loaded config to this instance
                Iterations = config.Iterations;
                Seed = config.Seed;
                OutputDirectory = config.OutputDirectory;
                CorpusDirectory = config.CorpusDirectory;
                TimeoutMs = config.TimeoutMs;
                MutationRate = config.MutationRate;
                Verbose = config.Verbose;
                GuidedFuzzing = config.GuidedFuzzing;
                MaxScriptSize = config.MaxScriptSize;
                MinScriptSize = config.MinScriptSize;
                ArithmeticProbability = config.ArithmeticProbability;
                StackProbability = config.StackProbability;
                ArrayProbability = config.ArrayProbability;
                RandomProbability = config.RandomProbability;
                CrossoverProbability = config.CrossoverProbability;
                MaxCorpusSize = config.MaxCorpusSize;
                SaveAllCrashes = config.SaveAllCrashes;
                SaveAllInteresting = config.SaveAllInteresting;
                ProgressReportInterval = config.ProgressReportInterval;

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading configuration: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Saves the current configuration to a JSON file
        /// </summary>
        /// <param name="filePath">Path to save the configuration file</param>
        /// <returns>True if configuration was saved successfully, false otherwise</returns>
        public bool SaveToFile(string filePath)
        {
            try
            {
                string json = JsonSerializer.Serialize(this, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                File.WriteAllText(filePath, json);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving configuration: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Creates a default configuration file if one doesn't exist
        /// </summary>
        /// <param name="filePath">Path to the configuration file</param>
        /// <returns>True if configuration was created successfully, false otherwise</returns>
        public static bool CreateDefaultConfigurationFile(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    return true;
                }

                return Instance.SaveToFile(filePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating default configuration: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Validates the current configuration settings
        /// </summary>
        /// <returns>True if configuration is valid, false otherwise</returns>
        public bool Validate()
        {
            // Ensure output directory exists or can be created
            try
            {
                Directory.CreateDirectory(OutputDirectory);
            }
            catch
            {
                Console.WriteLine($"Error: Cannot create output directory: {OutputDirectory}");
                return false;
            }

            // Validate corpus directory if specified
            if (!string.IsNullOrEmpty(CorpusDirectory) && !Directory.Exists(CorpusDirectory))
            {
                Console.WriteLine($"Warning: Corpus directory does not exist: {CorpusDirectory}");
            }

            // Validate numeric ranges
            if (Iterations <= 0)
            {
                Console.WriteLine("Error: Iterations must be greater than 0");
                return false;
            }

            if (TimeoutMs <= 0)
            {
                Console.WriteLine("Error: Timeout must be greater than 0");
                return false;
            }

            if (MutationRate < 0.0 || MutationRate > 1.0)
            {
                Console.WriteLine("Error: Mutation rate must be between 0.0 and 1.0");
                return false;
            }

            if (MaxScriptSize <= 0 || MinScriptSize <= 0 || MinScriptSize > MaxScriptSize)
            {
                Console.WriteLine("Error: Invalid script size range");
                return false;
            }

            // Validate probability distributions
            double totalProbability = ArithmeticProbability + StackProbability + ArrayProbability + RandomProbability;
            if (Math.Abs(totalProbability - 1.0) > 0.001)
            {
                Console.WriteLine($"Warning: Script type probabilities do not sum to 1.0 (sum: {totalProbability:F3})");
            }

            return true;
        }
    }
}
