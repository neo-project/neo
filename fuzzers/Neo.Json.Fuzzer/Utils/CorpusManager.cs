// Copyright (C) 2015-2025 The Neo Project.
//
// CorpusManager.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Neo.Json.Fuzzer.Utils
{
    /// <summary>
    /// Manages the corpus of JSON test cases
    /// </summary>
    public class CorpusManager
    {
        private readonly string _outputDir;
        private readonly string? _corpusDir;
        private readonly List<string> _corpus = new();
        private readonly HashSet<string> _knownHashes = new();

        /// <summary>
        /// Gets the current size of the corpus
        /// </summary>
        public int CorpusSize => _corpus.Count;

        /// <summary>
        /// Initializes a new instance of the CorpusManager class
        /// </summary>
        /// <param name="outputDir">Directory for storing output files</param>
        /// <param name="corpusDir">Optional directory with initial corpus</param>
        public CorpusManager(string outputDir, string? corpusDir = null)
        {
            _outputDir = outputDir ?? throw new ArgumentNullException(nameof(outputDir));
            _corpusDir = corpusDir;

            // Create necessary directories
            Directory.CreateDirectory(_outputDir);
            Directory.CreateDirectory(Path.Combine(_outputDir, "crashes"));
            Directory.CreateDirectory(Path.Combine(_outputDir, "interesting"));
            Directory.CreateDirectory(Path.Combine(_outputDir, "dos-vectors"));
        }

        /// <summary>
        /// Loads the initial corpus from the corpus directory
        /// </summary>
        public void LoadCorpus()
        {
            if (string.IsNullOrEmpty(_corpusDir) || !Directory.Exists(_corpusDir))
            {
                // Create a minimal corpus if no directory is provided
                CreateMinimalCorpus();
                return;
            }

            // Load all JSON files from the corpus directory
            foreach (string file in Directory.GetFiles(_corpusDir, "*.json", SearchOption.AllDirectories))
            {
                try
                {
                    string json = File.ReadAllText(file);
                    AddToCorpus(json);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading corpus file {file}: {ex.Message}");
                }
            }

            // If no valid files were found, create a minimal corpus
            if (_corpus.Count == 0)
            {
                CreateMinimalCorpus();
            }
        }

        /// <summary>
        /// Creates a minimal corpus with basic JSON structures
        /// </summary>
        private void CreateMinimalCorpus()
        {
            // Add basic JSON structures to the corpus
            string[] basicJsons = new[]
            {
                "{}",
                "[]",
                "null",
                "true",
                "false",
                "0",
                "1",
                "-1",
                "0.5",
                "\"\"",
                "\"test\"",
                "{\"key\": \"value\"}",
                "[1, 2, 3]",
                "{\"array\": [1, 2, 3]}",
                "[{\"key\": \"value\"}, {\"key\": \"value2\"}]",
                "{\"nested\": {\"key\": \"value\"}}",
                "{\"unicode\": \"\\u0000\\u0001\\u0002\"}",
                "{\"escape\": \"\\b\\f\\n\\r\\t\\\\\\\"\\/\"}",
                "{\"number\": 1.23e+45}",
                "{\"maxDepth\": " + GenerateNestedJson(20) + "}"
            };

            foreach (string json in basicJsons)
            {
                AddToCorpus(json);
            }
        }

        /// <summary>
        /// Generates a nested JSON structure with the specified depth
        /// </summary>
        private string GenerateNestedJson(int depth)
        {
            if (depth <= 0)
            {
                return "0";
            }

            return depth % 2 == 0
                ? $"{{\"level{depth}\": {GenerateNestedJson(depth - 1)}}}"
                : $"[{GenerateNestedJson(depth - 1)}]";
        }

        /// <summary>
        /// Gets a random JSON string from the corpus
        /// </summary>
        public string GetRandomJson()
        {
            if (_corpus.Count == 0)
            {
                return "{}"; // Return empty object if corpus is empty
            }

            int index = new Random().Next(_corpus.Count);
            return _corpus[index];
        }

        /// <summary>
        /// Adds a JSON string to the corpus if it's not already present
        /// </summary>
        private bool AddToCorpus(string json)
        {
            string hash = ComputeHash(json);

            if (_knownHashes.Contains(hash))
            {
                return false; // Already in corpus
            }

            _corpus.Add(json);
            _knownHashes.Add(hash);
            return true;
        }

        /// <summary>
        /// Saves a JSON string that caused a crash
        /// </summary>
        public void SaveCrash(string json, string? exceptionType)
        {
            string hash = ComputeHash(json);
            string filename = $"crash_{DateTime.Now:yyyyMMdd_HHmmss}_{hash.Substring(0, 8)}_{exceptionType}.json";
            string path = Path.Combine(_outputDir, "crashes", filename);

            try
            {
                File.WriteAllText(path, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving crash: {ex.Message}");
            }
        }

        /// <summary>
        /// Saves a JSON string that found new coverage
        /// </summary>
        public void SaveInteresting(string json)
        {
            // Add to corpus
            if (!AddToCorpus(json))
            {
                return; // Already in corpus
            }

            string hash = ComputeHash(json);
            string filename = $"interesting_{DateTime.Now:yyyyMMdd_HHmmss}_{hash.Substring(0, 8)}.json";
            string path = Path.Combine(_outputDir, "interesting", filename);

            try
            {
                File.WriteAllText(path, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving interesting case: {ex.Message}");
            }
        }

        /// <summary>
        /// Saves a JSON string that is a potential DOS vector
        /// </summary>
        public void SaveDOSVector(string json, DOSAnalysisResult analysis)
        {
            string hash = ComputeHash(json);
            string scoreStr = analysis.DOSScore.ToString("F2").Replace(".", "_");
            string filename = $"dos_{DateTime.Now:yyyyMMdd_HHmmss}_{hash.Substring(0, 8)}_{scoreStr}.json";
            string path = Path.Combine(_outputDir, "dos-vectors", filename);

            try
            {
                // Save the JSON
                File.WriteAllText(path, json);

                // Save analysis information
                string analysisPath = Path.ChangeExtension(path, ".analysis.txt");
                using (StreamWriter writer = new(analysisPath))
                {
                    writer.WriteLine($"DOS Score: {analysis.DOSScore:F2}");
                    writer.WriteLine($"Detection Reason: {analysis.DetectionReason}");
                    writer.WriteLine();
                    writer.WriteLine("Metrics:");
                    foreach (var metric in analysis.Metrics.OrderBy(m => m.Key))
                    {
                        writer.WriteLine($"  {metric.Key}: {metric.Value}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving DOS vector: {ex.Message}");
            }
        }

        /// <summary>
        /// Computes a hash of a JSON string
        /// </summary>
        private string ComputeHash(string json)
        {
            using var sha = SHA256.Create();
            byte[] bytes = Encoding.UTF8.GetBytes(json);
            byte[] hash = sha.ComputeHash(bytes);
            return BitConverter.ToString(hash).Replace("-", "");
        }
    }
}
