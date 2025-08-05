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
    public class NeoBuildToolRunner
    {
        private readonly ConcurrentQueue<string> _standardOutput = new();
        private readonly ConcurrentQueue<string> _standardError = new();

        public ICollection<string> StandardOutputResults => [.. _standardOutput];
        public ICollection<string> StandardErrorResults => [.. _standardError];

        public void Run(string filename, string args, string? workingDir = default)
        {
            var completeEvent = new ManualResetEvent(false);
            var process = new Process()
            {
                StartInfo = new(filename, args)
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    WorkingDirectory = string.IsNullOrEmpty(workingDir) ? string.Empty : workingDir,
                },
                EnableRaisingEvents = true,
            };

            process.OutputDataReceived += OnProcessOutputDataReceived;
            process.ErrorDataReceived += OnProcessErrorDataReceived;
            process.Exited += (sender, args) => completeEvent.Set();

            if (process.Start() == false)
                throw new InvalidProgramException();

            process.BeginOutputReadLine();
            process.BeginOutputReadLine();

            completeEvent.WaitOne();
        }

        private void OnProcessOutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data is null) return;

            _standardOutput.Enqueue(e.Data);
        }

        private void OnProcessErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data is null) return;

            _standardError.Enqueue(e.Data);
        }
    }
}
