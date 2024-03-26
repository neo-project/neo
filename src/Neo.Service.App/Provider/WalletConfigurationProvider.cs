// Copyright (C) 2015-2024 The Neo Project.
//
// WalletConfigurationProvider.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using Microsoft.Extensions.Configuration;
using Neo.Service.App.Configuration;
using System;

namespace Neo.Service.App.Provider
{
    internal sealed class WalletConfigurationProvider : ConfigurationProvider
    {
        public WalletConfigurationSource Source { get; }

        public WalletConfigurationProvider(
            WalletConfigurationSource source)
        {
            ArgumentNullException.ThrowIfNull(source, nameof(source));
            Source = source;
        }

        public override void Load()
        {

        }

        public override string ToString() =>
            $"{GetType().Name} for '{Source.Path}' on Network '{Source.ProtocolSettings.Network}'";
    }
}
