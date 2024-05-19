// Copyright (C) 2015-2024 The Neo Project.
//
// NeoErrorLoggerProvider.cs file belongs to the neo project and is free
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

namespace Neo.Hosting.App.Extensions.Logging
{
    [ProviderAlias("NeoError")]
    internal sealed class NeoErrorLoggerProvider : ILoggerProvider
    {
        private readonly ConcurrentDictionary<string, NeoErrorLogger> _loggers = new(StringComparer.OrdinalIgnoreCase);
        private readonly IDisposable? _onChangeToken;

        private NeoErrorLoggerOptions _currentConfig;


        public NeoErrorLoggerProvider(
            IOptionsMonitor<NeoErrorLoggerOptions> config)
        {
            _currentConfig = config.CurrentValue;
            _onChangeToken = config.OnChange(updatedConfig => _currentConfig = updatedConfig);

            _currentConfig.OutputDirectory = _currentConfig.OutputDirectory;
        }

        public ILogger CreateLogger(string categoryName) =>
            _loggers.GetOrAdd(categoryName, name => new NeoErrorLogger(name, GetCurrentConfig));

        private NeoErrorLoggerOptions GetCurrentConfig() =>
            _currentConfig;

        public void Dispose()
        {
            _loggers.Clear();
            _onChangeToken?.Dispose();
        }
    }
}
