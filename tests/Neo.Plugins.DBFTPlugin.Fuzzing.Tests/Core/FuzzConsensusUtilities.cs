// Copyright (C) 2015-2025 The Neo Project.
//
// FuzzConsensusUtilities.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Neo.Cryptography;
using System;
using System.IO;

namespace Neo.Plugins.DBFTPlugin.Fuzzing.Tests.Core
{
    public partial class FuzzConsensus
    {
        // Helper method to record interesting test cases for further analysis
        private static void RecordInterestingTestCase(byte[] data, string reason)
        {
            try
            {
                // Create directory if it doesn't exist
                string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "interesting_cases");
                Directory.CreateDirectory(dir);

                // Generate a unique filename based on the hash of the data and reason
                string hash = BitConverter.ToString(data.Sha256()).Replace("-", "").Substring(0, 16);
                string filename = Path.Combine(dir, $"{hash}_{reason.GetHashCode():X8}.bin");

                // Write the test case to disk
                File.WriteAllBytes(filename, data);
                LogInfo($"Recorded interesting test case: {filename}");
            }
            catch (Exception ex)
            {
                // Don't let recording failures affect the fuzzing process
                LogWarning($"Failed to record interesting test case: {ex.Message}");
            }
        }

        // Logging helpers with different severity levels
        private static void LogVerbose(string message)
        {
            // In production, verbose logs are typically disabled or redirected to a file
            // Console.WriteLine($"[VERBOSE] {message}");
        }

        private static void LogInfo(string message)
        {
            Console.WriteLine($"[INFO] {message}");
        }

        private static void LogWarning(string message)
        {
            Console.WriteLine($"[WARNING] {message}");
        }

        private static void LogError(string message)
        {
            Console.WriteLine($"[ERROR] {message}");
        }
    }
}
