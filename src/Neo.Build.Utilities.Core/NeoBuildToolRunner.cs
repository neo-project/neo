// Copyright (C) 2015-2025 The Neo Project.
//
// NeoBuildToolRunner.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace Neo.Build.Utilities.Core
{
    internal class NeoBuildToolRunner
    {
        private readonly ConcurrentQueue<string> _standardOutput = new();
        private readonly ConcurrentQueue<string> _standardError = new();

        public ICollection<string> StandardOutputResults => [.. _standardOutput];
        public ICollection<string> StandardErrorResults => [.. _standardError];

        public void Run(string filename, string args, string? workingDir = default)
        {
            var completeEvent = new ManualResetEvent(false);
            var startInfo = new ProcessStartInfo(filename, args)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                WorkingDirectory = string.IsNullOrEmpty(workingDir) ? string.Empty : workingDir,
            };

            var process = new Process()
            {
                StartInfo = startInfo,
                EnableRaisingEvents = true,
            };

            process.OutputDataReceived += ProcessStandardOutput;
            process.ErrorDataReceived += ProcessStandardError;

            process.Exited += (sender, args) => completeEvent.Set();

            if (process.Start() == false)
                throw new InvalidProgramException();

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            completeEvent.WaitOne();
        }

        private void ProcessStandardOutput(object sender, DataReceivedEventArgs args)
        {
            if (args.Data is null) return;

            _standardOutput.Enqueue(args.Data);
        }

        private void ProcessStandardError(object sender, DataReceivedEventArgs args)
        {
            if (args.Data is null) return;

            _standardError.Enqueue(args.Data);
        }
    }
}
