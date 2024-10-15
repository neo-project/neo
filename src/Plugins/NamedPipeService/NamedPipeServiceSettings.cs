// Copyright (C) 2015-2024 The Neo Project.
//
// NamedPipeServiceSettings.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.Extensions.Configuration;
using Neo.Plugins.Configuration;
using System;

namespace Neo.Plugins
{
    internal class NamedPipeServiceSettings
    {
        public NamedPipeEndPoint PipeName { get; private init; }

        public NamedPipeServerTransportOptions TransportOptions { get; private init; }

        public static NamedPipeServiceSettings Default { get; private set; } = new()
        {
            PipeName = new("NeoNodeService"),
            TransportOptions = new(),
        };

        public NamedPipeServiceSettings()
        {
            PipeName = Default.PipeName;
            TransportOptions = Default.TransportOptions;
        }

        private NamedPipeServiceSettings(IConfigurationSection section)
        {
            PipeName = section.GetValue(nameof(PipeName), Default.PipeName)!;
            TransportOptions = new()
            {
                ListenerQueueCount = Math.Min(section.GetValue("PipeCount", Environment.ProcessorCount), 16),
            };
        }

        public static NamedPipeServiceSettings Load(IConfigurationSection section) =>
            new(section);
    }
}
