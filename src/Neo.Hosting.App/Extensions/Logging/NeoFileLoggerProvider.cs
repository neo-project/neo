// Copyright (C) 2015-2024 The Neo Project.
//
// NeoFileLoggerProvider.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Neo.Hosting.App.Configuration.Logging;
using System;
using System.Collections.Concurrent;
using System.IO;

namespace Neo.Hosting.App.Extensions.Logging
{
    [ProviderAlias("NeoFile")]
    internal sealed class NeoFileLoggerProvider : ILoggerProvider
    {
        private readonly ConcurrentDictionary<string, NeoFileLogger> _loggers = new(StringComparer.OrdinalIgnoreCase);
        private readonly IDisposable? _onChangeToken;

        private NeoFileLoggerOptions _currentConfig;


        public NeoFileLoggerProvider(
            IOptionsMonitor<NeoFileLoggerOptions> config)
        {
            _currentConfig = config.CurrentValue;
            _onChangeToken = config.OnChange(updatedConfig => _currentConfig = updatedConfig);

            _currentConfig.OutputDirectory = _currentConfig.OutputDirectory;
        }

        public ILogger CreateLogger(string categoryName)
        {
            var config = GetCurrentConfig();
            var dirInfo = new DirectoryInfo(config.OutputDirectory);

            if (dirInfo.Exists == false)
                dirInfo.Create();

            return _loggers.GetOrAdd(categoryName, name => new NeoFileLogger(name, GetCurrentConfig));
        }

        private NeoFileLoggerOptions GetCurrentConfig() =>
            _currentConfig;

        public void Dispose()
        {
            _loggers.Clear();
            _onChangeToken?.Dispose();
        }
    }
}
