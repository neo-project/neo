// Copyright (C) 2015-2024 The Neo Project.
//
// NeoFileLoggerOptions.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System;
using System.IO;
using System.Linq;

namespace Neo.Hosting.App.Configuration.Logging
{
    internal sealed class NeoFileLoggerOptions
    {
        private static readonly char[] s_invalidFileNameChars = Path.GetInvalidFileNameChars();
        private static readonly char[] s_invalidPathChars = Path.GetInvalidPathChars();

        public Microsoft.Extensions.Logging.LogLevel LogLevel { get; set; } = Microsoft.Extensions.Logging.LogLevel.Information;

        public string TimestampFormat { get; set; } = "yyyy-MM-dd HH:mm:ss.fff";
        public bool UseUtcTimestamp { get; set; } = true;

        private string _outputDirectory = "logs";
        public string OutputDirectory
        {
            get => _outputDirectory;
            set
            {
                if (s_invalidPathChars.Any(a => value.Any(c => c == a)))
                    throw new ArgumentException("Invalid characters in path.");

                _outputDirectory = value;
            }
        }

        private string _outputFileExtension = ".log";
        public string OutputFileExtension
        {
            get => _outputFileExtension;
            set
            {
                if (s_invalidFileNameChars.Any(a => value.Any(c => c == a)))
                    throw new ArgumentException("Invalid characters in file extension.");

                _outputFileExtension = value;
            }
        }
    }
}
