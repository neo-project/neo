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

namespace Neo.VM.Fuzzer.Utils
{
    /// <summary>
    /// Manages the corpus of scripts for fuzzing, including saving and loading interesting scripts
    /// </summary>
    public class CorpusManager
    {
        private readonly string _outputDir;
        private readonly string _crashesDir;
        private readonly string _corpusDir;
        private readonly string _dosVectorsDir;
        private readonly HashSet<string> _knownCorpusHashes = new();
        private readonly HashSet<string> _knownCrashHashes = new();
        private readonly HashSet<string> _knownDOSVectorHashes = new();
        private readonly List<byte[]> _corpus = new List<byte[]>();
        private readonly Random _random = new Random();

        /// <summary>
        /// Gets the number of scripts in the corpus
        /// </summary>
        public int CorpusSize => _corpus.Count;

        /// <summary>
        /// Creates a new corpus manager
        /// </summary>
        /// <param name="outputDir">Directory to save results to</param>
        /// <param name="initialCorpusDir">Optional directory with initial corpus of scripts</param>
        public CorpusManager(string outputDir, string? initialCorpusDir = null)
        {
            _outputDir = outputDir;
            _corpusDir = Path.Combine(outputDir, "corpus");
            _crashesDir = Path.Combine(outputDir, "crashes");
            _dosVectorsDir = Path.Combine(outputDir, "dos-vectors");

            // Create output directories if they don't exist
            Directory.CreateDirectory(_outputDir);
            Directory.CreateDirectory(_corpusDir);
            Directory.CreateDirectory(_crashesDir);
            Directory.CreateDirectory(_dosVectorsDir);
        }

        /// <summary>
        /// Loads the initial corpus from the corpus directory
        /// </summary>
        public void LoadCorpus()
        {
            if (string.IsNullOrEmpty(_corpusDir) || !Directory.Exists(_corpusDir))
            {
                return;
            }

            foreach (var file in Directory.GetFiles(_corpusDir, "*.bin"))
            {
                try
                {
                    byte[] script = File.ReadAllBytes(file);
                    _corpus.Add(script);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error loading corpus file {file}: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Gets a random script from the corpus
        /// </summary>
        /// <returns>A random script from the corpus</returns>
        public byte[] GetRandomScript()
        {
            if (_corpus.Count == 0)
            {
                throw new InvalidOperationException("Corpus is empty");
            }

            return _corpus[_random.Next(_corpus.Count)];
        }

        /// <summary>
        /// Saves a script that caused a crash
        /// </summary>
        /// <param name="script">The script that caused the crash</param>
        /// <param name="exceptionMessage">The exception message</param>
        public void SaveCrash(byte[] script, string? exceptionMessage)
        {
            string crashDir = _crashesDir;
            string filename = $"crash_{DateTime.Now:yyyyMMdd_HHmmss}_{ComputeHash(script)}.bin";
            string path = Path.Combine(crashDir, filename);

            File.WriteAllBytes(path, script);

            // Save metadata
            if (!string.IsNullOrEmpty(exceptionMessage))
            {
                string metadataPath = Path.ChangeExtension(path, ".txt");
                File.WriteAllText(metadataPath, exceptionMessage);
            }
        }

        /// <summary>
        /// Saves an interesting script that found new coverage
        /// </summary>
        /// <param name="script">The interesting script</param>
        public void SaveInteresting(byte[] script)
        {
            // Add to corpus
            _corpus.Add(script);

            // Save to disk
            string interestingDir = _corpusDir;
            string filename = $"interesting_{DateTime.Now:yyyyMMdd_HHmmss}_{ComputeHash(script)}.bin";
            string path = Path.Combine(interestingDir, filename);

            File.WriteAllBytes(path, script);
        }

        /// <summary>
        /// Saves a script that is identified as a potential DOS vector
        /// </summary>
        /// <param name="script">The script bytes</param>
        /// <param name="dosAnalysis">The DOS analysis result</param>
        /// <returns>True if the script was saved, false if it was already known</returns>
        public bool SaveDOSVector(byte[] script, DOSDetector.DOSAnalysisResult dosAnalysis)
        {
            // Generate a hash of the script to check if we've seen it before
            string hash = ComputeHash(script);

            // Skip if we've already seen this DOS vector
            if (_knownDOSVectorHashes.Contains(hash))
            {
                return false;
            }

            // Add to known DOS vectors
            _knownDOSVectorHashes.Add(hash);

            // Generate a unique filename
            string timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
            string scoreStr = dosAnalysis.DOSScore.ToString("F2").Replace(".", "_");
            string reason = dosAnalysis.DetectionReason.Replace(" ", "_").Replace(":", "_");
            string filename = $"dos-{timestamp}-{scoreStr}-{reason}-{hash.Substring(0, 8)}";

            // Save the script
            string scriptPath = Path.Combine(_dosVectorsDir, $"{filename}.bin");
            File.WriteAllBytes(scriptPath, script);

            // Save the analysis details
            string analysisPath = Path.Combine(_dosVectorsDir, $"{filename}.txt");
            using var writer = new StreamWriter(analysisPath);

            writer.WriteLine($"DOS Vector Analysis: {filename}");
            writer.WriteLine($"Timestamp: {DateTime.Now}");
            writer.WriteLine($"DOS Score: {dosAnalysis.DOSScore:F2}");
            writer.WriteLine($"Detection Reason: {dosAnalysis.DetectionReason}");
            writer.WriteLine();

            writer.WriteLine("Metrics:");
            foreach (var metric in dosAnalysis.Metrics)
            {
                writer.WriteLine($"  {metric.Key}: {metric.Value}");
            }

            writer.WriteLine();
            writer.WriteLine("Recommendations:");
            foreach (var recommendation in dosAnalysis.Recommendations)
            {
                writer.WriteLine($"  - {recommendation}");
            }

            return true;
        }

        private string ComputeHash(byte[] data)
        {
            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(data);
            return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
        }
    }
}
